using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

public interface ILogService
{
    Task<ResponseDataModel<PagedLogResponse<LoginLogDto>>> GetLoginLogsAsync(LoginLogFilterRequest request);

    Task<ResponseDataModel<PagedLogResponse<SystemLogDto>>> GetSystemLogsAsync(SystemLogFilterRequest request);

    Task<ResponseDataModel<PagedLogResponse<EmailLogDto>>> GetEmailLogsAsync(EmailLogFilterRequest request);
}

public class LogService : ILogService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<LogService> _logger;

    public LogService(DeuEpostaContext context, ILogger<LogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResponseDataModel<PagedLogResponse<LoginLogDto>>> GetLoginLogsAsync(LoginLogFilterRequest request)
    {
        try
        {
            var query = _context.LogLogin
                .Include(l => l.Kullanici)
                .AsQueryable();

            // Tarih filtresi
            if (request.BaslangicTarihi.HasValue)
            {
                query = query.Where(l => l.GirisTarihi >= request.BaslangicTarihi.Value);
            }

            if (request.BitisTarihi.HasValue)
            {
                var bitisTarihiSonSaat = request.BitisTarihi.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.GirisTarihi <= bitisTarihiSonSaat);
            }

            // Sadece başarısız girişler
            if (request.SadeceBasarisiz.HasValue && request.SadeceBasarisiz.Value)
            {
                query = query.Where(l => l.Basarili == "N");
            }

            // Giriş türü filtresi
            if (!string.IsNullOrEmpty(request.GirisTuru))
            {
                query = query.Where(l => l.GirisTuru == request.GirisTuru);
            }

            // Genel arama (email, kullanıcı adı, IP)
            if (!string.IsNullOrEmpty(request.Arama))
            {
                var aramaLower = request.Arama.ToLower();
                query = query.Where(l =>
                    EF.Functions.Like(l.Email ?? "", $"%{aramaLower}%") ||
                    EF.Functions.Like(l.KullaniciAdi ?? "", $"%{aramaLower}%") ||
                    EF.Functions.Like(l.IpAdres ?? "", $"%{aramaLower}%")
                );
            }

            // Toplam kayıt sayısı
            var toplamKayit = await query.CountAsync();

            // Sıralama ve sayfalama
            var logsRaw = await query
                .OrderByDescending(l => l.GirisTarihi)
                .Skip((request.Sayfa - 1) * request.SayfaBoyutu)
                .Take(request.SayfaBoyutu)
                .ToListAsync();

            var logs = logsRaw.Select(l => new LoginLogDto
            {
                Id = l.Id,
                KullaniciId = l.KullaniciId,
                KullaniciAdi = l.KullaniciAdi,
                Email = l.Email,
                IpAdres = l.IpAdres,
                UserAgent = l.UserAgent,
                GirisTuru = l.GirisTuru,
                Basarili = l.Basarili == "Y",
                HataMesaji = l.HataMesaji,
                GirisTarihi = l.GirisTarihi
            }).ToList();

            var response = new PagedLogResponse<LoginLogDto>
            {
                Items = logs,
                ToplamKayit = toplamKayit,
                Sayfa = request.Sayfa,
                SayfaBoyutu = request.SayfaBoyutu
            };

            return ResponseDataModel<PagedLogResponse<LoginLogDto>>.SuccessResult(response, "Login logları alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching login logs");
            return ResponseDataModel<PagedLogResponse<LoginLogDto>>.ErrorResult("Login logları alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<PagedLogResponse<SystemLogDto>>> GetSystemLogsAsync(SystemLogFilterRequest request)
    {
        try
        {
            var query = _context.LogSistem
                .Include(l => l.Kullanici)
                .AsQueryable();

            // Tarih filtresi
            if (request.BaslangicTarihi.HasValue)
            {
                query = query.Where(l => l.LogTarihi >= request.BaslangicTarihi.Value);
            }

            if (request.BitisTarihi.HasValue)
            {
                var bitisTarihiSonSaat = request.BitisTarihi.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.LogTarihi <= bitisTarihiSonSaat);
            }

            // Log seviye filtresi (WARNING/WARN, CRITICAL/FATAL eşleştirmeli)
            if (!string.IsNullOrEmpty(request.LogSeviye))
            {
                var seviye = request.LogSeviye.ToUpper();
                query = query.Where(l =>
                    l.LogSeviye == seviye ||
                    (seviye == "WARNING" && l.LogSeviye == "WARN") ||
                    (seviye == "WARN" && l.LogSeviye == "WARNING") ||
                    (seviye == "CRITICAL" && l.LogSeviye == "FATAL") ||
                    (seviye == "FATAL" && l.LogSeviye == "CRITICAL")
                );
            }

            // Sadece hatalı loglar (ERROR, WARN)
            if (request.SadeceHata.HasValue && request.SadeceHata.Value)
            {
                query = query.Where(l => l.LogSeviye == "ERROR" || l.LogSeviye == "WARN");
            }

            // Kategori filtresi
            if (!string.IsNullOrEmpty(request.Kategori))
            {
                query = query.Where(l => l.Kategori == request.Kategori);
            }

            // Genel arama (kullanıcı adı, işlem, detay, IP)
            if (!string.IsNullOrEmpty(request.Arama))
            {
                var aramaLower = request.Arama.ToLower();
                query = query.Where(l =>
                    (l.Kullanici != null && EF.Functions.Like(l.Kullanici.AdSoyad ?? "", $"%{aramaLower}%")) ||
                    EF.Functions.Like(l.Islem ?? "", $"%{aramaLower}%") ||
                    EF.Functions.Like(l.Detay ?? "", $"%{aramaLower}%") ||
                    EF.Functions.Like(l.IpAdres ?? "", $"%{aramaLower}%")
                );
            }

            // Toplam kayıt sayısı
            var toplamKayit = await query.CountAsync();

            // Sıralama ve sayfalama
            var logs = await query
                .OrderByDescending(l => l.LogTarihi)
                .Skip((request.Sayfa - 1) * request.SayfaBoyutu)
                .Take(request.SayfaBoyutu)
                .Select(l => new SystemLogDto
                {
                    Id = l.Id,
                    KullaniciId = l.KullaniciId,
                    KullaniciAdi = l.Kullanici != null ? l.Kullanici.AdSoyad : null,
                    LogSeviye = l.LogSeviye,
                    Kategori = l.Kategori,
                    Islem = l.Islem,
                    Detay = l.Detay,
                    IpAdres = l.IpAdres,
                    UserAgent = l.UserAgent,
                    LogTarihi = l.LogTarihi
                })
                .ToListAsync();

            var response = new PagedLogResponse<SystemLogDto>
            {
                Items = logs,
                ToplamKayit = toplamKayit,
                Sayfa = request.Sayfa,
                SayfaBoyutu = request.SayfaBoyutu
            };

            return ResponseDataModel<PagedLogResponse<SystemLogDto>>.SuccessResult(response, "Sistem logları alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching system logs");
            return ResponseDataModel<PagedLogResponse<SystemLogDto>>.ErrorResult("Sistem logları alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<PagedLogResponse<EmailLogDto>>> GetEmailLogsAsync(EmailLogFilterRequest request)
    {
        try
        {
            var query = _context.EpostaDuyuruGonderimLoglari
                .Include(l => l.Duyuru)
                .AsQueryable();

            // Tarih filtresi
            if (request.BaslangicTarihi.HasValue)
            {
                query = query.Where(l => l.GonderimTarihi >= request.BaslangicTarihi.Value);
            }

            if (request.BitisTarihi.HasValue)
            {
                var bitisTarihiSonSaat = request.BitisTarihi.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.GonderimTarihi <= bitisTarihiSonSaat);
            }

            // Duyuru ID filtresi
            if (request.DuyuruId.HasValue)
            {
                query = query.Where(l => l.DuyuruId == request.DuyuruId.Value);
            }

            // Sadece başarısız gönderimler
            if (request.SadeceBasarisiz.HasValue && request.SadeceBasarisiz.Value)
            {
                query = query.Where(l => l.GonderimDurumu == "BASARISIZ");
            }

            // Genel arama (duyuru konusu, alıcı email, ad soyad)
            if (!string.IsNullOrEmpty(request.Arama))
            {
                var aramaLower = request.Arama.ToLower();
                query = query.Where(l =>
                    EF.Functions.Like(l.AliciEmail ?? "", $"%{aramaLower}%") ||
                    EF.Functions.Like(l.AliciAdSoyad ?? "", $"%{aramaLower}%") ||
                    (l.Duyuru != null && EF.Functions.Like(l.Duyuru.Konu ?? "", $"%{aramaLower}%"))
                );
            }

            // Toplam kayıt sayısı
            var toplamKayit = await query.CountAsync();

            // Sıralama ve sayfalama
            var logsRaw = await query
                .OrderByDescending(l => l.GonderimTarihi)
                .Skip((request.Sayfa - 1) * request.SayfaBoyutu)
                .Take(request.SayfaBoyutu)
                .ToListAsync();

            var logs = logsRaw.Select(l => new EmailLogDto
            {
                Id = l.Id,
                DuyuruId = l.DuyuruId,
                DuyuruKonu = l.Duyuru != null ? l.Duyuru.Konu : null,
                AliciEmail = l.AliciEmail,
                AliciAdSoyad = l.AliciAdSoyad,
                AliciKategorisi = l.AliciKategorisi,
                GonderimBasarili = l.GonderimDurumu == "BASARILI",
                HataMesaji = l.HataMesaji,
                GonderimTarihi = l.GonderimTarihi
            }).ToList();

            var response = new PagedLogResponse<EmailLogDto>
            {
                Items = logs,
                ToplamKayit = toplamKayit,
                Sayfa = request.Sayfa,
                SayfaBoyutu = request.SayfaBoyutu
            };

            return ResponseDataModel<PagedLogResponse<EmailLogDto>>.SuccessResult(response, "Email gönderim logları alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching email logs");
            return ResponseDataModel<PagedLogResponse<EmailLogDto>>.ErrorResult("Email logları alınırken hata oluştu", 500);
        }
    }
}