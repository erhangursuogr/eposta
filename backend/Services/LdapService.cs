using System.ServiceModel;

namespace DeuEposta.Services;

public interface ILdapService
{
    Task<bool> AuthenticateUserAsync(string email, string password);

    Task<LdapAuthResult> AuthenticateWithDetailsAsync(string email, string password);
}

public class LdapAuthResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserInfo { get; set; }
}

public class LdapService : ILdapService
{
    private readonly ILogger<LdapService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISystemSettingsService _systemSettingsService;

    public LdapService(
        ILogger<LdapService> logger,
        IConfiguration configuration,
        ISystemSettingsService systemSettingsService)
    {
        _logger = logger;
        _configuration = configuration;
        _systemSettingsService = systemSettingsService;
    }

    public async Task<bool> AuthenticateUserAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("LDAP authentication attempted with empty email or password");
                return false;
            }

            // DEBIS servis ayarlarını database'den al (SISTEM_AYARLARI.DEBIS kategorisi)
            var serviceKey = await _systemSettingsService.GetSettingValueAsync("DEBIS", "SERVICE_KEY")
                             ?? _configuration["LdapSettings:ServiceKey"]
                             ?? "ndlar75aGaQ345a5s6s1a0?63dTTf"; // Final fallback

            // LDAP servis çağrısı
            using var client = new DebisReference.DebisHSServiceSoapClient(DebisReference.DebisHSServiceSoapClient.EndpointConfiguration.DebisHSServiceSoap);

            await client.OpenAsync();

            var encryptedEmail = StringCipher.EncryptRSA(email);
            var encryptedPassword = StringCipher.EncryptRSA(password);

            var result = await client.LDAPGirisKontroluAsync(serviceKey, encryptedEmail, encryptedPassword);

            await client.CloseAsync();

            _logger.LogInformation("LDAP authentication result for {Email}: {Result}",
                email.Substring(0, Math.Min(3, email.Length)) + "***", result);

            return result;
        }
        catch (CommunicationException ex)
        {
            _logger.LogError(ex, "LDAP service communication error for user {Email}", email);
            return false;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "LDAP service timeout for user {Email}", email);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LDAP authentication error for user {Email}", email);
            return false;
        }
    }

    public async Task<LdapAuthResult> AuthenticateWithDetailsAsync(string email, string password)
    {
        try
        {
            var isAuthenticated = await AuthenticateUserAsync(email, password);

            return new LdapAuthResult
            {
                IsSuccess = isAuthenticated,
                ErrorMessage = isAuthenticated ? null : "Kullanıcı adı veya şifre hatalı",
                UserInfo = isAuthenticated ? "LDAP authentication successful" : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LDAP detailed authentication error for user {Email}", email);

            return new LdapAuthResult
            {
                IsSuccess = false,
                ErrorMessage = "Sistem hatası: LDAP servisi ile bağlantı kurulamadı",
                UserInfo = null
            };
        }
    }
}