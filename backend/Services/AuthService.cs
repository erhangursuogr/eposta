using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DeuEposta.Services;

public interface IAuthService
{
    Task<ResponseDataModel<LoginData>> LoginAsync(LoginRequest request, string clientIP, string userAgent);

    Task<ResponseModel> LogoutAsync(string? email, string? jti, string ipAddress);

    Task<ResponseDataModel<UserInfo>> GetCurrentUserAsync(string? email);
}

public class AuthService : IAuthService
{
    private readonly DeuEpostaContext _context;
    private readonly ILdapService _ldapService;
    private readonly IOracle11gService _oracle11gService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IMemoryCache _memoryCache;
    private readonly ISystemSettingsService _systemSettingsService;

    public AuthService(
        DeuEpostaContext context,
        ILdapService ldapService,
        IOracle11gService oracle11gService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IServiceProvider serviceProvider,
        ITokenBlacklistService tokenBlacklistService,
        IMemoryCache memoryCache,
        ISystemSettingsService systemSettingsService)
    {
        _context = context;
        _ldapService = ldapService;
        _oracle11gService = oracle11gService;
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _tokenBlacklistService = tokenBlacklistService;
        _memoryCache = memoryCache;
        _systemSettingsService = systemSettingsService;
    }

    public async Task<ResponseDataModel<LoginData>> LoginAsync(LoginRequest request, string clientIP, string userAgent)
    {
        var response = new ResponseDataModel<LoginData>();

        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            response.Success = false;
            response.StatusCode = 400;
            response.Message = "Email ve şifre gerekli";
            return response;
        }

        // @deu.edu.tr uzantısı yoksa ekle
        string email = request.Email.Trim().ToLower();
        if (!email.Contains("@"))
        {
            email = email + "@deu.edu.tr";
        }
        else if (!email.EndsWith("@deu.edu.tr"))
        {
            response.Success = false;
            response.StatusCode = 400;
            response.Message = "Sadece @deu.edu.tr uzantılı e-posta adresleri kabul edilir";
            return response;
        }

        // Email'i normalize edilmiş haliyle güncelle
        request.Email = email;

        try
        {
            // Security validations
            var securityService = _serviceProvider.GetRequiredService<ISecurityService>();

            if (!await securityService.ValidateLoginAttemptAsync(request.Email, clientIP, userAgent))
            {
                await securityService.LogSecurityEventAsync("LOGIN_BLOCKED",
                    $"Login blocked for {request.Email}", clientIP);
                response.Success = false;
                response.StatusCode = 429;
                response.Message = "Çok fazla başarısız giriş denemesi. Lütfen 1 saat sonra tekrar deneyin.";
                return response;
            }

            if (!securityService.IsUniversityEmail(request.Email))
            {
                await securityService.LogSecurityEventAsync("INVALID_EMAIL_DOMAIN",
                    $"Login attempt with non-university email: {request.Email}", clientIP);
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Sadece üniversite email adresleri kabul edilir.";
                return response;
            }

            // LDAP ONLY AUTHENTICATION
            // Sadece gerçek LDAP kullanıcıları sisteme girebilir

            // LDAP authentication
            var ldapResult = await _ldapService.AuthenticateWithDetailsAsync(request.Email, request.Password);

            if (!ldapResult.IsSuccess)
            {
                securityService.RecordFailedLoginAttempt(request.Email, clientIP, userAgent);
                await LogLoginAttemptAsync(request.Email, "LDAP", clientIP, ldapResult.ErrorMessage, null, userAgent);
                response.Success = false;
                response.StatusCode = 401;
                response.Message = ldapResult.ErrorMessage ?? "Kullanıcı adı veya şifre hatalı";
                return response;
            }

            // SADECE KAYITLI KULLANICILAR GİREBİLİR - Otomatik kayıt kapatıldı
            var user = await GetUserFromCacheAsync(request.Email);

            if (user == null)
            {
                securityService.RecordFailedLoginAttempt(request.Email, clientIP, userAgent);
                await LogLoginAttemptAsync(request.Email, "LDAP", clientIP, "Kullanıcı sistemde kayıtlı değil", null, userAgent);
                response.Success = false;
                response.StatusCode = 403;
                // GÜVENLIK: User enumeration prevention - Generic error message
                response.Message = "Geçersiz kullanıcı bilgileri. Lütfen bilgilerinizi kontrol edin veya sistem yöneticisiyle iletişime geçin.";
                return response;
            }

            if (user.Aktif != "Y")
            {
                securityService.RecordFailedLoginAttempt(request.Email, clientIP, userAgent);
                await LogLoginAttemptAsync(request.Email, "LDAP", clientIP, "Kullanıcı aktif değil", null, userAgent);
                response.Success = false;
                response.StatusCode = 403;
                // GÜVENLIK: User enumeration prevention - Generic error message
                response.Message = "Geçersiz kullanıcı bilgileri. Lütfen bilgilerinizi kontrol edin veya sistem yöneticisiyle iletişime geçin.";
                return response;
            }

            // 11g'den personel bilgilerini güncelle
            await UpdateUserFromOracle11gAsync(user);

            var token = GenerateJwtToken(user);
            user.SonGirisTarihi = DateTime.Now;
            user.GuncellemeTarihi = DateTime.Now;

            // Tüm değişiklikleri kaydet (AdSoyad, Unvan, Departman, SonGirisTarihi)
            await _context.SaveChangesAsync();

            securityService.RecordSuccessfulLogin(request.Email, clientIP);
            await LogLoginAttemptAsync(request.Email, "LDAP", clientIP, null, user.Id, userAgent);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Giriş başarılı";
            response.Data = new LoginData
            {
                Token = token,
                User = new UserInfo
                {
                    Id = user.Id,
                    KullaniciAdi = user.KullaniciAdi,
                    AdSoyad = user.AdSoyad,
                    Email = user.Email,
                    Departman = user.Departman,
                    Unvan = user.Unvan,
                    GorevYeri = user.GorevYeri,
                    GorevYeriAdi = user.GorevYeriAdi,
                    Rol = user.Rol?.RolKodu ?? RolKodu.VIEWER
                }
            };
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for user {Email}", request.Email);
            await LogLoginAttemptAsync(request.Email, "LDAP", clientIP, "Sistem hatası", null, userAgent);

            response.Success = false;
            response.StatusCode = 500;
            response.Message = "Sistem hatası oluştu";
            return response;
        }
    }

    public async Task<ResponseModel> LogoutAsync(string? email, string? jti, string ipAddress)
    {
        var response = new ResponseModel();

        if (string.IsNullOrEmpty(email))
        {
            response.Success = false;
            response.StatusCode = 400;
            response.Message = "Geçersiz token";
            return response;
        }

        try
        {
            var user = await GetUserFromCacheAsync(email);

            // Token'ı blacklist'e ekle
            if (!string.IsNullOrEmpty(jti))
            {
                var jwtSettings = _configuration.GetSection("Jwt");
                var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");
                var tokenExpiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

                _tokenBlacklistService.BlacklistToken(jti, tokenExpiration);
                _logger.LogInformation("Token {Jti} blacklisted for user {Email}", jti, email);
            }

            await LogLoginAttemptAsync(email, "API", ipAddress, null, user?.Id);

            _logger.LogInformation("User {Email} logged out from IP {IP}", email, ipAddress);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Çıkış başarılı";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error for user {Email}", email);

            response.Success = false;
            response.StatusCode = 500;
            response.Message = "Sistem hatası oluştu";
            return response;
        }
    }

    public async Task<ResponseDataModel<UserInfo>> GetCurrentUserAsync(string? email)
    {
        var response = new ResponseDataModel<UserInfo>();

        if (string.IsNullOrEmpty(email))
        {
            response.Success = false;
            response.StatusCode = 401;
            response.Message = "Geçersiz token";
            return response;
        }

        try
        {
            var user = await GetUserFromCacheAsync(email);

            if (user == null)
            {
                response.Success = false;
                response.StatusCode = 404;
                response.Message = "Kullanıcı bulunamadı";
                return response;
            }

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Kullanıcı bilgileri alındı";
            response.Data = new UserInfo
            {
                Id = user.Id,
                KullaniciAdi = user.KullaniciAdi,
                AdSoyad = user.AdSoyad,
                Email = user.Email,
                Departman = user.Departman,
                Unvan = user.Unvan,
                GorevYeri = user.GorevYeri,
                GorevYeriAdi = user.GorevYeriAdi,
                Rol = user.Rol?.RolKodu ?? RolKodu.VIEWER
            };
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentUser endpoint error");

            response.Success = false;
            response.StatusCode = 500;
            response.Message = "Sistem hatası oluştu";
            return response;
        }
    }

    public async Task<Kullanici?> GetUserFromCacheAsync(string email)
    {
        const string aktifDeger = "Y";
        var cacheKey = $"user_{email}";

        // Cache'den kontrol et
        if (_memoryCache.TryGetValue(cacheKey, out Kullanici? cachedUser))
        {
            _logger.LogDebug("User {Email} retrieved from cache", email);
            return cachedUser;
        }

        // Cache'de yoksa database'den al
        var user = await _context.Kullanicilar
            .Include(k => k.Rol)
            .FirstOrDefaultAsync(k => k.Email == email && k.Aktif == aktifDeger);

        // Cache'e ekle (süre sistem ayarlarından okunuyor)
        if (user != null)
        {
            int cacheMinutes;
            try
            {
                cacheMinutes = await _systemSettingsService.GetSettingValueAsync("CACHE", "USER_DATA_MINUTES", 5);
            }
            catch
            {
                cacheMinutes = 5; // Fallback
            }

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(Math.Max(1, cacheMinutes / 2))
            };

            _memoryCache.Set(cacheKey, user, cacheOptions);
            _logger.LogDebug("User {Email} cached for {Minutes} minutes", email, cacheMinutes);
        }

        return user;
    }

    // REMOVED: GetOrCreateUserAsync - Otomatik kayıt özelliği kaldırıldı
    // Kullanıcılar artık sadece admin tarafından manuel olarak eklenebilir

    private string GenerateJwtToken(Kullanici user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        // Environment variable fallback for JWT secret key
        var secretKey = Environment.GetEnvironmentVariable("EPOSTA_JWT_KEY")
                       ?? jwtSettings["Key"]
                       ?? throw new InvalidOperationException("JWT Key not found. Set via appsettings.json or EPOSTA_JWT_KEY environment variable.");

        var key = Encoding.ASCII.GetBytes(secretKey);

        // Unique token ID for blacklisting
        var jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.AdSoyad),
            new Claim(ClaimTypes.Role, user.Rol?.RolKodu ?? RolKodu.VIEWER),
            new Claim("KullaniciAdi", user.KullaniciAdi),
            new Claim("RolKodu", user.Rol?.RolKodu ?? RolKodu.VIEWER)
        };

        // GorevYeri claims ekle (nullable olduğu için opsiyonel)
        if (user.GorevYeri.HasValue)
        {
            claims.Add(new Claim("GorevYeri", user.GorevYeri.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(user.GorevYeriAdi))
        {
            claims.Add(new Claim("GorevYeriAdi", user.GorevYeriAdi));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationInMinutes"] ?? "600")),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    private async Task LogLoginAttemptAsync(string email, string girisTuru, string ipAdresi,
        string? hataMesaji, int? kullaniciId = null, string? userAgent = null)
    {
        try
        {
            // Email'den kullanıcı adını ayır (@'den önceki kısım)
            var kullaniciAdi = email.Contains("@") ? email.Split('@')[0] : email;

            var log = new LogLogin
            {
                KullaniciAdi = kullaniciAdi,
                Email = email,
                KullaniciId = kullaniciId,
                GirisTuru = girisTuru,
                IpAdres = ipAdresi,
                UserAgent = userAgent,
                HataMesaji = hataMesaji,
                Basarili = hataMesaji == null ? "Y" : "N",
                GirisTarihi = DateTime.Now
            };

            _context.LogLogin.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging login attempt for {User}", email);
        }
    }

    /// <summary>
    /// Oracle 11g'den personel bilgilerini alıp kullanıcı kaydını günceller
    /// 1. Email ile V_PERSONEL_EMAIL_BILGI tablosundan ara
    /// 2. Bulunamazsa XXDEU.KULLANICI -> TC kimlik no -> V_PERSONEL_EMAIL_BILGI
    /// 3. Bulunan bilgileri KULLANICILAR tablosunda güncelle
    /// </summary>
    private async Task UpdateUserFromOracle11gAsync(Kullanici user)
    {
        try
        {
            // 11g'den personel bilgilerini email ile getir
            var personel = await _oracle11gService.GetPersonelByEmailAsync(user.Email);

            if (personel == null)
            {
                _logger.LogWarning("Personel not found in Oracle 11g for email: {Email}", user.Email);
                return;
            }

            // Kullanıcı bilgilerini güncelle
            var updated = false;

            if (!string.IsNullOrEmpty(personel.AdSoyad) && user.AdSoyad != personel.AdSoyad)
            {
                _logger.LogInformation("Updating AdSoyad: {Old} -> {New}", user.AdSoyad, personel.AdSoyad);
                user.AdSoyad = personel.AdSoyad;
                updated = true;
            }

            if (!string.IsNullOrEmpty(personel.Unvan) && user.Unvan != personel.Unvan)
            {
                _logger.LogInformation("Updating Unvan: {Old} -> {New}", user.Unvan, personel.Unvan);
                user.Unvan = personel.Unvan;
                updated = true;
            }

            if (!string.IsNullOrEmpty(personel.Departman) && user.Departman != personel.Departman)
            {
                _logger.LogInformation("Updating Departman: {Old} -> {New}", user.Departman, personel.Departman);
                user.Departman = personel.Departman;
                updated = true;
            }

            if (personel.GorevYeri.HasValue && user.GorevYeri != personel.GorevYeri)
            {
                _logger.LogInformation("Updating GorevYeri: {Old} -> {New}", user.GorevYeri, personel.GorevYeri);
                user.GorevYeri = personel.GorevYeri;
                updated = true;
            }

            if (!string.IsNullOrEmpty(personel.GorevYeriAdi) && user.GorevYeriAdi != personel.GorevYeriAdi)
            {
                _logger.LogInformation("Updating GorevYeriAdi: {Old} -> {New}", user.GorevYeriAdi, personel.GorevYeriAdi);
                user.GorevYeriAdi = personel.GorevYeriAdi;
                updated = true;
            }

            // Cache'i her durumda invalidate et (kullanıcı login olduğunda)
            var cacheKey = $"user_{user.Email}";
            _memoryCache.Remove(cacheKey);

            if (updated)
            {
                _logger.LogInformation("User {Email} updated from Oracle 11g (cache invalidated)", user.Email);
            }
            else
            {
                _logger.LogInformation("User {Email} already up to date with Oracle 11g (cache invalidated)", user.Email);
            }
        }
        catch (Exception ex)
        {
            // Oracle 11g bağlantı hatası login'i engellemez, sadece log kaydı
            _logger.LogWarning(ex, "Failed to update user from Oracle 11g for {Email}", user.Email);
        }
    }
}