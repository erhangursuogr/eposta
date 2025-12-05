using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Models.Enums;
using DeuEposta.Utils;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

public interface IUserService
{
    Task<ResponseDataModel<List<UserListView>>> GetUsersAsync(string? search, string? role, string activeOnly, int page, int pageSize);
    Task<ResponseDataModel<UserDetailView>> GetUserByIdAsync(int id);
    Task<ResponseDataModel<UserDetailView>> CreateUserAsync(CreateUserRequest request, int createdByUserId);
    Task<ResponseDataModel<UserDetailView>> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<ResponseModel> DeleteUserAsync(int id);
    Task<ResponseDataModel<UserStatistics>> GetUserStatisticsAsync();
    Task<ResponseDataModel<List<ApproverView>>> GetApproversAsync();
}

public class UserService : IUserService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IAuditLogService _auditLog;

    public UserService(DeuEpostaContext context, ILogger<UserService> logger, IAuditLogService auditLog)
    {
        _context = context;
        _logger = logger;
        _auditLog = auditLog;
    }

    public async Task<ResponseDataModel<List<UserListView>>> GetUsersAsync(string? search, string? role, string activeOnly, int page, int pageSize)
    {
        try
        {
            // GÜVENLİK: Pagination parametrelerini normalize et (OOM ve negatif Skip önleme)
            (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

            var query = _context.Kullanicilar.Include(u => u.Rol).AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(activeOnly))
            {
                query = query.Where(u => u.Aktif == activeOnly);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.KullaniciAdi.Contains(search) ||
                                       u.AdSoyad.Contains(search) ||
                                       u.Email.Contains(search) ||
                                       (u.Departman != null && u.Departman.Contains(search)));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Rol!.RolKodu == role);
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Rol!.YetkiSeviyesi)
                .ThenBy(u => u.AdSoyad)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListView
                {
                    Id = u.Id,
                    KullaniciAdi = u.KullaniciAdi,
                    AdSoyad = u.AdSoyad,
                    Email = u.Email,
                    Departman = u.Departman,
                    Unvan = u.Unvan,
                    Rol = u.Rol!.RolKodu,
                    RolKodu = u.Rol!.RolKodu,
                    RolAdi = u.Rol!.RolAdi,
                    Aktif = u.Aktif,
                    SonGirisTarihi = u.SonGirisTarihi,
                    OlusturmaTarihi = u.OlusturmaTarihi
                })
                .ToListAsync();

            return ResponseDataModel<List<UserListView>>.SuccessResultWithPagination(
                users, totalCount, page, pageSize, "Kullanıcılar alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return ResponseDataModel<List<UserListView>>.ErrorResult("Kullanıcı listesi alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<UserDetailView>> GetUserByIdAsync(int id)
    {
        try
        {
            var user = await _context.Kullanicilar
                .Include(u => u.Rol)
                .Where(u => u.Id == id)
                .Select(u => new UserDetailView
                {
                    Id = u.Id,
                    KullaniciAdi = u.KullaniciAdi,
                    AdSoyad = u.AdSoyad,
                    Email = u.Email,
                    Departman = u.Departman,
                    Unvan = u.Unvan,
                    RolId = u.RolId,
                    Rol = u.Rol!.RolKodu,
                    RolKodu = u.Rol!.RolKodu,
                    RolAdi = u.Rol!.RolAdi,
                    Aktif = u.Aktif,
                    SonGirisTarihi = u.SonGirisTarihi,
                    OlusturmaTarihi = u.OlusturmaTarihi,
                    GuncellemeTarihi = u.GuncellemeTarihi
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return ResponseDataModel<UserDetailView>.ErrorResult("Kullanıcı bulunamadı", 404);

            return ResponseDataModel<UserDetailView>.SuccessResult(user, "Kullanıcı bilgileri alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return ResponseDataModel<UserDetailView>.ErrorResult("Kullanıcı bilgileri alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<UserDetailView>> CreateUserAsync(CreateUserRequest request, int createdByUserId)
    {
        try
        {
            // Email domain validation
            if (!request.Email.EndsWith("@deu.edu.tr", StringComparison.OrdinalIgnoreCase))
                return ResponseDataModel<UserDetailView>.ErrorResult("Sadece @deu.edu.tr uzantılı email adresleri kabul edilir", 400);

            // Email uniqueness check
            var existingUser = await _context.Kullanicilar
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
                return ResponseDataModel<UserDetailView>.ErrorResult("Bu email adresi zaten kayıtlı", 400);

            // Extract username from email (before @)
            var kullaniciAdi = request.Email.Split('@')[0];

            // Role validation
            var role = await _context.Roller.FirstOrDefaultAsync(r => r.Id == request.RolId && r.Aktif == "Y");
            if (role == null)
                return ResponseDataModel<UserDetailView>.ErrorResult("Geçersiz rol seçimi", 400);

            // Create user with minimal info (email + rol + aktif)
            // AdSoyad, Departman, Unvan will be updated from Oracle 11g on first login
            var newUser = new Kullanici
            {
                KullaniciAdi = kullaniciAdi,
                AdSoyad = kullaniciAdi, // Temporary, will be updated from Oracle 11g on first login
                Email = request.Email,
                Departman = null,
                Unvan = null,
                RolId = request.RolId,
                Aktif = request.Aktif,
                OlusturmaTarihi = DateTime.Now
            };

            _context.Kullanicilar.Add(newUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user created: {Email} with role {Role} by admin {AdminId}. User details will be fetched from Oracle 11g on first login.",
                newUser.Email, role.RolKodu, createdByUserId);

            // Audit log: Kullanıcı oluşturma
            await _auditLog.LogAsync(
                kategori: "USER",
                islem: "KULLANICI_OLUSTURMA",
                detay: $"Yeni kullanıcı oluşturuldu. ID: {newUser.Id}, Email: {newUser.Email}, Rol: {role.RolAdi}, Durum: {request.Aktif}. Kullanıcı bilgileri ilk girişte Personel Otomasyonundan çekilecek.",
                kullaniciId: createdByUserId
            );

            return await GetUserByIdAsync(newUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return ResponseDataModel<UserDetailView>.ErrorResult("Kullanıcı oluşturulurken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<UserDetailView>> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        try
        {
            var user = await _context.Kullanicilar
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ResponseDataModel<UserDetailView>.ErrorResult("Kullanıcı bulunamadı", 404);

            // Email uniqueness check (exclude current user)
            var existingUserWithEmail = await _context.Kullanicilar
                .Where(u => u.Email == request.Email && u.Id != id)
                .FirstOrDefaultAsync();

            if (existingUserWithEmail != null)
                return ResponseDataModel<UserDetailView>.ErrorResult("Bu email adresi başka bir kullanıcı tarafından kullanılıyor", 400);

            // Role validation
            var newRole = await _context.Roller.FirstOrDefaultAsync(r => r.Id == request.RolId && r.Aktif == "Y");
            if (newRole == null)
                return ResponseDataModel<UserDetailView>.ErrorResult("Geçersiz rol seçimi", 400);

            var oldRoleId = user.RolId;
            var oldRoleName = user.Rol?.RolAdi;
            var oldAktif = user.Aktif;

            // Update user
            user.AdSoyad = request.AdSoyad;
            user.Email = request.Email;
            user.Departman = request.Departman;
            user.Unvan = request.Unvan;
            user.RolId = request.RolId;
            user.Aktif = request.Aktif;
            user.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated successfully", user.Id);

            // Audit log: Kullanıcı güncelleme
            var changes = new List<string>();
            if (oldRoleId != request.RolId)
                changes.Add($"Rol: {oldRoleName} → {newRole.RolAdi}");
            if (oldAktif != request.Aktif)
                changes.Add($"Durum: {oldAktif} → {request.Aktif}");

            if (changes.Any())
            {
                await _auditLog.LogAsync(
                    kategori: "USER",
                    islem: "KULLANICI_GUNCELLEME",
                    detay: $"Kullanıcı güncellendi. ID: {id}, Ad: {user.AdSoyad}, Email: {user.Email}, Değişiklikler: {string.Join(", ", changes)}",
                    kullaniciId: null // Admin tarafından yapılıyor, controller'dan alınmalı
                );
            }

            return await GetUserByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return ResponseDataModel<UserDetailView>.ErrorResult("Kullanıcı güncellenirken hata oluştu", 500);
        }
    }

    public async Task<ResponseModel> DeleteUserAsync(int id)
    {
        try
        {
            var user = await _context.Kullanicilar
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ResponseModel.ErrorResult("Kullanıcı bulunamadı", 404);

            // Kullanıcının sistemde kaydı var mı kontrol et (Oracle uyumlu - Count kullan)
            var announcementsCount = await _context.EpostaDuyurulari
                .CountAsync(d => d.OlusturanKullaniciId == id);

            var firstApproverCount = await _context.EpostaDuyurulari
                .CountAsync(d => d.IlkOnaylayanKullaniciId == id);

            var finalApproverCount = await _context.EpostaDuyurulari
                .CountAsync(d => d.SonOnaylayanKullaniciId == id);

            var logsCount = await _context.LogSistem
                .CountAsync(l => l.KullaniciId == id);

            // Eğer kullanıcının herhangi bir kaydı varsa sadece pasif yap
            if (announcementsCount > 0 || firstApproverCount > 0 || finalApproverCount > 0 || logsCount > 0)
            {
                user.Aktif = "N";
                user.GuncellemeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deactivated (has records)", user.Id);

                await _auditLog.LogAsync(
                    kategori: "USER",
                    islem: "KULLANICI_PASIF_YAPMA",
                    detay: $"Kullanıcı pasif yapıldı (sistemde kayıtları var). ID: {id}, Ad: {user.AdSoyad}, Email: {user.Email}",
                    kullaniciId: null
                );

                return ResponseModel.ErrorResult("Kullanıcının sistemde kayıtları bulunduğu için silinemedi. Kullanıcı pasif yapıldı.", 400);
            }

            // Hiç kaydı yoksa gerçekten sil (hard delete)
            _context.Kullanicilar.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} permanently deleted (no records)", user.Id);

            // NOT: Audit log yazmıyoruz çünkü kullanıcı silindi, log'da foreign key hatası olur

            return ResponseModel.SuccessResult("Kullanıcı kalıcı olarak silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return ResponseModel.ErrorResult("Kullanıcı silinirken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<UserStatistics>> GetUserStatisticsAsync()
    {
        try
        {
            var stats = new UserStatistics
            {
                TotalUsers = await _context.Kullanicilar.CountAsync(),
                ActiveUsers = await _context.Kullanicilar.CountAsync(u => u.Aktif == "Y"),
                InactiveUsers = await _context.Kullanicilar.CountAsync(u => u.Aktif == "N"),
                AdminCount = await _context.Kullanicilar.Include(u => u.Rol).CountAsync(u => u.Rol!.RolKodu == RolKodu.ADMIN && u.Aktif == "Y"),
                ManagerCount = await _context.Kullanicilar.Include(u => u.Rol).CountAsync(u => u.Rol!.RolKodu == RolKodu.MANAGER && u.Aktif == "Y"),
                ModeratorCount = await _context.Kullanicilar.Include(u => u.Rol).CountAsync(u => u.Rol!.RolKodu == "MODERATOR" && u.Aktif == "Y"),
                EditorCount = await _context.Kullanicilar.Include(u => u.Rol).CountAsync(u => u.Rol!.RolKodu == RolKodu.EDITOR && u.Aktif == "Y"),
                ViewerCount = await _context.Kullanicilar.Include(u => u.Rol).CountAsync(u => u.Rol!.RolKodu == RolKodu.VIEWER && u.Aktif == "Y")
            };

            return ResponseDataModel<UserStatistics>.SuccessResult(stats, "Kullanıcı istatistikleri alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics");
            return ResponseDataModel<UserStatistics>.ErrorResult("İstatistikler alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<ApproverView>>> GetApproversAsync()
    {
        try
        {
            var approvers = await _context.Kullanicilar
                .Include(u => u.Rol)
                .Where(u => u.Aktif == "Y" && u.Rol!.RolKodu == RolKodu.MANAGER)
                .OrderBy(u => u.AdSoyad)
                .Select(u => new ApproverView
                {
                    Id = u.Id,
                    AdSoyad = u.AdSoyad,
                    Email = u.Email,
                    Departman = u.Departman,
                    Unvan = u.Unvan,
                    RolKodu = u.Rol!.RolKodu,
                    RolAdi = u.Rol!.RolAdi
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} approvers", approvers.Count);

            return ResponseDataModel<List<ApproverView>>.SuccessResult(approvers,
                $"{approvers.Count} onaylayıcı bulundu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approvers");
            return ResponseDataModel<List<ApproverView>>.ErrorResult("Onaylayıcılar alınırken hata oluştu", 500);
        }
    }
}
