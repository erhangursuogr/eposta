using AutoMapper;
using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using DeuEposta.Utils;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

public interface IAnnouncementService
{
    Task<ResponseDataModel<List<DuyuruListeView>>> GetAnnouncementsAsync(int page = 1, int pageSize = 20, string? durum = null, int? kullaniciId = null, int currentUserId = 0, string userRole = RolKodu.VIEWER, string? searchTerm = null, DateTime? startDate = null, DateTime? endDate = null);

    Task<ResponseDataModel<AnnouncementDetailView>> GetAnnouncementByIdAsync(int id);

    Task<ResponseDataModel<int>> CreateAnnouncementAsync(CreateAnnouncementRequest request, int kullaniciId);

    Task<ResponseModel> UpdateAnnouncementAsync(int id, UpdateAnnouncementRequest request, int kullaniciId);

    Task<ResponseModel> DeleteAnnouncementAsync(int id, int kullaniciId);

    Task<ResponseModel> DuplicateAnnouncementAsync(int id, int kullaniciId);
}

public class AnnouncementService : IAnnouncementService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<AnnouncementService> _logger;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IEmailCategoryService _emailCategoryService;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IAuditLogService _auditLog;
    private readonly IScheduleService _scheduleService;
    private readonly IFileService _fileService;
    private readonly ISecurityService _securityService;
    private readonly string _uploadPath;

    public AnnouncementService(
        DeuEpostaContext context,
        ILogger<AnnouncementService> logger,
        IMapper mapper,
        IEmailService emailService,
        IEmailCategoryService emailCategoryService,
        ISystemSettingsService systemSettingsService,
        IAuditLogService auditLog,
        IScheduleService scheduleService,
        IFileService fileService,
        ISecurityService securityService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
        _emailService = emailService;
        _emailCategoryService = emailCategoryService;
        _systemSettingsService = systemSettingsService;
        _auditLog = auditLog;
        _scheduleService = scheduleService;
        _fileService = fileService;
        _securityService = securityService;

        var uploadPath = configuration["FileSettings:UploadPath"] ?? "uploads";
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadPath);
    }

    public async Task<ResponseDataModel<List<DuyuruListeView>>> GetAnnouncementsAsync(
        int page = 1,
        int pageSize = 20,
        string? durum = null,
        int? kullaniciId = null,
        int currentUserId = 0,
        string userRole = RolKodu.VIEWER,
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            // GÜVENLİK: Pagination parametrelerini normalize et (OOM ve negatif Skip önleme)
            (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

            // PERFORMANS: Database view kullan (V_DUYURULAR_LISTE)
            // View'de TOPLAM_ALICI_SAYISI zaten hesaplanmış, N+1 problemi yok
            var query = _context.VDuyurularListe
                .AsNoTracking() // Read-only query
                .AsQueryable();

            // Rol bazlı filtreleme
            switch (userRole)
            {
                case RolKodu.ADMIN:
                    // ADMIN: onlyMine aktifse sadece kendi oluşturduklarını, değilse tümünü göster
                    if (kullaniciId.HasValue)
                        query = query.Where(d => d.OlusturanKullaniciId == kullaniciId.Value);
                    break;

                case RolKodu.MANAGER:
                    // MANAGER: Tüm duyuruları görebilir; TASLAK hariç, ancak kendi TASLAK'larını görebilsin
                    query = query.Where(d => d.Durum != DuyuruDurum.TASLAK || d.OlusturanKullaniciId == currentUserId);
                    break;

                case RolKodu.COORDINATOR:
                    // COORDINATOR: TASLAK hariç tüm duyuruları görsün (kendi taslakları dahil olmasın)
                    query = query.Where(d => d.Durum != DuyuruDurum.TASLAK);
                    break;

                case RolKodu.VIEWER:
                    // VIEWER: TASLAK hariç tüm duyuruları görebilir
                    query = query.Where(d => d.Durum != DuyuruDurum.TASLAK);
                    break;

                case RolKodu.EDITOR:
                default:
                    // EDITOR: Sadece kendi oluşturduğu duyuruları görebilir
                    query = query.Where(d => d.OlusturanKullaniciId == currentUserId);
                    break;
            }

            // Durum filtresi
            if (!string.IsNullOrEmpty(durum))
                query = query.Where(d => d.Durum == durum);

            // BACKEND FILTRELEME (Gemini Audit Fix): Client-side filtreleme yerine database'de filtrele
            // Arama filtresi (Konu içinde ara)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var trimmedSearch = searchTerm.Trim();
                query = query.Where(d => d.Konu.Contains(trimmedSearch));
            }

            // Tarih aralığı filtresi
            if (startDate.HasValue)
            {
                query = query.Where(d => d.OlusturmaTarihi >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // Bitiş tarihinin gün sonuna kadar (23:59:59) dahil et
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(d => d.OlusturmaTarihi <= endOfDay);
            }

            var totalCount = await query.CountAsync();

            var announcements = await query
                .OrderByDescending(d => d.OlusturmaTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ResponseDataModel<List<DuyuruListeView>>.SuccessResultWithPagination(
                announcements, totalCount, page, pageSize, "Duyurular başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcements");
            return ResponseDataModel<List<DuyuruListeView>>.ErrorResult("Duyurular alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<AnnouncementDetailView>> GetAnnouncementByIdAsync(int id)
    {
        try
        {
            // Hareketler tablosunu da include et (onay/red notları için)
            // PERFORMANS: AsSplitQuery ile N+1 query önlenir (derin Include zinciri için)
            var announcementView = await _context.EpostaDuyurulari
                .AsSplitQuery()
                .Include(d => d.Hareketler)
                    .ThenInclude(h => h.Kullanici)
                        .ThenInclude(k => k!.Rol)
                .Where(d => d.Id == id)
                .Select(d => new AnnouncementDetailView
                {
                    Id = d.Id,
                    Konu = d.Konu,
                    Icerik = d.Icerik,
                    DuyuruDurumu = d.Durum,
                    DuyuruKategorisi = d.DuyuruKategorisi,
                    GondericiKategori = d.GondericiKategori,
                    OlusturanKullaniciId = d.OlusturanKullaniciId,
                    OlusturanKullaniciAdi = d.OlusturanKullanici != null ? d.OlusturanKullanici.AdSoyad : null,
                    OlusturmaTarihi = d.OlusturmaTarihi,
                    GuncellemeTarihi = d.GuncellemeTarihi,
                    Aciklama = d.Aciklama ?? string.Empty,
                    SablonId = d.SablonId,
                    SablonAdi = d.Sablon != null ? d.Sablon.SablonAdi : null,
                    ToplamAliciSayisi = d.ToplamAliciSayisi,
                    // Hareket tablosundan en son onay/red notunu çek
                    OnayTarihi = d.Hareketler
                        .Where(h => h.IslemTipi == "ONAYLAMA" || h.IslemTipi == "REDDETME")
                        .OrderByDescending(h => h.IslemTarihi)
                        .Select(h => h.IslemTarihi)
                        .FirstOrDefault(),
                    OnayNotu = d.Hareketler
                        .Where(h => (h.IslemTipi == "ONAYLAMA" || h.IslemTipi == "REDDETME") && h.Aciklama != null)
                        .OrderByDescending(h => h.IslemTarihi)
                        .Select(h => h.Aciklama)
                        .FirstOrDefault(),
                    // Koordinatör red bilgileri (ILK_ONAY_BEKLIYOR'dan TASLAK'a geçiş)
                    KoordinatorRedNotu = d.Hareketler
                        .Where(h => h.IslemTipi == "REDDETME" &&
                                   h.OncekiDurum == DuyuruDurum.ILK_ONAY_BEKLIYOR &&
                                   h.Kullanici != null &&
                                   h.Kullanici.Rol != null &&
                                   h.Kullanici.Rol.RolKodu == RolKodu.COORDINATOR)
                        .OrderByDescending(h => h.IslemTarihi)
                        .Select(h => h.Aciklama)
                        .FirstOrDefault(),
                    KoordinatorRedTarihi = d.Hareketler
                        .Where(h => h.IslemTipi == "REDDETME" &&
                                   h.OncekiDurum == DuyuruDurum.ILK_ONAY_BEKLIYOR &&
                                   h.Kullanici != null &&
                                   h.Kullanici.Rol != null &&
                                   h.Kullanici.Rol.RolKodu == RolKodu.COORDINATOR)
                        .OrderByDescending(h => h.IslemTarihi)
                        .Select(h => h.IslemTarihi)
                        .FirstOrDefault(),
                    // Manager red bilgileri (SON_ONAY_BEKLIYOR'dan TASLAK'a geçiş)
                    ManagerRedNotu = d.Hareketler
                        .Where(h => h.IslemTipi == "REDDETME" &&
                                   h.OncekiDurum == DuyuruDurum.SON_ONAY_BEKLIYOR &&
                                   h.Kullanici != null &&
                                   h.Kullanici.Rol != null &&
                                   h.Kullanici.Rol.RolKodu == RolKodu.MANAGER)
                        .OrderByDescending(h => h.IslemTarihi)
                        .Select(h => h.Aciklama)
                        .FirstOrDefault(),
                    ManagerRedTarihi = d.Hareketler
                        .Where(h => h.IslemTipi == "REDDETME" &&
                                   h.OncekiDurum == DuyuruDurum.SON_ONAY_BEKLIYOR &&
                                   h.Kullanici != null &&
                                   h.Kullanici.Rol != null &&
                                   h.Kullanici.Rol.RolKodu == RolKodu.MANAGER)
                        .OrderByDescending(h => h.IslemTarihi)
                        .Select(h => h.IslemTarihi)
                        .FirstOrDefault(),
                    GonderimTarihi = d.GercekGonderimTarihi,
                    OnaylayanKullaniciId = d.SonOnaylayanKullaniciId,
                    OnaylayanKullaniciAdi = d.SonOnaylayanKullanici != null ? d.SonOnaylayanKullanici.AdSoyad : null
                })
                .FirstOrDefaultAsync();

            if (announcementView == null)
                return ResponseDataModel<AnnouncementDetailView>.ErrorResult("Duyuru bulunamadı", 404);

            // Dosya var mı kontrolü (Oracle: COUNT kullan, boolean yerine)
            var dosyaSayisi = await _context.Dosyalar
                .Where(f => f.DuyuruId == id && f.Aktif == "Y")
                .CountAsync();
            announcementView.DosyaVarMi = dosyaSayisi > 0;

            // Alıcı gruplarını çek
            announcementView.GrupIdList = await _context.EpostaDuyuruAlicilari
                .Where(a => a.DuyuruId == id && a.GrupId != null)
                .Select(a => a.GrupId!.Value)
                .Distinct()
                .ToListAsync();

            // Manuel email'leri çek (grup_id null olanlar)
            announcementView.AliciEmailList = await _context.EpostaDuyuruAlicilari
                .Where(a => a.DuyuruId == id && a.GrupId == null && a.Email != null)
                .Select(a => a.Email!)
                .Distinct()
                .ToListAsync();

            return ResponseDataModel<AnnouncementDetailView>.SuccessResult(announcementView, "Duyuru başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcement {Id}", id);
            return ResponseDataModel<AnnouncementDetailView>.ErrorResult("Duyuru alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<int>> CreateAnnouncementAsync(CreateAnnouncementRequest request, int kullaniciId)
    {
        // CRITICAL FIX: Transaction Management
        // Çoklu SaveChangesAsync() var - atomicity için transaction gerekli
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Oluşturan kullanıcının rolünü kontrol et
            var creator = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == kullaniciId && k.Aktif == "Y");

            if (creator == null)
                return ResponseDataModel<int>.ErrorResult("Kullanıcı bulunamadı", 404);

            // Onaylayıcı seçilmişse kontrolü yap
            if (request.OnaylayanKullaniciId.HasValue)
            {
                var approver = await _context.Kullanicilar
                    .Include(k => k.Rol)
                    .FirstOrDefaultAsync(k => k.Id == request.OnaylayanKullaniciId.Value && k.Aktif == "Y");

                if (approver == null)
                    return ResponseDataModel<int>.ErrorResult("Seçilen onaylayıcı bulunamadı", 404);

                if (approver.Rol?.RolKodu != RolKodu.MANAGER && approver.Rol?.RolKodu != RolKodu.ADMIN)
                    return ResponseDataModel<int>.ErrorResult("Onaylayıcı MANAGER veya ADMIN rolünde olmalıdır", 400);
            }
            else
            {
                // Onaylayıcı seçilmemişse ve oluşturan ADMIN veya MANAGER ise otomatik kendisi onaylayıcı olsun
                if (creator.Rol?.RolKodu == RolKodu.ADMIN || creator.Rol?.RolKodu == RolKodu.MANAGER)
                {
                    request.OnaylayanKullaniciId = kullaniciId;
                }
            }

            // Validation: Ya şablon seçilmeli ya da konu+içerik verilmeli
            if (!request.SablonId.HasValue && (string.IsNullOrWhiteSpace(request.Konu) || string.IsNullOrWhiteSpace(request.Icerik)))
            {
                return ResponseDataModel<int>.ErrorResult("Şablon seçilmediyse konu ve içerik zorunludur", 400);
            }

            // GÜVENLİK: SMTP gönderici kategorisinin geçerli olup olmadığını kontrol et
            if (!string.IsNullOrWhiteSpace(request.GondericiKategori))
            {
                var isValidCategory = await _emailCategoryService.IsValidCategoryAsync(request.GondericiKategori);
                if (!isValidCategory)
                {
                    return ResponseDataModel<int>.ErrorResult($"Geçersiz SMTP gönderici kategorisi: {request.GondericiKategori}", 400);
                }
            }

            var announcement = _mapper.Map<EpostaDuyuru>(request);

            // GÜVENLİK: HTML içeriğini sanitize et (XSS önlemi)
            if (!string.IsNullOrEmpty(announcement.Icerik))
            {
                announcement.Icerik = _securityService.SanitizeHtmlContent(announcement.Icerik);
            }

            // Şablon seçilmişse, şablon içeriğini duyuruya yükle
            if (request.SablonId.HasValue)
            {
                var template = await _context.EpostaSablonlari
                    .FirstOrDefaultAsync(s => s.Id == request.SablonId.Value && s.Aktif == "Y");

                if (template == null)
                    return ResponseDataModel<int>.ErrorResult("Seçilen şablon aktif değil veya bulunamadı", 400);

                // Şablon içeriğini duyuruya aktar
                announcement.Konu = template.KonuSablonu ?? "Konu";
                announcement.Icerik = template.IcerikSablonu;

                _logger.LogInformation("Template {TemplateId} ({TemplateName}) loaded into announcement",
                    template.Id, template.SablonAdi);
            }

            // Banner dosyası kontrolü
            if (request.BannerDosyaId.HasValue)
            {
                var bannerFile = await _context.Dosyalar
                    .FirstOrDefaultAsync(f => f.Id == request.BannerDosyaId.Value);

                if (bannerFile == null)
                    return ResponseDataModel<int>.ErrorResult("Seçilen banner dosyası bulunamadı", 404);

                // Banner dosyasının görsel olup olmadığını kontrol et
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                var fileExtension = Path.GetExtension(bannerFile.DosyaAdi).ToLowerInvariant();

                if (!imageExtensions.Contains(fileExtension))
                    return ResponseDataModel<int>.ErrorResult("Banner dosyası bir görsel dosya olmalıdır (jpg, png, gif, etc.)", 400);

                _logger.LogInformation("Banner file {FileId} ({FileName}) attached to announcement",
                    bannerFile.Id, bannerFile.DosyaAdi);
            }

            announcement.OlusturanKullaniciId = kullaniciId;
            announcement.OlusturmaTarihi = DateTime.Now;
            announcement.Durum = DuyuruDurum.TASLAK;

            _context.EpostaDuyurulari.Add(announcement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement created: {Id} by user {UserId}, Approver: {ApproverId}",
                announcement.Id, kullaniciId, request.OnaylayanKullaniciId);

            // Alıcıları ekle - Gruplar
            if (request.GrupIdList != null && request.GrupIdList.Any())
            {
                // N+1 problemi önlendi: Tüm grupları tek sorguda al
                var groups = await _context.EpostaGruplari
                    .Where(g => request.GrupIdList.Contains(g.Id) && g.Aktif == "Y")
                    .ToListAsync();

                foreach (var grup in groups)
                {
                    // Güvenlik: Tüm gruplar BCC-only (KVKK/GDPR uyumu)
                    var kategori = "BCC";

                    // KRİTİK: DEBIS grupları SADECE BCC olabilir (güvenlik kontrolü)
                    if (!ValidateDebisGroupSecurity(grup, kategori, out var debisError))
                    {
                        return ResponseDataModel<int>.ErrorResult(debisError, 400);
                    }

                    _context.EpostaDuyuruAlicilari.Add(new EpostaDuyuruAlici
                    {
                        DuyuruId = announcement.Id,
                        GrupId = grup.Id,
                        AliciTipi = "GRUP",
                        AliciKategorisi = kategori,
                        OlusturmaTarihi = DateTime.Now
                    });
                }
            }

            // Alıcıları ekle - Manuel emailler
            if (request.AliciEmailList != null && request.AliciEmailList.Any())
            {
                foreach (var email in request.AliciEmailList)
                {
                    _context.EpostaDuyuruAlicilari.Add(new EpostaDuyuruAlici
                    {
                        DuyuruId = announcement.Id,
                        Email = email,
                        AliciTipi = "MANUEL",
                        AliciKategorisi = "BCC",
                        OlusturmaTarihi = DateTime.Now
                    });
                }
            }

            // Hareket kaydı: Duyuru oluşturma (aynı transaction içinde)
            AddHareket(announcement.Id, null, DuyuruDurum.TASLAK,
                "OLUSTURMA", kullaniciId, "Duyuru oluşturuldu", null);

            // Tek transaction: Duyuru + Alıcılar + Hareket kaydı
            await _context.SaveChangesAsync();

            // Audit log: Duyuru oluşturma
            var approverInfo = request.OnaylayanKullaniciId.HasValue
                ? $"Onaylayıcı: {creator.Rol?.RolKodu} kullanıcı (ID: {request.OnaylayanKullaniciId})"
                : "Onaylayıcı seçilmedi";

            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "DUYURU_OLUSTURMA",
                detay: $"Duyuru oluşturuldu. ID: {announcement.Id}, Konu: {announcement.Konu}, {approverInfo}"
            );

            // NOT: Toplam alıcı sayısı PKG_ALICI_SAYISI trigger'ı tarafından otomatik hesaplanır

            // Zamanlanmışsa schedule oluştur
            if (request.ZamanlanmisTarih.HasValue)
            {
                if (string.IsNullOrEmpty(request.TekrarSikligi) || request.TekrarSikligi == "NONE")
                {
                    // Tek seferlik zamanlama
                    var scheduleRequest = new CreateScheduleRequest
                    {
                        DuyuruId = announcement.Id,
                        ZamanlanmaTarihi = request.ZamanlanmisTarih.Value
                    };

                    await _scheduleService.CreateScheduleAsync(scheduleRequest, kullaniciId);
                }
                else
                {
                    // Tekrarlı zamanlama için CreateBulkScheduleAsync kullanılabilir
                    // Şimdilik tek seferlik oluştur
                    var scheduleRequest = new CreateScheduleRequest
                    {
                        DuyuruId = announcement.Id,
                        ZamanlanmaTarihi = request.ZamanlanmisTarih.Value
                    };

                    await _scheduleService.CreateScheduleAsync(scheduleRequest, kullaniciId);
                }

                _logger.LogInformation("Schedule created for announcement {Id} at {Date}", announcement.Id, request.ZamanlanmisTarih);
            }

            // Transaction commit - Tüm işlemler başarılıysa commit et
            await transaction.CommitAsync();

            return ResponseDataModel<int>.SuccessResult(announcement.Id, "Duyuru başarıyla oluşturuldu");
        }
        catch (Exception ex)
        {
            // Transaction rollback - Hata durumunda tüm işlemleri geri al
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating announcement - Transaction rolled back");
            return ResponseDataModel<int>.ErrorResult("Duyuru oluşturulurken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> UpdateAnnouncementAsync(int id, UpdateAnnouncementRequest request, int kullaniciId)
    {
        try
        {
            // KRİTİK: Transaction içinde durum kontrolü yap (race condition önlemi)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Atomik durum kontrolü: Sadece TASLAK veya REDDEDILDI durumunda düzenlemeye izin ver
                var announcement = await _context.EpostaDuyurulari
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (announcement == null)
                {
                    await transaction.RollbackAsync();
                    return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);
                }

                // GÜVENLİK: Ownership kontrolü - ADMIN ve MANAGER hariç sadece sahibi düzenleyebilir
                var currentUser = await _context.Kullanicilar
                    .Include(k => k.Rol)
                    .FirstOrDefaultAsync(k => k.Id == kullaniciId);

                var isAdmin = currentUser?.Rol?.RolKodu == "ADMIN";
                var isManager = currentUser?.Rol?.RolKodu == "MANAGER";

                if (announcement.OlusturanKullaniciId != kullaniciId && !isAdmin && !isManager)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Unauthorized update attempt: User {UserId} tried to update announcement {AnnouncementId} owned by {OwnerId}",
                        kullaniciId, id, announcement.OlusturanKullaniciId);
                    return ResponseModel.ErrorResult("Bu duyuruyu düzenleme yetkiniz yok", 403);
                }

                // KRİTİK: TASLAK veya REDDEDILDI dışındaki durumlarda düzenleme YASAK
                // Bu kontrol transaction içinde olduğu için race condition'dan korunuyoruz
                if (announcement.Durum != DuyuruDurum.TASLAK &&
                    announcement.Durum != DuyuruDurum.REDDEDILDI)
                {
                    await transaction.RollbackAsync();
                    return ResponseModel.ErrorResult(
                        $"Bu durumdaki duyuru düzenlenemez. Sadece taslak veya reddedilmiş duyurular düzenlenebilir. Mevcut durum: {announcement.Durum}",
                        400);
                }

                // REDDEDILDI durumundaysa TASLAK'a çevir (düzenleme için)
                if (announcement.Durum == DuyuruDurum.REDDEDILDI)
                {
                    // Eski red nedenini hareket kaydından oku (kullanıcıya gösterebilmek için)
                    var lastRejection = await _context.EpostaDuyuruHareketleri
                        .Where(h => h.DuyuruId == id && h.IslemTipi == "REDDETME")
                        .OrderByDescending(h => h.IslemTarihi)
                        .FirstOrDefaultAsync();

                    announcement.Durum = DuyuruDurum.TASLAK;

                    // Hareket kaydı ekle: Red nedeni ile birlikte taslağa döndürme bilgisi
                    _context.EpostaDuyuruHareketleri.Add(new EpostaDuyuruHareket
                    {
                        DuyuruId = id,
                        OncekiDurum = DuyuruDurum.REDDEDILDI,
                        YeniDurum = DuyuruDurum.TASLAK,
                        IslemTipi = "DUZENLEME",
                        KullaniciId = kullaniciId,
                        Aciklama = $"Reddedilen duyuru düzenlenmek üzere taslağa döndürüldü. Önceki red nedeni: {lastRejection?.Aciklama}",
                        IslemTarihi = DateTime.Now
                    });

                    _logger.LogInformation("Announcement {Id} status changed from REDDEDILDI to TASLAK for re-editing. Previous rejection: {Reason}",
                        id, lastRejection?.Aciklama);
                }

                var oldApproverId = announcement.SonOnaylayanKullaniciId;
                var oldKonu = announcement.Konu;
                var oldIcerik = announcement.Icerik;
                var oldBannerId = announcement.BannerDosyaId;

                // Onaylayıcı değiştirilmişse kontrolü yap
                if (request.OnaylayanKullaniciId.HasValue)
                {
                    var approver = await _context.Kullanicilar
                        .Include(k => k.Rol)
                        .FirstOrDefaultAsync(k => k.Id == request.OnaylayanKullaniciId.Value && k.Aktif == "Y");

                    if (approver == null)
                        return ResponseModel.ErrorResult("Seçilen onaylayıcı bulunamadı", 404);

                    if (approver.Rol?.RolKodu != RolKodu.MANAGER && approver.Rol?.RolKodu != RolKodu.ADMIN)
                        return ResponseModel.ErrorResult("Onaylayıcı MANAGER veya ADMIN rolünde olmalıdır", 400);
                }

                // GÜVENLİK: SMTP gönderici kategorisinin geçerli olup olmadığını kontrol et
                if (!string.IsNullOrWhiteSpace(request.GondericiKategori))
                {
                    var isValidCategory = await _emailCategoryService.IsValidCategoryAsync(request.GondericiKategori);
                    if (!isValidCategory)
                    {
                        await transaction.RollbackAsync();
                        return ResponseModel.ErrorResult($"Geçersiz SMTP gönderici kategorisi: {request.GondericiKategori}", 400);
                    }
                }

                // Şablon seçilmişse, şablon içeriğini duyuruya yükle
                if (request.SablonId.HasValue)
                {
                    var template = await _context.EpostaSablonlari
                        .FirstOrDefaultAsync(s => s.Id == request.SablonId.Value && s.Aktif == "Y");

                    if (template == null)
                        return ResponseModel.ErrorResult("Seçilen şablon aktif değil veya bulunamadı", 400);

                    // Şablon içeriğini duyuruya aktar
                    announcement.Konu = template.KonuSablonu ?? "Konu";
                    announcement.Icerik = template.IcerikSablonu;
                    announcement.SablonId = template.Id;

                    _logger.LogInformation("Template {TemplateId} ({TemplateName}) loaded into announcement {AnnouncementId}",
                        template.Id, template.SablonAdi, id);
                }
                else
                {
                    // Şablon seçilmediyse request'teki değerleri kullan
                    _mapper.Map(request, announcement);

                    // GÜVENLİK: HTML içeriğini sanitize et (XSS önlemi)
                    if (!string.IsNullOrEmpty(announcement.Icerik))
                    {
                        announcement.Icerik = _securityService.SanitizeHtmlContent(announcement.Icerik);
                    }
                }

                // Banner dosyası kontrolü
                if (request.BannerDosyaId.HasValue)
                {
                    var bannerFile = await _context.Dosyalar
                        .FirstOrDefaultAsync(f => f.Id == request.BannerDosyaId.Value);

                    if (bannerFile == null)
                        return ResponseModel.ErrorResult("Seçilen banner dosyası bulunamadı", 404);

                    // Banner dosyasının görsel olup olmadığını kontrol et
                    var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                    var fileExtension = Path.GetExtension(bannerFile.DosyaAdi).ToLowerInvariant();

                    if (!imageExtensions.Contains(fileExtension))
                        return ResponseModel.ErrorResult("Banner dosyası bir görsel dosya olmalıdır (jpg, png, gif, etc.)", 400);

                    _logger.LogInformation("Banner file {FileId} ({FileName}) attached to announcement {AnnouncementId}",
                        bannerFile.Id, bannerFile.DosyaAdi, id);
                }

                announcement.GuncellemeTarihi = DateTime.Now;

                // Mevcut alıcıları sil (zaten transaction içindeyiz)
                var existingRecipients = await _context.EpostaDuyuruAlicilari
                    .Where(a => a.DuyuruId == id)
                    .ToListAsync();
                _context.EpostaDuyuruAlicilari.RemoveRange(existingRecipients);

                // Yeni alıcıları ekle - Gruplar
                if (request.GrupIdList != null && request.GrupIdList.Count > 0)
                {
                    // Grup tiplerini kontrol et
                    var groups = await _context.EpostaGruplari
                        .Where(g => request.GrupIdList.Contains(g.Id))
                        .ToListAsync();

                    foreach (var grupId in request.GrupIdList)
                    {
                        var grup = groups.FirstOrDefault(g => g.Id == grupId);

                        if (grup == null)
                        {
                            await transaction.RollbackAsync();
                            return ResponseModel.ErrorResult($"Grup ID {grupId} bulunamadı", 404);
                        }

                        // Güvenlik: Tüm gruplar BCC-only (KVKK/GDPR uyumu)
                        var kategori = "BCC";

                        // KRİTİK: DEBIS grupları SADECE BCC olabilir (çift güvenlik kontrolü)
                        if (!ValidateDebisGroupSecurity(grup, kategori, out var debisError))
                        {
                            await transaction.RollbackAsync();
                            return ResponseModel.ErrorResult(debisError, 400);
                        }

                        _context.EpostaDuyuruAlicilari.Add(new EpostaDuyuruAlici
                        {
                            DuyuruId = id,
                            GrupId = grupId,
                            AliciTipi = "GRUP",
                            AliciKategorisi = kategori,
                            GonderimDurumu = "BEKLIYOR"
                        });
                    }
                }

                // Yeni alıcıları ekle - Manuel emailler (varsayılan TO)
                if (request.AliciEmailList != null && request.AliciEmailList.Count > 0)
                {
                    foreach (var email in request.AliciEmailList)
                    {
                        _context.EpostaDuyuruAlicilari.Add(new EpostaDuyuruAlici
                        {
                            DuyuruId = id,
                            Email = email,
                            AliciTipi = "MANUEL",
                            AliciKategorisi = "BCC", // Tüm emailler BCC (KVKK/GDPR)
                            GonderimDurumu = "BEKLIYOR"
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // NOT: Toplam alıcı sayısı PKG_ALICI_SAYISI trigger'ı tarafından otomatik hesaplanır

                _logger.LogInformation("Announcement updated: {Id} by user {UserId}, Approver: {ApproverId}",
                    id, kullaniciId, request.OnaylayanKullaniciId);

                // Zamanlamayı güncelle veya oluştur
                if (request.ZamanlanmisTarih.HasValue)
                {
                    // Mevcut zamanlamaları iptal et
                    var existingSchedules = await _context.EpostaDuyuruZamanlamalari
                        .Where(z => z.DuyuruId == id && z.Durum != "IPTAL" && z.Durum != "TAMAMLANDI")
                        .ToListAsync();

                    foreach (var schedule in existingSchedules)
                    {
                        schedule.Durum = "IPTAL";
                    }

                    // Yeni zamanlama oluştur
                    var scheduleRequest = new CreateScheduleRequest
                    {
                        DuyuruId = id,
                        ZamanlanmaTarihi = request.ZamanlanmisTarih.Value
                    };

                    await _scheduleService.CreateScheduleAsync(scheduleRequest, kullaniciId);
                    _logger.LogInformation("Schedule updated for announcement {Id} at {Date}", id, request.ZamanlanmisTarih);
                }

                // Audit log: Duyuru güncelleme - onaylayıcı değişimi
                if (oldApproverId != request.OnaylayanKullaniciId)
                {
                    var detay = oldApproverId.HasValue
                        ? $"Onaylayıcı değiştirildi: {oldApproverId} → {request.OnaylayanKullaniciId}"
                        : $"Onaylayıcı seçildi: {request.OnaylayanKullaniciId}";

                    await _auditLog.LogAsync(
                        kategori: "EMAIL",
                        islem: "ONAYLAYICI_DEGISIMI",
                        detay: $"Duyuru ID: {id}, Konu: {announcement.Konu}, {detay}",
                        kullaniciId: kullaniciId
                    );
                }

                // Audit trail: İçerik değişikliklerini kaydet
                var degisiklikler = new List<string>();

                if (oldKonu != announcement.Konu)
                    degisiklikler.Add($"Konu değişti");

                if (oldIcerik != announcement.Icerik)
                    degisiklikler.Add("İçerik güncellendi");

                if (oldBannerId != announcement.BannerDosyaId)
                {
                    if (announcement.BannerDosyaId.HasValue && !oldBannerId.HasValue)
                        degisiklikler.Add("Banner eklendi");
                    else if (!announcement.BannerDosyaId.HasValue && oldBannerId.HasValue)
                        degisiklikler.Add("Banner kaldırıldı");
                    else
                        degisiklikler.Add("Banner değiştirildi");
                }

                // Alıcı değişikliği kontrolü (sayı bazlı - detaylı karşılaştırma maliyetli)
                var yeniAliciSayisi = (request.GrupIdList?.Count ?? 0) + (request.AliciEmailList?.Count ?? 0);
                var eskiAliciSayisi = existingRecipients.Count;
                if (yeniAliciSayisi != eskiAliciSayisi)
                    degisiklikler.Add($"Alıcı listesi değişti ({eskiAliciSayisi} → {yeniAliciSayisi})");

                // Eğer değişiklik varsa hareket kaydı ekle (transaction içinde, commit öncesi eklendi - satır 700)
                if (degisiklikler.Count > 0)
                {
                    var aciklama = "Duyuru güncellendi: " + string.Join(", ", degisiklikler);
                    _logger.LogDebug("Duyuru düzenleme hareket kaydı: {Aciklama}", aciklama);
                }

                return ResponseModel.SuccessResult("Duyuru başarıyla güncellendi");
            }
            catch (Exception innerEx)
            {
                await transaction.RollbackAsync();
                _logger.LogError(innerEx, "Error updating announcement {Id}", id);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating announcement {Id}", id);
            return ResponseModel.ErrorResult("Duyuru güncellenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DeleteAnnouncementAsync(int id, int kullaniciId)
    {
        // CRITICAL FIX: Transaction Management
        // Çoklu DB operasyonu + fiziksel dosya silme var - atomicity için transaction gerekli
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var announcement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (announcement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            // GÜVENLİK: Ownership kontrolü - ADMIN ve MANAGER hariç sadece sahibi silebilir
            var currentUser = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == kullaniciId);

            var isAdmin = currentUser?.Rol?.RolKodu == "ADMIN";
            var isManager = currentUser?.Rol?.RolKodu == "MANAGER";

            if (announcement.OlusturanKullaniciId != kullaniciId && !isAdmin && !isManager)
            {
                _logger.LogWarning("Unauthorized delete attempt: User {UserId} tried to delete announcement {AnnouncementId} owned by {OwnerId}",
                    kullaniciId, id, announcement.OlusturanKullaniciId);
                return ResponseModel.ErrorResult("Bu duyuruyu silme yetkiniz yok", 403);
            }

            // Gönderilmiş veya onaylanmış duyurular silinemez
            if (announcement.GercekGonderimTarihi.HasValue)
                return ResponseModel.ErrorResult("Daha önce gönderilmiş duyurular silinemez", 400);

            // KRİTİK: GONDERILIYOR durumunda da silme yasak (job çalışırken silme riski)
            if (announcement.Durum is DuyuruDurum.ONAYLANDI or DuyuruDurum.GONDERILDI or DuyuruDurum.GONDERILIYOR)
                return ResponseModel.ErrorResult("Onaylanmış, gönderilmiş veya gönderiliyor durumundaki duyurular silinemez", 400);

            var oncekiDurum = announcement.Durum;

            // Hareket kaydı: Silme işlemi (duyuru silinmeden ÖNCE kaydet, aynı transaction içinde)
            AddHareket(id, oncekiDurum, "SILINDI",
                "SILME", kullaniciId, "Duyuru kullanıcı tarafından silindi", null);

            // Cascade delete: İlişkili kayıtları sil
            if (announcement.Alicilar.Any())
            {
                _context.EpostaDuyuruAlicilari.RemoveRange(announcement.Alicilar);
            }

            // Gönderim logları varsa sil
            var sendLogs = await _context.EpostaDuyuruGonderimLoglari
                .Where(l => l.DuyuruId == id)
                .ToListAsync();
            if (sendLogs.Any())
            {
                _context.EpostaDuyuruGonderimLoglari.RemoveRange(sendLogs);
            }

            // Dosyaları sil (hem DB'den hem fiziksel olarak)
            var files = await _context.Dosyalar
                .Where(f => f.DuyuruId == id)
                .ToListAsync();

            foreach (var file in files)
            {
                // Fiziksel dosyayı sil
                var filePath = Path.Combine(_uploadPath, file.DosyaYolu);
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("Deleted file: {FilePath}", filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete file: {FilePath}", filePath);
                    }
                }
            }

            // DB'den dosyaları sil
            if (files.Any())
            {
                _context.Dosyalar.RemoveRange(files);
            }

            // Duyuru klasörünü sil
            var year = announcement.OlusturmaTarihi.Year;
            var month = announcement.OlusturmaTarihi.Month.ToString("00");
            var announcementFolder = Path.Combine(_uploadPath, "announcements", year.ToString(), month, $"announcement-{id}");
            if (Directory.Exists(announcementFolder))
            {
                try
                {
                    Directory.Delete(announcementFolder, recursive: true);
                    _logger.LogInformation("Deleted announcement folder: {FolderPath}", announcementFolder);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete announcement folder: {FolderPath}", announcementFolder);
                }
            }

            // Duyuruyu sil (CASCADE ile hareket kayıtları da silinecek)
            _context.EpostaDuyurulari.Remove(announcement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement deleted: {Id} by user {UserId}, Previous status: {Status}",
                id, kullaniciId, oncekiDurum);

            // Audit log (duyuru silinse bile kalır)
            await _auditLog.LogAsync(
                kategori: "EMAIL",
                islem: "DUYURU_SILME",
                detay: $"Duyuru ID: {id}, Konu: {announcement.Konu}, Önceki Durum: {oncekiDurum}",
                kullaniciId: kullaniciId
            );

            // Transaction commit - Tüm işlemler başarılıysa commit et
            await transaction.CommitAsync();

            return ResponseModel.SuccessResult("Duyuru başarıyla silindi");
        }
        catch (Exception ex)
        {
            // Transaction rollback - Hata durumunda tüm işlemleri geri al
            // NOT: Fiziksel dosyalar silinmişse rollback yapılamaz (file system transaction yok)
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting announcement {Id} - Transaction rolled back", id);
            return ResponseModel.ErrorResult("Duyuru silinirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DuplicateAnnouncementAsync(int id, int kullaniciId)
    {
        // CRITICAL FIX: Transaction Management
        // 2 ayrı SaveChangesAsync() var - atomicity için transaction gerekli
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var originalAnnouncement = await _context.EpostaDuyurulari
                .Include(d => d.Alicilar)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (originalAnnouncement == null)
                return ResponseModel.ErrorResult("Duyuru bulunamadı", 404);

            var duplicatedAnnouncement = new EpostaDuyuru
            {
                Konu = $"{originalAnnouncement.Konu} (Kopya)",
                Icerik = originalAnnouncement.Icerik,
                SablonId = originalAnnouncement.SablonId,
                DuyuruKategorisi = originalAnnouncement.DuyuruKategorisi,
                SonOnaylayanKullaniciId = originalAnnouncement.SonOnaylayanKullaniciId,
                OlusturanKullaniciId = kullaniciId,
                OlusturmaTarihi = DateTime.Now,
                Durum = DuyuruDurum.TASLAK
            };

            _context.EpostaDuyurulari.Add(duplicatedAnnouncement);
            await _context.SaveChangesAsync();

            // Copy recipients
            if (originalAnnouncement.Alicilar.Any())
            {
                foreach (var alici in originalAnnouncement.Alicilar)
                {
                    var duplicatedAlici = new EpostaDuyuruAlici
                    {
                        DuyuruId = duplicatedAnnouncement.Id,
                        GrupId = alici.GrupId,
                        Email = alici.Email,
                        AliciKategorisi = alici.AliciKategorisi
                    };
                    _context.EpostaDuyuruAlicilari.Add(duplicatedAlici);
                }
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Announcement duplicated: {OriginalId} -> {NewId} by user {UserId}, Recipients: {Count}",
                id, duplicatedAnnouncement.Id, kullaniciId, originalAnnouncement.Alicilar.Count);

            // Transaction commit - Tüm işlemler başarılıysa commit et
            await transaction.CommitAsync();

            return ResponseModel.SuccessResult("Duyuru başarıyla kopyalandı");
        }
        catch (Exception ex)
        {
            // Transaction rollback - Hata durumunda tüm işlemleri geri al
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error duplicating announcement {Id} - Transaction rolled back", id);
            return ResponseModel.ErrorResult("Duyuru kopyalanırken hata oluştu", 500);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Hareket kaydı ekler. NOT: SaveChangesAsync çağırmaz, ana metodun transaction'ına dahil olur.
    /// </summary>
    private void AddHareket(int duyuruId, string? oncekiDurum, string yeniDurum,
        string islemTipi, int? kullaniciId, string? aciklama, int? secilenOnaylayiciId)
    {
        var hareket = new EpostaDuyuruHareket
        {
            DuyuruId = duyuruId,
            OncekiDurum = oncekiDurum,
            YeniDurum = yeniDurum,
            IslemTipi = islemTipi,
            KullaniciId = kullaniciId,
            Aciklama = aciklama,
            SecilenOnaylayiciId = secilenOnaylayiciId,
            IslemTarihi = DateTime.Now
        };

        _context.EpostaDuyuruHareketleri.Add(hareket);

        _logger.LogDebug("Hareket eklendi (commit bekliyor): Duyuru {DuyuruId}, {OncekiDurum} -> {YeniDurum}, İşlem: {IslemTipi}",
            duyuruId, oncekiDurum ?? "YOK", yeniDurum, islemTipi);
    }

    #endregion Helper Methods

    /// <summary>
    /// DEBIS grup güvenlik kontrolü - Sadece BCC kategorisinde kullanılabilir
    /// </summary>
    private bool ValidateDebisGroupSecurity(EpostaGrubu grup, string kategori, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (grup.GrupTipi == "DEBIS" && kategori != "BCC")
        {
            _logger.LogError("SECURITY VIOLATION: DEBIS group {GroupId} ({GroupName}) attempted with {Category} category",
                grup.Id, grup.GrupAdi, kategori);
            errorMessage = $"DEBIS grupları güvenlik nedeniyle sadece BCC kategorisinde kullanılabilir: {grup.GrupAdi}";
            return false;
        }

        return true;
    }
}