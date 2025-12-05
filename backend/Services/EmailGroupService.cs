using ClosedXML.Excel;
using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using DeuEposta.Utils;
using Microsoft.EntityFrameworkCore;
using System.Security;
using System.Text.RegularExpressions;

namespace DeuEposta.Services;

public interface IEmailGroupService
{
    Task<ResponseDataModel<List<EmailGroupListDto>>> GetGroupsAsync(int page = 1, int pageSize = 20, string? searchTerm = null, string? userRole = null);

    Task<ResponseDataModel<EmailGroupDetailDto>> GetGroupByIdAsync(int id);

    Task<ResponseModel> CreateGroupAsync(CreateEmailGroupDto request, int kullaniciId);

    Task<ResponseModel> UpdateGroupAsync(int id, UpdateEmailGroupDto request, int kullaniciId);

    Task<ResponseModel> DeleteGroupAsync(int id, int kullaniciId);

    Task<ResponseDataModel<List<EpostaGrupUyesi>>> GetGroupMembersAsync(int groupId);

    Task<ResponseModel> AddMemberAsync(int groupId, AddGroupMemberRequest request, int kullaniciId);

    Task<ResponseModel> RemoveMemberAsync(int groupId, int memberId, int kullaniciId);

    Task<ResponseModel> UpdateMemberAsync(int groupId, int memberId, UpdateGroupMemberRequest request, int kullaniciId);

    Task<ResponseDataModel<List<string>>> GetGroupEmailsAsync(int groupId);

    Task<ResponseDataModel<ImportMembersResult>> ImportMembersFromFileAsync(int groupId, IFormFile file, int kullaniciId);

    Task<ResponseDataModel<DynamicGroupPreviewDto>> PreviewDynamicGroupAsync(string viewAdi, string? filterKosulu);
}

public class EmailGroupService : IEmailGroupService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<EmailGroupService> _logger;

    private readonly IAuditLogService _auditLog;

    public EmailGroupService(DeuEpostaContext context, ILogger<EmailGroupService> logger, IAuditLogService auditLog)
    {
        _context = context;
        _logger = logger;
        _auditLog = auditLog;
    }

    public async Task<ResponseDataModel<List<EmailGroupListDto>>> GetGroupsAsync(int page = 1, int pageSize = 20, string? searchTerm = null, string? userRole = null)
    {
        try
        {
            // GÜVENLİK: Pagination parametrelerini normalize et
            (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

            // PERFORMANS: Include(Uyeler) gereksiz - sadece UyeSayisi kullanılıyor (database'de hesaplı)
            var query = _context.EpostaGruplari
                .AsNoTracking() // Read-only query için performance
                .AsQueryable();

            // Pasif grupları sadece ADMIN görebilir
            if (userRole != RolKodu.ADMIN)
            {
                query = query.Where(g => g.Aktif == "Y");
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(g => g.GrupAdi.Contains(searchTerm) ||
                                        (g.Aciklama != null && g.Aciklama.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            // Önce grup tipine göre, sonra ID'ye göre sırala
            var groupsRaw = await query
                .OrderBy(g => g.GrupTipi)
                .ThenBy(g => g.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO'ya map et - UyeSayisi database'den okunur
            // STATIK gruplar: PKG_GRUP_UYE_SAYISI trigger'ı tarafından otomatik güncellenir
            // DINAMIK gruplar: Backend tarafından create/update sırasında hesaplanır
            var groups = groupsRaw.Select(g => new EmailGroupListDto
            {
                Id = g.Id,
                GrupAdi = g.GrupAdi,
                Aciklama = g.Aciklama,
                GrupTipi = DeuEposta.Models.Enums.GrupTipiExtensions.ParseSafely(g.GrupTipi),
                UyeSayisi = g.UyeSayisi, // Database'den oku
                Aktif = g.Aktif,
                OlusturmaTarihi = g.OlusturmaTarihi
            }).ToList();

            return ResponseDataModel<List<EmailGroupListDto>>.SuccessResultWithPagination(
                groups, totalCount, page, pageSize, "Email grupları başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email groups");
            return ResponseDataModel<List<EmailGroupListDto>>.ErrorResult("Email grupları alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<EmailGroupDetailDto>> GetGroupByIdAsync(int id)
    {
        try
        {
            var grupData = await _context.EpostaGruplari
                .Include(g => g.Uyeler)
                .Where(g => g.Id == id)
                .FirstOrDefaultAsync();

            if (grupData == null)
                return ResponseDataModel<EmailGroupDetailDto>.ErrorResult("Grup bulunamadı", 404);

            var group = new EmailGroupDetailDto
            {
                Id = grupData.Id,
                GrupAdi = grupData.GrupAdi,
                Aciklama = grupData.Aciklama ?? string.Empty,
                GrupTipi = DeuEposta.Models.Enums.GrupTipiExtensions.ParseSafely(grupData.GrupTipi),
                ViewAdi = grupData.ViewAdi ?? string.Empty,
                FilterKosulu = grupData.FilterKosulu ?? string.Empty,
                Aktif = grupData.Aktif,
                OlusturmaTarihi = grupData.OlusturmaTarihi,
                GuncellemeTarihi = grupData.GuncellemeTarihi,
                UyeSayisi = grupData.Uyeler.Count(u => u.Durum == "AKTIF"),
                Uyeler = grupData.Uyeler.Select(u => new EmailGroupMemberDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    AdSoyad = u.AdSoyad,
                    Departman = u.Departman ?? string.Empty,
                    Aktif = u.Durum,
                    EklenmeTarihi = u.EklenmeTarihi
                }).ToList()
            };

            return ResponseDataModel<EmailGroupDetailDto>.SuccessResult(group, "Grup başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group {Id}", id);
            return ResponseDataModel<EmailGroupDetailDto>.ErrorResult("Grup alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> CreateGroupAsync(CreateEmailGroupDto request, int kullaniciId)
    {
        try
        {
            // Check if group name already exists
            var existingGroup = await _context.EpostaGruplari
                .FirstOrDefaultAsync(g => g.GrupAdi == request.GrupAdi);

            if (existingGroup != null)
                return ResponseModel.ErrorResult("Bu isimde bir grup zaten var", 400);

            var group = new EpostaGrubu
            {
                GrupAdi = request.GrupAdi,
                Aciklama = request.Aciklama,
                GrupTipi = request.GrupTipi.ToString(), // Enum'u string'e çevir
                ViewAdi = request.ViewAdi,
                FilterKosulu = request.FilterKosulu,
                // DEBIS grupları için default listeci email, diğerleri için request'ten al
                ListeciEmail = request.GrupTipi == GrupTipi.DEBIS ? "listeci@deu.edu.tr" : request.ListeciEmail,
                UyeSayisi = 0, // Default 0 - grup tipine göre aşağıda hesaplanır
                Aktif = "Y",
                OlusturmaTarihi = DateTime.Now
            };

            // HYBRID APPROACH: Backend'de üye sayısını hesapla (trigger yerine)
            if (request.GrupTipi == GrupTipi.DINAMIK && !string.IsNullOrEmpty(request.ViewAdi))
            {
                // DINAMIK gruplar: View'den gerçek zamanlı COUNT
                try
                {
                    var emails = await GetDynamicGroupEmailsAsync(request.ViewAdi, request.FilterKosulu);
                    group.UyeSayisi = emails.Count;
                    _logger.LogInformation("Dinamik grup üye sayısı hesaplandı: {Count}", emails.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Dinamik grup üye sayısı hesaplanamadı, 0 olarak ayarlandı");
                    group.UyeSayisi = 0;
                }
            }
            else if (request.GrupTipi == GrupTipi.DEBIS)
            {
                // DEBIS gruplar: Listeci email = 1 alıcı
                group.UyeSayisi = 1;
                _logger.LogInformation("DEBIS grup üye sayısı: 1 (listeci email)");
            }
            // NORMAL/STATIK gruplar: İlk oluşturulduğunda 0, üye eklendikçe artacak

            _context.EpostaGruplari.Add(group);
            await _context.SaveChangesAsync();

            // NORMAL/STATIK gruplar için üyeleri ekle (eğer varsa)
            if ((request.GrupTipi == GrupTipi.NORMAL || request.GrupTipi == GrupTipi.STATIK)
                && request.StatikUyeler != null && request.StatikUyeler.Any())
            {
                // Performans optimizasyonu: AddRange kullan
                var members = request.StatikUyeler.Select(uye => new EpostaGrupUyesi
                {
                    GrupId = group.Id,
                    Email = uye.Email,
                    AdSoyad = uye.AdSoyad ?? uye.Email,
                    Departman = uye.Departman,
                    Durum = "AKTIF",
                    OlusturmaTarihi = DateTime.Now,
                    EklenmeTarihi = DateTime.Now
                }).ToList();

                _context.EpostaGrupUyeleri.AddRange(members);
                await _context.SaveChangesAsync();

                // HYBRID APPROACH: Backend'de UYE_SAYISI güncelle
                await UpdateGroupMemberCountAsync(group.Id);

                _logger.LogInformation("Added {Count} members to group {GroupId}", members.Count, group.Id);
            }

            await _auditLog.LogAsync(
                           kategori: "GROUP",
                           islem: "GROUP_CREATE",
                           detay: $"Created group '{group.GrupAdi}' (ID: {group.Id}) with type {group.GrupTipi} and member count {group.UyeSayisi}"
                       );

            _logger.LogInformation("Email group created: {Id} - {Name} by user {UserId}", group.Id, group.GrupAdi, kullaniciId);

            return ResponseDataModel<object>.SuccessResult(
                new { id = group.Id, grupAdi = group.GrupAdi },
                "Email grubu başarıyla oluşturuldu"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email group");
            return ResponseModel.ErrorResult("Email grubu oluşturulurken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> UpdateGroupAsync(int id, UpdateEmailGroupDto request, int kullaniciId)
    {
        try
        {
            var group = await _context.EpostaGruplari.FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return ResponseModel.ErrorResult("Grup bulunamadı", 404);

            // Check if another group with same name exists (only if GrupAdi provided)
            if (!string.IsNullOrEmpty(request.GrupAdi))
            {
                var existingGroup = await _context.EpostaGruplari
                    .FirstOrDefaultAsync(g => g.GrupAdi == request.GrupAdi && g.Id != id);

                if (existingGroup != null)
                    return ResponseModel.ErrorResult("Bu isimde başka bir grup zaten var", 400);

                group.GrupAdi = request.GrupAdi;
            }

            if (request.Aciklama != null)
                group.Aciklama = request.Aciklama;

            // Aktif/Pasif durumu güncelleme (sadece ADMIN)
            if (request.Aktif != null)
            {
                var user = await _context.Kullanicilar
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Id == kullaniciId);

                if (user?.Rol?.RolKodu != RolKodu.ADMIN)
                {
                    _logger.LogWarning("Non-admin user {UserId} attempted to change group status for group {GroupId}",
                        kullaniciId, id);
                    return ResponseModel.ErrorResult("Grup durumu sadece sistem yöneticisi (ADMIN) değiştirebilir", 403);
                }

                group.Aktif = request.Aktif;
            }

            // FilterKosulu güncellendiğinde DINAMIK gruplar için üye sayısını yeniden hesapla
            bool filterUpdated = false;
            if (request.FilterKosulu != null)
            {
                group.FilterKosulu = request.FilterKosulu;
                filterUpdated = true;
            }

            // DEBIS grup listeci email güvenlik kontrolü
            if (request.ListeciEmail != null)
            {
                if (group.GrupTipi == "DEBIS")
                {
                    // Sadece ADMIN güncelleyebilir
                    var user = await _context.Kullanicilar
                        .Include(u => u.Rol)
                        .FirstOrDefaultAsync(u => u.Id == kullaniciId);

                    if (user?.Rol?.RolKodu != RolKodu.ADMIN)
                    {
                        _logger.LogWarning("Non-admin user {UserId} attempted to update DEBIS group listeci email for group {GroupId}",
                            kullaniciId, id);
                        return ResponseModel.ErrorResult("DEBIS grup listeci email adresi sadece sistem yöneticisi (ADMIN) güncelleyebilir", 403);
                    }
                }

                group.ListeciEmail = request.ListeciEmail;
            }

            // DINAMIK grup ise ve filter güncellenmiş ise üye sayısını yeniden hesapla
            if (filterUpdated && group.GrupTipi == "DINAMIK" && !string.IsNullOrEmpty(group.ViewAdi))
            {
                try
                {
                    var emails = await GetDynamicGroupEmailsAsync(group.ViewAdi, group.FilterKosulu);
                    group.UyeSayisi = emails.Count;
                    _logger.LogInformation("Dinamik grup {Id} üye sayısı güncellendi: {Count}", group.Id, emails.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Dinamik grup {Id} üye sayısı hesaplanamadı", group.Id);
                }
            }

            group.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(
                           kategori: "GROUP",
                           islem: "GROUP_MEMBER_IMPORT",
                           detay: $"Created group '{group.GrupAdi}' (ID: {group.Id}) with type {group.GrupTipi} and member count {group.UyeSayisi}"
                       );

            _logger.LogInformation("Email group updated: {Id} - {Name} by user {UserId}", id, group.GrupAdi, kullaniciId);

            return ResponseModel.SuccessResult("Email grubu başarıyla güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email group {Id}", id);
            return ResponseModel.ErrorResult("Email grubu güncellenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DeleteGroupAsync(int id, int kullaniciId)
    {
        try
        {
            var group = await _context.EpostaGruplari
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return ResponseModel.ErrorResult("Grup bulunamadı", 404);

            // Check if group is being used in announcements - Oracle compatible check
            var usageCount = await _context.EpostaDuyuruAlicilari
                .CountAsync(a => a.GrupId == id);

            if (usageCount > 0)
                return ResponseModel.ErrorResult("Bu grup duyurularda kullanıldığı için silinemez", 400);

            // KRİTİK: Aktif zamanlamalarda kullanılıyor mu kontrol et
            // Oracle uyumlu sorgu - nested Any yerine Count kullan
            var activeSchedulesWithGroup = await _context.EpostaDuyuruZamanlamalari
                .Where(z => z.Durum == "BEKLEMEDE")
                .Join(_context.EpostaDuyuruAlicilari,
                    z => z.DuyuruId,
                    a => a.DuyuruId,
                    (z, a) => new { z, a })
                .CountAsync(x => x.a.GrupId == id);

            if (activeSchedulesWithGroup > 0)
            {
                _logger.LogWarning("Cannot delete group {GroupId} ({GroupName}) - used in active scheduled announcements",
                    id, group.GrupAdi);
                return ResponseModel.ErrorResult(
                    "Bu grup aktif zamanlanmış duyurularda kullanıldığı için silinemez. Önce zamanlamaları iptal edin.",
                    400);
            }

            // Remove all members first (sadece NORMAL/STATIK gruplarda üye var)
            if (group.GrupTipi == "NORMAL" || group.GrupTipi == "STATIK")
            {
                var members = await _context.EpostaGrupUyeleri
                    .Where(u => u.GrupId == id)
                    .ToListAsync();
                _context.EpostaGrupUyeleri.RemoveRange(members);
            }

            _context.EpostaGruplari.Remove(group);

            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(
                           kategori: "GROUP",
                           islem: "GROUP_DELETE",
                           detay: $"Deleted group '{group.GrupAdi}' (ID: {group.Id})"
                       );

            _logger.LogInformation("Email group deleted: {Id} - {Name} by user {UserId}", id, group.GrupAdi, kullaniciId);

            return ResponseModel.SuccessResult("Email grubu başarıyla silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email group {Id}", id);
            return ResponseModel.ErrorResult("Email grubu silinirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<EpostaGrupUyesi>>> GetGroupMembersAsync(int groupId)
    {
        try
        {
            var group = await _context.EpostaGruplari.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return ResponseDataModel<List<EpostaGrupUyesi>>.ErrorResult("Grup bulunamadı", 404);

            // Only return active members (Durum == 'AKTIF') to avoid returning inactive/archived entries
            var members = await _context.EpostaGrupUyeleri
                .Where(u => u.GrupId == groupId && u.Durum == "AKTIF")
                .OrderBy(u => u.AdSoyad)
                .ToListAsync();

            return ResponseDataModel<List<EpostaGrupUyesi>>.SuccessResult(members, "Grup üyeleri başarıyla alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group members for group {GroupId}", groupId);
            return ResponseDataModel<List<EpostaGrupUyesi>>.ErrorResult("Grup üyeleri alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> AddMemberAsync(int groupId, AddGroupMemberRequest request, int kullaniciId)
    {
        try
        {
            var group = await _context.EpostaGruplari.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return ResponseModel.ErrorResult("Grup bulunamadı", 404);

            // Check if member already exists
            var existingMember = await _context.EpostaGrupUyeleri
                .FirstOrDefaultAsync(u => u.GrupId == groupId && u.Email == request.Email);

            if (existingMember != null)
                return ResponseModel.ErrorResult("Bu email adresi zaten grup üyesi", 400);

            var member = new EpostaGrupUyesi
            {
                GrupId = groupId,
                Email = request.Email,
                AdSoyad = request.AdSoyad,
                Durum = "AKTIF",
                OlusturmaTarihi = DateTime.Now
            };

            _context.EpostaGrupUyeleri.Add(member);
            await _context.SaveChangesAsync();

            // HYBRID APPROACH: Backend'de UYE_SAYISI güncelle (trigger yerine)
            await UpdateGroupMemberCountAsync(groupId);

            await _auditLog.LogAsync(
                           kategori: "GROUP",
                           islem: "GROUP_MEMBER_ADD",
                           detay: $"Added member '{member.Email}' to group '{group.GrupAdi}' (ID: {group.Id})"
                       );

            _logger.LogInformation("Member added to group {GroupId}: {Email} by user {UserId}", groupId, request.Email, kullaniciId);

            return ResponseModel.SuccessResult("Üye başarıyla eklendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to group {GroupId}", groupId);
            return ResponseModel.ErrorResult("Üye eklenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> RemoveMemberAsync(int groupId, int memberId, int kullaniciId)
    {
        try
        {
            var member = await _context.EpostaGrupUyeleri
                .FirstOrDefaultAsync(u => u.Id == memberId && u.GrupId == groupId);

            if (member == null)
                return ResponseModel.ErrorResult("Üye bulunamadı", 404);

            _context.EpostaGrupUyeleri.Remove(member);
            await _context.SaveChangesAsync();

            // HYBRID APPROACH: Backend'de UYE_SAYISI güncelle (trigger yerine)
            await UpdateGroupMemberCountAsync(groupId);

            await _auditLog.LogAsync(
                           kategori: "GROUP",
                           islem: "GROUP_MEMBER_REMOVE",
                           detay: $"Removed member '{member.Email}' from group ID: {groupId}"
                       );

            _logger.LogInformation("Member removed from group {GroupId}: {MemberId} by user {UserId}", groupId, memberId, kullaniciId);

            return ResponseModel.SuccessResult("Üye başarıyla çıkarıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {MemberId} from group {GroupId}", memberId, groupId);
            return ResponseModel.ErrorResult("Üye çıkarılırken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> UpdateMemberAsync(int groupId, int memberId, UpdateGroupMemberRequest request, int kullaniciId)
    {
        try
        {
            var member = await _context.EpostaGrupUyeleri
                .FirstOrDefaultAsync(u => u.Id == memberId && u.GrupId == groupId);

            if (member == null)
                return ResponseModel.ErrorResult("Üye bulunamadı", 404);

            var oldDurum = member.Durum;
            member.Email = request.Email;
            member.AdSoyad = request.AdSoyad;
            member.Durum = request.Durum;
            member.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            // HYBRID APPROACH: Durum değiştiğinde UYE_SAYISI güncelle (AKTIF<->PASIF geçişlerinde)
            if (oldDurum != request.Durum)
            {
                await UpdateGroupMemberCountAsync(groupId);
            }

            await _auditLog.LogAsync(
                           kategori: "GROUP",
                           islem: "GROUP_MEMBER_UPDATE",
                           detay: $"Updated member '{member.Email}' in group ID: {groupId}. New status: {member.Durum}"
                       );

            _logger.LogInformation("Member updated in group {GroupId}: {MemberId} by user {UserId}", groupId, memberId, kullaniciId);

            return ResponseModel.SuccessResult("Üye başarıyla güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member {MemberId} in group {GroupId}", memberId, groupId);
            return ResponseModel.ErrorResult("Üye güncellenirken hata oluştu", 500);
        }
    }

    /// <summary>
    /// Grup email listesini döner - grup tipine göre farklı logic
    /// </summary>
    public async Task<ResponseDataModel<List<string>>> GetGroupEmailsAsync(int groupId)
    {
        try
        {
            var group = await _context.EpostaGruplari.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return ResponseDataModel<List<string>>.ErrorResult("Grup bulunamadı", 404);

            List<string> emails = new();

            switch (group.GrupTipi)
            {
                case "NORMAL":
                    emails = await _context.EpostaGrupUyeleri
                        .Where(u => u.GrupId == groupId && u.Durum == "AKTIF")
                        .Select(u => u.Email)
                        .ToListAsync();
                    _logger.LogInformation("Normal group {GroupId}: {Count} emails", groupId, emails.Count);
                    break;

                case "DEBIS":
                    // DEBIS grupları için listeci email kullanılır
                    if (!string.IsNullOrEmpty(group.ListeciEmail))
                    {
                        emails.Add(group.ListeciEmail);
                        _logger.LogInformation("DEBIS group {GroupId}: using listeci email {ListeciEmail}",
                            groupId, group.ListeciEmail);
                    }
                    else
                    {
                        _logger.LogWarning("DEBIS group {GroupId} has no listeci email configured", groupId);
                    }
                    break;

                case "STATIK":
                case "STATIC":
                    // Static groups - get emails from predefined data source
                    emails = await GetStaticGroupEmailsAsync(groupId, group.ViewAdi, group.FilterKosulu);
                    _logger.LogInformation("Static group {GroupId}: {Count} emails from view {ViewName}",
                        groupId, emails.Count, group.ViewAdi);
                    break;

                case "DINAMIK":
                case "DYNAMIC":
                    // Dynamic groups - get emails based on criteria
                    emails = await GetDynamicGroupEmailsAsync(group.ViewAdi, group.FilterKosulu);
                    _logger.LogInformation("Dynamic group {GroupId}: {Count} emails from view {ViewName}",
                        groupId, emails.Count, group.ViewAdi);
                    break;

                default:
                    _logger.LogWarning("Unknown group type {GroupType} for group {GroupId}", group.GrupTipi, groupId);
                    break;
            }

            return ResponseDataModel<List<string>>.SuccessResult(emails,
                $"Grup email adresleri alındı ({emails.Count} adet)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group emails for group {GroupId}", groupId);
            return ResponseDataModel<List<string>>.ErrorResult("Grup email adresleri alınırken hata oluştu", 500);
        }
    }

    /// <summary>
    /// Statik grup email'lerini database'den alır
    /// Statik gruplar: Excel/CSV dosyalarından import edilip EPOSTA_GRUP_UYELERI tablosuna kaydedilen sabit listeler
    /// </summary>
    private async Task<List<string>> GetStaticGroupEmailsAsync(int groupId, string? viewName, string? filterCondition)
    {
        var emails = new List<string>();

        try
        {
            _logger.LogInformation("Resolving static group emails for group {GroupId}, view: {ViewName}", groupId, viewName);

            // Statik gruplar database'deki EPOSTA_GRUP_UYELERI tablosundan GrupId ile alınır
            // ViewName ve FilterCondition şimdilik kullanılmıyor - gelecekte file-based veya view-based static gruplar için kullanılabilir
            emails = await _context.EpostaGrupUyeleri
                .Where(u => u.GrupId == groupId && u.Durum == "AKTIF")
                .Select(u => u.Email)
                .Distinct()
                .ToListAsync();

            _logger.LogInformation("Static group {GroupId} resolved: found {Count} emails", groupId, emails.Count);

            return emails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving static group emails for group {GroupId}", groupId);
            return emails;
        }
    }

    /// <summary>
    /// Dinamik grup email'lerini gerçek zamanlı Oracle view'den alır
    /// ViewName: Oracle view adı (ör: xxper.V_PERSONEL_EMAIL, xxdeu.V_OGRENCI_EMAIL)
    /// FilterCondition: SQL WHERE koşulu (ör: "DEPARTMAN = 'MÜHENDİSLİK'")
    /// </summary>
    private async Task<List<string>> GetDynamicGroupEmailsAsync(string? viewName, string? filterCondition)
    {
        var emails = new List<string>();

        if (string.IsNullOrEmpty(viewName))
        {
            _logger.LogWarning("Dynamic group viewName is null or empty, returning empty list");
            return emails;
        }

        // SECURITY: View name ve filter validation (defense in depth)
        var validationError = ValidateDynamicGroupQuery(viewName, filterCondition);
        if (!string.IsNullOrEmpty(validationError))
        {
            _logger.LogError("SECURITY: Invalid dynamic group query - {Error}. View: {ViewName}, Filter: {FilterCondition}",
                validationError, viewName, filterCondition);
            throw new SecurityException($"Güvenlik: {validationError}");
        }

        try
        {
            _logger.LogInformation("Resolving dynamic group emails for view: {ViewName}, filter: {FilterCondition}",
                viewName, filterCondition ?? "none");

            // Oracle view'den email listesi çek - RAW SQL kullanarak
            var sql = $"SELECT EMAIL FROM {viewName}";

            if (!string.IsNullOrWhiteSpace(filterCondition))
            {
                sql += $" WHERE {filterCondition}";
            }

            _logger.LogInformation("Executing dynamic group SQL: {Sql}", sql);

            // Raw SQL ile sorgu çalıştır - try-finally ile connection leak engelle
            try
            {
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                await _context.Database.OpenConnectionAsync();

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var email = reader.GetString(0);
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        emails.Add(email);
                    }
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }

            _logger.LogInformation("Dynamic group resolved: {ViewName}, found {Count} emails", viewName, emails.Count);

            return emails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving dynamic group emails for view {ViewName} with filter {FilterCondition}",
                viewName, filterCondition);

            // Exception'ı rethrow et - caller handle etsin
            throw new InvalidOperationException(
                $"Dinamik grup sorgusu başarısız oldu (View: {viewName}, Filter: {filterCondition}). " +
                $"Database hatası veya view bulunamadı. Detay: {ex.Message}",
                ex);
        }
    }

    public async Task<ResponseDataModel<ImportMembersResult>> ImportMembersFromFileAsync(int groupId, IFormFile file, int kullaniciId)
    {
        try
        {
            // Grup kontrolü
            var group = await _context.EpostaGruplari.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return ResponseDataModel<ImportMembersResult>.ErrorResult("Grup bulunamadı", 404);

            // Grup tipi kontrolü - sadece STATIK gruplar için izin ver
            var grupTipi = GrupTipiExtensions.ParseSafely(group.GrupTipi);
            if (grupTipi != GrupTipi.STATIK)
                return ResponseDataModel<ImportMembersResult>.ErrorResult("Sadece statik gruplara dosyadan üye eklenebilir", 400);

            // Dosya kontrolü
            if (file == null || file.Length == 0)
                return ResponseDataModel<ImportMembersResult>.ErrorResult("Dosya yüklenmedi", 400);

            var result = new ImportMembersResult();
            var emails = new List<(string Email, string? AdSoyad)>();

            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            // Dosya formatına göre parse et
            using (var stream = file.OpenReadStream())
            {
                if (fileExtension == ".xlsx" || fileExtension == ".xls")
                {
                    emails = await ParseExcelFileAsync(stream);
                }
                else if (fileExtension == ".csv")
                {
                    emails = await ParseCsvFileAsync(stream);
                }
                else if (fileExtension == ".txt")
                {
                    emails = await ParseTxtFileAsync(stream);
                }
                else
                {
                    return ResponseDataModel<ImportMembersResult>.ErrorResult("Desteklenmeyen dosya formatı. Sadece .xlsx, .csv veya .txt dosyaları yüklenebilir", 400);
                }
            }

            result.TotalRows = emails.Count;

            if (emails.Count == 0)
                return ResponseDataModel<ImportMembersResult>.ErrorResult("Dosyada geçerli email adresi bulunamadı", 400);

            if (emails.Count > 10000)
                return ResponseDataModel<ImportMembersResult>.ErrorResult($"Maksimum 10,000 satır yüklenebilir. Dosyanızda {emails.Count} satır var", 400);

            // KRİTİK: Transaction ile veri bütünlüğü sağla (concurrent import'ta data loss önlemi)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ÖNEMLİ: Önce mevcut tüm üyeleri sil (her import'ta baştan yükle)
                // "dosyadan yükleme istediği kadar yapabilir, her yaptığında üyeleri silsin baştan tekrar yüklesin"
                var existingMembers = await _context.EpostaGrupUyeleri
                    .Where(m => m.GrupId == groupId)
                    .ToListAsync();

                if (existingMembers.Any())
                {
                    _context.EpostaGrupUyeleri.RemoveRange(existingMembers);
                    _logger.LogInformation("Deleted {Count} existing members from group {GroupId} before import",
                        existingMembers.Count, groupId);
                }

                // Aynı dosyada duplicate kontrolü için set
                var processedEmails = new HashSet<string>();

                // Her email'i işle
                foreach (var (email, adSoyad) in emails)
                {
                    var normalizedEmail = email.ToLower().Trim();

                    // Email format kontrolü
                    if (!IsValidEmail(normalizedEmail))
                    {
                        result.FailedCount++;
                        result.FailedEmails.Add($"{email} (geçersiz format)");
                        continue;
                    }

                    // Aynı dosyada duplicate kontrolü (database'den değil)
                    if (processedEmails.Contains(normalizedEmail))
                    {
                        result.DuplicateCount++;
                        result.DuplicateEmails.Add(email);
                        continue;
                    }

                    // Yeni üye ekle
                    var newMember = new EpostaGrupUyesi
                    {
                        GrupId = groupId,
                        Email = normalizedEmail,
                        AdSoyad = adSoyad ?? email,
                        Durum = "AKTIF",
                        OlusturmaTarihi = DateTime.Now,
                        EklenmeTarihi = DateTime.Now
                    };

                    _context.EpostaGrupUyeleri.Add(newMember);
                    processedEmails.Add(normalizedEmail); // Aynı dosyada duplicate olmaması için
                    result.SuccessCount++;
                }

                await _context.SaveChangesAsync();

                // HYBRID APPROACH: Backend'de UYE_SAYISI güncelle
                await UpdateGroupMemberCountAsync(groupId);

                await transaction.CommitAsync();

                result.Message = $"{result.SuccessCount} üye başarıyla eklendi.";
                if (result.DuplicateCount > 0)
                    result.Message += $" {result.DuplicateCount} duplicate atlandı.";
                if (result.FailedCount > 0)
                    result.Message += $" {result.FailedCount} geçersiz email atlandı.";

                _logger.LogInformation("Imported {SuccessCount} members to group {GroupId} from file {FileName} by user {UserId}",
                    result.SuccessCount, groupId, file.FileName, kullaniciId);

                return ResponseDataModel<ImportMembersResult>.SuccessResult(result, result.Message);
            }
            catch (Exception innerEx)
            {
                await transaction.RollbackAsync();
                _logger.LogError(innerEx, "Transaction error importing members for group {GroupId}", groupId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing members from file for group {GroupId}", groupId);
            return ResponseDataModel<ImportMembersResult>.ErrorResult("Dosyadan üye eklenirken bir hata oluştu", 500);
        }
    }

    private Task<List<(string Email, string? AdSoyad)>> ParseExcelFileAsync(Stream stream)
    {
        var emails = new List<(string, string?)>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (int row = 2; row <= rowCount; row++) // Başlık satırını atla
        {
            var email = worksheet.Cell(row, 1).GetString().Trim();
            var adSoyad = worksheet.Cell(row, 2).GetString().Trim();

            if (!string.IsNullOrWhiteSpace(email))
                emails.Add((email, string.IsNullOrWhiteSpace(adSoyad) ? null : adSoyad));
        }

        return Task.FromResult(emails);
    }

    private async Task<List<(string Email, string? AdSoyad)>> ParseCsvFileAsync(Stream stream)
    {
        var emails = new List<(string, string?)>();
        using var reader = new StreamReader(stream);

        // Başlık satırını atla
        await reader.ReadLineAsync();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            var email = parts[0].Trim().Trim('"');
            var adSoyad = parts.Length > 1 ? parts[1].Trim().Trim('"') : null;

            if (!string.IsNullOrWhiteSpace(email))
                emails.Add((email, adSoyad));
        }

        return emails;
    }

    private async Task<List<(string Email, string? AdSoyad)>> ParseTxtFileAsync(Stream stream)
    {
        var emails = new List<(string, string?)>();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var email = line.Trim();
            if (!string.IsNullOrWhiteSpace(email))
                emails.Add((email, null));
        }

        return emails;
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            if (!emailRegex.IsMatch(email))
                return false;

            // SECURITY: Sadece @deu.edu.tr domain'ine izin ver
            if (!email.EndsWith("@deu.edu.tr", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("SECURITY: Non-DEU email rejected: {Email}", email);
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Dinamik grup view önizlemesi - View'den ilk 10 kaydı getirir ve validasyon yapar
    /// </summary>
    public async Task<ResponseDataModel<DynamicGroupPreviewDto>> PreviewDynamicGroupAsync(string viewAdi, string? filterKosulu)
    {
        var preview = new DynamicGroupPreviewDto
        {
            ViewAdi = viewAdi,
            FilterKosulu = filterKosulu,
            IsValid = false
        };

        try
        {
            if (string.IsNullOrWhiteSpace(viewAdi))
            {
                preview.ErrorMessage = "View adı boş olamaz";
                return ResponseDataModel<DynamicGroupPreviewDto>.ErrorResult(preview.ErrorMessage, 400);
            }

            // SECURITY: View name ve filter validation
            var validationError = ValidateDynamicGroupQuery(viewAdi, filterKosulu);
            if (!string.IsNullOrEmpty(validationError))
            {
                preview.ErrorMessage = validationError;
                _logger.LogWarning("SECURITY: Invalid dynamic group query - {Error}. View: {ViewAdi}, Filter: {FilterKosulu}",
                    validationError, viewAdi, filterKosulu);
                return ResponseDataModel<DynamicGroupPreviewDto>.ErrorResult(preview.ErrorMessage, 400);
            }

            _logger.LogInformation("Previewing dynamic group view: {ViewAdi}, filter: {FilterKosulu}",
                viewAdi, filterKosulu ?? "none");

            // Oracle view'den email listesi çek - RAW SQL kullanarak
            var sql = $"SELECT EMAIL, AD_SOYAD FROM {viewAdi}";

            if (!string.IsNullOrWhiteSpace(filterKosulu))
            {
                sql += $" WHERE {filterKosulu}";
            }

            // İlk 10 kayıt için ROWNUM kullan (Oracle syntax)
            sql = $"SELECT * FROM ({sql}) WHERE ROWNUM <= 10";

            _logger.LogInformation("Executing preview SQL: {Sql}", sql);

            // Raw SQL ile sorgu çalıştır - try-finally ile connection leak engelle
            var members = new List<EmailGroupMemberDto>();
            var totalCount = 0;

            try
            {
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                await _context.Database.OpenConnectionAsync();

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var email = reader.GetString(0);
                    var adSoyad = reader.IsDBNull(1) ? email : reader.GetString(1);

                    members.Add(new EmailGroupMemberDto
                    {
                        Email = email,
                        AdSoyad = adSoyad
                    });
                    totalCount++;
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }

            // Toplam üye sayısını almak için ayrı sorgu
            var countSql = $"SELECT COUNT(*) FROM {viewAdi}";
            if (!string.IsNullOrWhiteSpace(filterKosulu))
            {
                countSql += $" WHERE {filterKosulu}";
            }

            using var countCommand = _context.Database.GetDbConnection().CreateCommand();
            countCommand.CommandText = countSql;
            await _context.Database.OpenConnectionAsync();

            var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
            await _context.Database.CloseConnectionAsync();

            preview.ToplamUye = total;
            preview.OnizlemeUyeler = members;
            preview.IsValid = true;

            _logger.LogInformation("Preview successful: {Count} members found in view {ViewAdi}", total, viewAdi);

            return ResponseDataModel<DynamicGroupPreviewDto>.SuccessResult(
                preview,
                $"View sorgusu başarılı. Toplam {total} üye bulundu."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing dynamic group view: {ViewAdi}", viewAdi);

            preview.ErrorMessage = ex.Message.Contains("ORA-")
                ? $"View sorgusu hatası: {ex.Message}"
                : "View sorgulanırken hata oluştu. View adını ve filter koşulunu kontrol edin.";

            preview.IsValid = false;

            return ResponseDataModel<DynamicGroupPreviewDto>.ErrorResult(preview.ErrorMessage, 400);
        }
    }

    /// <summary>
    /// Dinamik grup view ve filter validation (SQL injection prevention)
    /// </summary>
    /// <returns>Hata mesajı (varsa), yoksa null/empty</returns>
    private static string? ValidateDynamicGroupQuery(string viewName, string? filterCondition)
    {
        // View adı naming convention kontrolü
        if (!viewName.StartsWith("V_EMAIL_", StringComparison.OrdinalIgnoreCase))
        {
            return "View adı V_EMAIL_ ile başlamalı";
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(viewName, @"^[A-Z_][A-Z0-9_]*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return "View adı sadece alfanumerik karakterler ve underscore içermelidir";
        }

        // Filter koşulu validation
        if (!string.IsNullOrWhiteSpace(filterCondition))
        {
            if (filterCondition.Contains(";") || filterCondition.Contains("--") ||
                filterCondition.Contains("/*") || filterCondition.Contains("*/") ||
                filterCondition.ToUpper().Contains("DROP") || filterCondition.ToUpper().Contains("DELETE") ||
                filterCondition.ToUpper().Contains("INSERT") || filterCondition.ToUpper().Contains("UPDATE") ||
                filterCondition.ToUpper().Contains("EXEC") || filterCondition.ToUpper().Contains("UNION"))
            {
                return "Filter koşulunda geçersiz karakterler veya SQL komutları bulundu";
            }
        }

        return null; // Validation passed
    }

    /// <summary>
    /// HYBRID APPROACH: Backend'de grup üye sayısını hesaplar ve günceller
    /// Sadece NORMAL/STATIK gruplarda çağrılmalı (DINAMIK/DEBIS için gerek yok)
    /// </summary>
    private async Task UpdateGroupMemberCountAsync(int groupId)
    {
        try
        {
            var group = await _context.EpostaGruplari.FindAsync(groupId);
            if (group == null)
            {
                _logger.LogWarning("UpdateGroupMemberCount: Group {GroupId} not found", groupId);
                return;
            }

            // Grup tipine göre hesaplama
            int uyeSayisi = 0;

            switch (group.GrupTipi)
            {
                case "NORMAL":
                case "STATIK":
                    // AKTIF üyeleri say
                    uyeSayisi = await _context.EpostaGrupUyeleri
                        .CountAsync(u => u.GrupId == groupId && u.Durum == "AKTIF");
                    break;

                case "DINAMIK":
                    // DINAMIK gruplar için view'den COUNT (opsiyonel - çağrılmamalı normalde)
                    if (!string.IsNullOrEmpty(group.ViewAdi))
                    {
                        try
                        {
                            var emails = await GetDynamicGroupEmailsAsync(group.ViewAdi, group.FilterKosulu);
                            uyeSayisi = emails.Count;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "UpdateGroupMemberCount: Dynamic group {GroupId} count failed", groupId);
                            uyeSayisi = 0;
                        }
                    }
                    break;

                case "DEBIS":
                    // DEBIS gruplar için sabit 1
                    uyeSayisi = 1;
                    break;

                default:
                    _logger.LogWarning("UpdateGroupMemberCount: Unknown group type {GrupTipi} for group {GroupId}",
                        group.GrupTipi, groupId);
                    break;
            }

            // UYE_SAYISI güncelle
            group.UyeSayisi = uyeSayisi;
            group.GuncellemeTarihi = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("UpdateGroupMemberCount: Group {GroupId} ({GrupTipi}) updated to {UyeSayisi} members",
                groupId, group.GrupTipi, uyeSayisi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateGroupMemberCount: Error updating member count for group {GroupId}", groupId);
            // Silent fail - UYE_SAYISI güncelleme hatası kritik değil, asıl işlem başarılı
        }
    }
}

// DTOs moved to Models/DTOs/EmailGroupDTOs.cs