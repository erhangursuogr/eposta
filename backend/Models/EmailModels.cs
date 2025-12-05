namespace DeuEposta.Models;

// YENİ Email Sistemi İçin DTO'lar

/// <summary>
/// Frontend'de görüntülenecek email seçenekleri (GÜVENLİ)
/// </summary>
public class DisplayEmailOption
{
    public string Id { get; set; } = string.Empty; // UNIQUE_KEY
    public string DisplayName { get; set; } = string.Empty; // "DEÜ Duyuru"
    public string DisplayEmail { get; set; } = string.Empty; // "duyuru@deu.edu.tr"
    public string Description { get; set; } = string.Empty; // "Genel duyurular için"
    public string Category { get; set; } = string.Empty; // FROM, TO, CC için
    public bool Available { get; set; } = true;
}

/// <summary>
/// Listeci grup seçenekleri (Frontend için GÜVENLİ)
/// </summary>
public class ListeciGroupOption
{
    public int GrupId { get; set; }
    public string GoruntuleAdi { get; set; } = string.Empty; // "Akademik Personel Grubu"
    public string EmailKategori { get; set; } = string.Empty; // "AKADEMIK"
    public string Aciklama { get; set; } = string.Empty;
    public int UyeSayisi { get; set; } // Tahmini üye sayısı
    public bool Aktif { get; set; } = true;
    // LİSTECİ EMAIL ADRESİ ASLA EKLENMEDİ! 🔒
}

/// <summary>
/// Backend'de email gönderimi için tam bilgi (GİZLİ)
/// </summary>
public class EmailSenderConfig
{
    // Display bilgileri (kullanıcı bunu görür)
    public string FromDisplayName { get; set; } = string.Empty; // "DEÜ Rektörlüğü"

    public string FromDisplayEmail { get; set; } = string.Empty; // "rektor@deu.edu.tr"

    // Gerçek gönderici (sistem kullanır, GİZLİ)
    public string ActualSenderEmail { get; set; } = string.Empty; // "kiraz_akd44@deu.edu.tr"

    public string ActualSenderName { get; set; } = string.Empty; // "DEÜ Duyuru Sistemi"

    // TO/CC (görünür)
    public List<string> ToEmails { get; set; } = new();

    public List<string> CcEmails { get; set; } = new();

    // BCC (tamamen gizli - listeci emailler)
    public List<string> BccEmails { get; set; } = new();

    // Reply-To ayarı
    public string ReplyToEmail { get; set; } = string.Empty;
}

/// <summary>
/// Email gönderim sonucu
/// </summary>
public class EmailSendResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public int SuccessfulSends { get; set; }
    public int FailedSends { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime SentAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Email duyuru oluşturma isteği (Frontend'den gelir)
/// </summary>
public class CreateEmailAnnouncementRequest
{
    // Email ayarları (display)
    public string FromDisplayId { get; set; } = string.Empty; // "REKTOR_DISPLAY"

    public List<string> ToDisplayIds { get; set; } = new(); // ["DUYURU_DISPLAY"]
    public List<string> CcDisplayIds { get; set; } = new(); // ["BILGI_ISLEM_DISPLAY"]

    // Grup seçimleri (BCC için)
    public List<int> ListeciGrupIds { get; set; } = new(); // [1, 2] -> akademik + idari

    public List<string> BireyselEmails { get; set; } = new(); // Manuel eklenenler

    // İçerik
    public string Subject { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;

    // Zamanlama
    public DateTime? ScheduledAt { get; set; }

    public bool SendImmediately { get; set; } = false;
}