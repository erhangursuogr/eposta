using Oracle.ManagedDataAccess.Client;

namespace DeuEposta.Services;

/// <summary>
/// Oracle 11g veritabanı sorguları için basit servis
/// Personel view'ları ve kontrol sorguları için kullanılır
/// </summary>
public interface IOracle11gService
{
    Task<PersonelDto?> GetPersonelByEmailAsync(string email);
}

public class Oracle11gService : IOracle11gService
{
    private readonly string _connectionString;
    private readonly ILogger<Oracle11gService> _logger;

    public Oracle11gService(IConfiguration configuration, ILogger<Oracle11gService> logger)
    {
        // Environment variable fallback for Oracle 11g connection
        _connectionString = Environment.GetEnvironmentVariable("EPOSTA_ORACLE11G_CONNECTION")
                           ?? configuration.GetConnectionString("Oracle11gConnection")
                           ?? throw new InvalidOperationException("Oracle11gConnection string is required. Set via appsettings.json or EPOSTA_ORACLE11G_CONNECTION environment variable.");
        _logger = logger;
    }

    /// <summary>
    /// Email adresine göre personel bilgisi çeker
    /// 1. Önce V_PERSONEL_EMAIL_BILGI tablosundan email ile ara
    /// 2. Bulunamazsa XXDEU.KULLANICI tablosundan TC kimlik no al
    /// 3. TC kimlik no ile V_PERSONEL_EMAIL_BILGI tablosunda tekrar ara
    /// </summary>
    public async Task<PersonelDto?> GetPersonelByEmailAsync(string email)
    {
        try
        {
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            // 1. Adım: Email ile direkt arama V_PERSONEL_EMAIL_BILGI tablosunda
            var sql1 = @"
                SELECT
                    ISIM || ' ' || SOYISIM AS AD_SOYAD,
                    EMAIL,
                    BIRIMDEKI_CALISMA_YERI AS DEPARTMAN,
                    UNVANI AS UNVAN,
                    TC_KIMLIK_NO,
                    GOREV_YERI,
                    GOREV_YERI_ADI,
                    CEPTEL,
                    DURUM
                FROM V_PERSONEL_EMAIL_BILGI
                WHERE UPPER(EMAIL) = UPPER(:email)                
                AND KURUM <> 999                
                AND ROWNUM = 1";

            using var command1 = new OracleCommand(sql1, connection);
            command1.Parameters.Add(":email", OracleDbType.Varchar2).Value = email;
            command1.CommandTimeout = 30;

            using var reader1 = await command1.ExecuteReaderAsync();
            if (await reader1.ReadAsync())
            {
                var rawPhone = reader1.IsDBNull(7) ? null : reader1.GetString(7);
                var personel = new PersonelDto
                {
                    AdSoyad = reader1.IsDBNull(0) ? null : reader1.GetString(0),
                    Email = reader1.IsDBNull(1) ? null : reader1.GetString(1),
                    Departman = reader1.IsDBNull(2) ? null : reader1.GetString(2),
                    Unvan = reader1.IsDBNull(3) ? null : reader1.GetString(3),
                    TcKimlikNo = reader1.IsDBNull(4) ? null : reader1.GetString(4),
                    GorevYeri = reader1.IsDBNull(5) ? null : (int?)reader1.GetInt32(5),
                    GorevYeriAdi = reader1.IsDBNull(6) ? null : reader1.GetString(6),
                    CepTel = NormalizePhoneNumber(rawPhone),
                    Durum = reader1.IsDBNull(8) ? null : (int?)reader1.GetInt32(8)
                };

                _logger.LogInformation("Personel found by email: {Email} -> {AdSoyad}", email, personel.AdSoyad);
                return personel;
            }

            // 2. Adım: Bulunamadıysa XXDEU.KULLANICI tablosundan TC kimlik no al
            _logger.LogInformation("Personel not found by email, trying XXDEU.KULLANICI table for: {Email}", email);

            var sql2 = @"
                SELECT TC_KIMLIK_NO
                FROM XXDEU.KULLANICI
                WHERE UPPER(KULLANICI_KODU) = UPPER(:email)
                AND TC_KIMLIK_NO IS NOT NULL
                AND ROWNUM = 1";

            using var command2 = new OracleCommand(sql2, connection);
            command2.Parameters.Add(":email", OracleDbType.Varchar2).Value = email;
            command2.CommandTimeout = 30;

            using var reader2 = await command2.ExecuteReaderAsync();
            string? tcKimlikNo = null;

            if (await reader2.ReadAsync())
            {
                tcKimlikNo = reader2.IsDBNull(0) ? null : reader2.GetString(0);
                // SECURITY: TC Kimlik No is PII - do not log
            }

            if (string.IsNullOrEmpty(tcKimlikNo))
            {
                _logger.LogWarning("TC Kimlik No not found in XXDEU.KULLANICI for email: {Email}", email);
                return null;
            }

            // 3. Adım: TC kimlik no ile V_PERSONEL_EMAIL_BILGI tablosunda ara
            var sql3 = @"
                SELECT
                    ISIM || ' ' || SOYISIM AS AD_SOYAD,
                    EMAIL,
                    BIRIMDEKI_CALISMA_YERI AS DEPARTMAN,
                    UNVANI AS UNVAN,
                    TC_KIMLIK_NO,
                    GOREV_YERI,
                    GOREV_YERI_ADI,
                    CEPTEL,
                    DURUM
                FROM V_PERSONEL_EMAIL_BILGI
                WHERE TC_KIMLIK_NO = :tcKimlikNo                
                AND KURUM NOT <> 999                
                AND ROWNUM = 1";

            using var command3 = new OracleCommand(sql3, connection);
            command3.Parameters.Add(":tcKimlikNo", OracleDbType.Varchar2).Value = tcKimlikNo;
            command3.CommandTimeout = 30;

            using var reader3 = await command3.ExecuteReaderAsync();
            if (await reader3.ReadAsync())
            {
                var rawPhone3 = reader3.IsDBNull(7) ? null : reader3.GetString(7);
                var personel = new PersonelDto
                {
                    AdSoyad = reader3.IsDBNull(0) ? null : reader3.GetString(0),
                    Email = reader3.IsDBNull(1) ? null : reader3.GetString(1),
                    Departman = reader3.IsDBNull(2) ? null : reader3.GetString(2),
                    Unvan = reader3.IsDBNull(3) ? null : reader3.GetString(3),
                    TcKimlikNo = reader3.IsDBNull(4) ? null : reader3.GetString(4),
                    GorevYeri = reader3.IsDBNull(5) ? null : (int?)reader3.GetInt32(5),
                    GorevYeriAdi = reader3.IsDBNull(6) ? null : reader3.GetString(6),
                    CepTel = NormalizePhoneNumber(rawPhone3),
                    Durum = reader3.IsDBNull(8) ? null : (int?)reader3.GetInt32(8)
                };

                // SECURITY: TC Kimlik No is PII - do not log
                _logger.LogInformation("Personel found for email: {Email} -> {AdSoyad}", email, personel.AdSoyad);
                return personel;
            }

            _logger.LogWarning("Personel not found in V_PERSONEL_EMAIL_BILGI for email: {Email}", email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching personel by email {Email} from Oracle 11g", email);
            return null;
        }
    }

    /// <summary>
    /// Cep telefonu numarasını normalize eder
    /// Örnekler: "05551234567" -> "5551234567", "555 123 45 67" -> "5551234567"
    /// </summary>
    private string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Boşlukları, tire ve parantezleri temizle
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();

        // Sadece rakamları al
        cleaned = new string(cleaned.Where(char.IsDigit).ToArray());

        // Boş ise null dön
        if (string.IsNullOrEmpty(cleaned))
            return null;

        // Başında 0 varsa kaldır (05551234567 -> 5551234567)
        if (cleaned.StartsWith("0"))
            cleaned = cleaned.Substring(1);

        // Geçerli uzunluk kontrolü (10 haneli olmalı: 5551234567)
        if (cleaned.Length != 10)
        {
            _logger.LogWarning("Invalid phone number length after normalization: {Phone} (length: {Length})", phoneNumber, cleaned.Length);
            return null;
        }

        return cleaned;
    }
}

/// <summary>
/// Oracle 11g Personel DTO
/// </summary>
public class PersonelDto
{
    public string? AdSoyad { get; set; }
    public string? Email { get; set; }
    public string? Departman { get; set; }
    public string? Unvan { get; set; }
    public string? TcKimlikNo { get; set; }
    public int? GorevYeri { get; set; } // Görev yeri kodu (0=Rektörlük, 500=Mühendislik vb.)
    public string? GorevYeriAdi { get; set; } // Görev yeri adı
    public string? CepTel { get; set; } // Cep telefonu (sadece rakamlar, başında 0 yok: 5551234567)
    public int ? Durum { get; set; } // Personel durumu (1=pasif, 0=Aktif) 
}