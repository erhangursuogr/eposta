using System.Security.Cryptography;
using System.Security;
using System.Text;

namespace DeuEposta.Utils;

public static class SecureEmailCrypto
{
    // Bu anahtar sadece yetkili kişi bilir - defterde yazılı
    private static readonly byte[] MasterKey = Convert.FromBase64String(
        Environment.GetEnvironmentVariable("DEU_EMAIL_MASTER_KEY") ?? 
        "VEhJU19JU19BX1NFQ1VSRV9LRVlfRk9SX0RFVV9FTUFJTF9TWVNURU0xMjM0NTY3ODkwQUJDREVG" // Fallback
    );

    public static string EncryptEmail(string plainEmail)
    {
        if (string.IsNullOrEmpty(plainEmail)) return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = MasterKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainEmail);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // IV + ŞifreliVeri formatında sakla
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            throw new SecurityException($"Email şifreleme hatası: {ex.Message}");
        }
    }

    public static string DecryptEmail(string encryptedEmail)
    {
        if (string.IsNullOrEmpty(encryptedEmail)) return string.Empty;

        try
        {
            var fullCipher = Convert.FromBase64String(encryptedEmail);
            
            using var aes = Aes.Create();
            aes.Key = MasterKey;

            // IV'yi ayır (ilk 16 byte)
            var iv = new byte[16];
            var cipherBytes = new byte[fullCipher.Length - 16];
            
            Array.Copy(fullCipher, 0, iv, 0, 16);
            Array.Copy(fullCipher, 16, cipherBytes, 0, cipherBytes.Length);
            
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new SecurityException($"Email çözme hatası: {ex.Message}");
        }
    }

    // Test/geliştirme için güvenli anahtar üretme
    public static string GenerateSecureMasterKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[32]; // 256-bit anahtar
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    // Email'in şifreli olup olmadığını kontrol et
    public static bool IsEmailEncrypted(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        
        try
        {
            // Base64 formatında mı ve geçerli uzunlukta mı kontrol et
            var bytes = Convert.FromBase64String(email);
            return bytes.Length > 16; // IV (16) + en az 1 byte şifreli veri
        }
        catch
        {
            return false; // Şifreli değil, düz text
        }
    }

    // Güvenlik için email adresini maskele (loglarda kullanım için)
    public static string MaskEmailForLogging(string email)
    {
        if (string.IsNullOrEmpty(email)) return "***";
        
        if (IsEmailEncrypted(email))
        {
            return $"[ENCRYPTED:{email[..8]}...]";
        }
        
        // Normal email için maskeleme
        var parts = email.Split('@');
        if (parts.Length != 2) return "[INVALID_EMAIL]";
        
        var username = parts[0];
        var domain = parts[1];
        
        var maskedUsername = username.Length > 3 
            ? username[..2] + "***" + username[^1..]
            : "***";
            
        return $"{maskedUsername}@{domain}";
    }
}