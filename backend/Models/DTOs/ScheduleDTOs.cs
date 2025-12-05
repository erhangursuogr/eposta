using System.ComponentModel.DataAnnotations;

namespace DeuEposta.Models.DTOs;

/// <summary>
/// Zamanlama oluşturma isteği
/// </summary>
public class CreateScheduleRequest
{
    [Required(ErrorMessage = "Duyuru ID zorunludur")]
    public int DuyuruId { get; set; }

    [Required(ErrorMessage = "Zamanlanan tarih zorunludur")]
    public DateTime ZamanlanmaTarihi { get; set; }
}

/// <summary>
/// Toplu zamanlama oluşturma (örn: 1 ay boyunca her 5 günde bir)
/// </summary>
public class CreateBulkScheduleRequest
{
    [Required(ErrorMessage = "Duyuru ID zorunludur")]
    public int DuyuruId { get; set; }

    /// <summary>
    /// İlk gönderim tarihi
    /// </summary>
    [Required(ErrorMessage = "Başlangıç tarihi zorunludur")]
    public DateTime BaslangicTarihi { get; set; }

    /// <summary>
    /// Son gönderim tarihi
    /// </summary>
    [Required(ErrorMessage = "Bitiş tarihi zorunludur")]
    public DateTime BitisTarihi { get; set; }

    /// <summary>
    /// Kaç günde bir tekrar edeceği (örn: 5 günde bir için 5)
    /// </summary>
    [Required(ErrorMessage = "Tekrar aralığı zorunludur")]
    [Range(1, 365, ErrorMessage = "Tekrar aralığı 1-365 gün arasında olmalıdır")]
    public int TekrarGunAraligi { get; set; }
}

/// <summary>
/// Zamanlama iptal isteği
/// </summary>
public class CancelScheduleRequest
{
    [MaxLength(500, ErrorMessage = "İptal notu 500 karakteri geçemez")]
    public string? IptalNotu { get; set; }
}

/// <summary>
/// Zamanlama detay view
/// </summary>
public class ScheduleDetailView
{
    public int Id { get; set; }
    public int DuyuruId { get; set; }
    public string? Konu { get; set; }
    public DateTime ZamanlanmaTarihi { get; set; }
    public string Durum { get; set; } = string.Empty;
    public DateTime? GonderimTarihi { get; set; }
    public string? HangfireJobId { get; set; }
    public string? HataMesaji { get; set; }
    public int AliciSayisi { get; set; }
    public int OlusturanKullaniciId { get; set; }
    public string? OlusturanKullaniciAdi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    public string? IptalNotu { get; set; }
}

/// <summary>
/// Zamanlama liste view
/// </summary>
public class ScheduleListView
{
    public int Id { get; set; }
    public int DuyuruId { get; set; }
    public string? Konu { get; set; }
    public DateTime ZamanlanmaTarihi { get; set; }
    public string Durum { get; set; } = string.Empty;
    public DateTime? GonderimTarihi { get; set; }
    public int AliciSayisi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}

/// <summary>
/// Zamanlama istatistikleri
/// </summary>
public class ScheduleStatistics
{
    public int TotalSchedules { get; set; }
    public int PendingSchedules { get; set; }
    public int SentSchedules { get; set; }
    public int CancelledSchedules { get; set; }
    public int FailedSchedules { get; set; }
    public DateTime? NextScheduledDate { get; set; }
}

/// <summary>
/// Zamanlama response DTO
/// </summary>
public class ScheduleResponse
{
    public int Id { get; set; }
    public int DuyuruId { get; set; }
    public string? Konu { get; set; }
    public DateTime ZamanlanmaTarihi { get; set; }
    public string Durum { get; set; } = string.Empty;
    public DateTime? GonderimTarihi { get; set; }
    public int AliciSayisi { get; set; }
    public string? HangfireJobId { get; set; }
    public string? HataMesaji { get; set; }
    public string? IptalNotu { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}