using DeuEposta.Data;
using DeuEposta.Models;
using DeuEposta.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Services;

public interface IDashboardService
{
    Task<ResponseDataModel<DashboardStats>> GetDashboardStatsAsync(int? kullaniciId = null, int currentUserId = 0, bool isAdmin = false, bool isManager = false, bool isCoordinator = false);

    Task<ResponseDataModel<List<RecentActivity>>> GetRecentActivitiesAsync(int count = 10);

    Task<ResponseDataModel<List<AnnouncementChart>>> GetAnnouncementChartDataAsync(int days = 30);

    Task<ResponseDataModel<List<GroupStats>>> GetGroupStatsAsync();

    Task<ResponseDataModel<SystemHealth>> GetSystemHealthAsync();

    Task<ResponseDataModel<List<TopUser>>> GetTopUsersAsync(int count = 5);
}

public class DashboardService : IDashboardService
{
    private readonly DeuEpostaContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(DeuEpostaContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResponseDataModel<DashboardStats>> GetDashboardStatsAsync(int? kullaniciId = null, int currentUserId = 0, bool isAdmin = false, bool isManager = false, bool isCoordinator = false)
    {
        try
        {
            var stats = new DashboardStats();

            // Total announcements
            var announcementQuery = _context.EpostaDuyurulari.AsQueryable();
            if (kullaniciId.HasValue)
                announcementQuery = announcementQuery.Where(d => d.OlusturanKullaniciId == kullaniciId.Value);

            stats.TotalAnnouncements = await announcementQuery.CountAsync();
            stats.DraftAnnouncements = await announcementQuery.CountAsync(d => d.Durum == DuyuruDurum.TASLAK);

            // Pending Announcements - Role based
            var pendingQuery = _context.EpostaDuyurulari.Where(d => d.Durum == DuyuruDurum.ILK_ONAY_BEKLIYOR || d.Durum == DuyuruDurum.SON_ONAY_BEKLIYOR);
            if (!isAdmin && currentUserId > 0)
            {
                if (isCoordinator)
                {
                    // COORDINATOR: Sadece ILK_ONAY_BEKLIYOR durumundaki tüm duyuruları say
                    pendingQuery = pendingQuery.Where(d => d.Durum == "ILK_ONAY_BEKLIYOR");
                }
                else if (isManager)
                {
                    // MANAGER: Kendisine atanan duyuruları say
                    pendingQuery = pendingQuery.Where(d => d.SonOnaylayanKullaniciId == currentUserId);
                }
                else
                {
                    // EDITOR: Kendi oluşturduğu onay bekleyenleri say
                    pendingQuery = pendingQuery.Where(d => d.OlusturanKullaniciId == currentUserId);
                }
            }
            stats.PendingAnnouncements = await pendingQuery.CountAsync();

            stats.ApprovedAnnouncements = await announcementQuery.CountAsync(d => d.Durum == DuyuruDurum.ONAYLANDI);
            stats.SentAnnouncements = await announcementQuery.CountAsync(d => d.Durum == DuyuruDurum.GONDERILDI);

            // Total groups
            stats.TotalGroups = await _context.EpostaGruplari.CountAsync(g => g.Aktif == "Y");
            stats.ActiveGroups = await _context.EpostaGruplari.CountAsync(g => g.Aktif == "Y");

            // Total users
            stats.TotalUsers = await _context.Kullanicilar.CountAsync(u => u.Aktif == "Y");

            // Today's activities (UTC safe)
            var utcToday = DateTime.UtcNow.Date;
            var utcTomorrow = utcToday.AddDays(1);
            stats.TodayAnnouncements = await announcementQuery
                .CountAsync(d => d.OlusturmaTarihi >= utcToday && d.OlusturmaTarihi < utcTomorrow);

            stats.TodayLogins = await _context.LogLogin
                .CountAsync(l => l.GirisTarihi >= utcToday && l.GirisTarihi < utcTomorrow && l.Basarili == "Y");

            // Recent stats (last 7 days) - UTC safe
            var weekAgo = DateTime.UtcNow.Date.AddDays(-7);
            stats.WeekAnnouncements = await announcementQuery
                .CountAsync(d => d.OlusturmaTarihi >= weekAgo);

            stats.WeekSentAnnouncements = await announcementQuery
                .CountAsync(d => d.Durum == DuyuruDurum.GONDERILDI && d.GercekGonderimTarihi.HasValue && d.GercekGonderimTarihi >= weekAgo);

            return ResponseDataModel<DashboardStats>.SuccessResult(stats, "Dashboard istatistikleri alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return ResponseDataModel<DashboardStats>.ErrorResult("Dashboard istatistikleri alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<RecentActivity>>> GetRecentActivitiesAsync(int count = 10)
    {
        try
        {
            var activities = new List<RecentActivity>();

            // Recent announcements
            var recentAnnouncements = await _context.EpostaDuyurulari
                .Include(d => d.OlusturanKullanici)
                .OrderByDescending(d => d.OlusturmaTarihi)
                .Take(count / 2)
                .Select(d => new RecentActivity
                {
                    Type = "DUYURU",
                    Description = $"{d.OlusturanKullanici!.AdSoyad} yeni bir duyuru oluşturdu: {d.Konu}",
                    Date = d.OlusturmaTarihi,
                    UserId = d.OlusturanKullaniciId,
                    UserName = d.OlusturanKullanici.AdSoyad,
                    RelatedId = d.Id
                })
                .ToListAsync();

            activities.AddRange(recentAnnouncements);

            // Recent logins
            var recentLogins = await _context.LogLogin
                .Where(l => l.Basarili == "Y")
                .OrderByDescending(l => l.GirisTarihi)
                .Take(count / 2)
                .Select(l => new RecentActivity
                {
                    Type = "LOGIN",
                    Description = $"{l.KullaniciAdi ?? "Unknown"} sisteme giriş yaptı",
                    Date = l.GirisTarihi,
                    UserId = l.KullaniciId ?? 0,
                    UserName = l.KullaniciAdi ?? "Unknown",
                    IPAddress = l.IpAdres
                })
                .ToListAsync();

            activities.AddRange(recentLogins);

            // Sort by date and take requested count
            var sortedActivities = activities
                .OrderByDescending(a => a.Date)
                .Take(count)
                .ToList();

            return ResponseDataModel<List<RecentActivity>>.SuccessResult(sortedActivities, "Son aktiviteler alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            return ResponseDataModel<List<RecentActivity>>.ErrorResult("Son aktiviteler alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<AnnouncementChart>>> GetAnnouncementChartDataAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            var endDate = startDate.AddDays(days);

            // Single DB query: group by Y/M/D to avoid N+1 query problem
            var grouped = await _context.EpostaDuyurulari
                .Where(d => d.OlusturmaTarihi >= startDate && d.OlusturmaTarihi < endDate)
                .GroupBy(d => new { d.OlusturmaTarihi.Year, d.OlusturmaTarihi.Month, d.OlusturmaTarihi.Day })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Count = g.Count() })
                .ToListAsync();

            var data = Enumerable.Range(0, days)
                .Select(i =>
                {
                    var date = startDate.AddDays(i);
                    var found = grouped.FirstOrDefault(g => g.Year == date.Year && g.Month == date.Month && g.Day == date.Day);
                    return new AnnouncementChart
                    {
                        Date = date,
                        Count = found?.Count ?? 0,
                        DateString = date.ToString("dd/MM")
                    };
                })
                .ToList();

            return ResponseDataModel<List<AnnouncementChart>>.SuccessResult(data, "Duyuru grafik verileri alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcement chart data");
            return ResponseDataModel<List<AnnouncementChart>>.ErrorResult("Duyuru grafik verileri alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<GroupStats>>> GetGroupStatsAsync()
    {
        try
        {
            var stats = await _context.EpostaGruplari
                .Where(g => g.Aktif == "Y")
                .Select(g => new GroupStats
                {
                    GroupId = g.Id,
                    GroupName = g.GrupAdi,
                    GroupType = g.GrupTipi,
                    MemberCount = g.Uyeler.Count(u => u.Durum == "AKTIF"),
                    AnnouncementCount = _context.EpostaDuyuruAlicilari.Count(a => a.GrupId == g.Id)
                })
                .OrderByDescending(s => s.MemberCount)
                .ToListAsync();

            return ResponseDataModel<List<GroupStats>>.SuccessResult(stats, "Grup istatistikleri alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group stats");
            return ResponseDataModel<List<GroupStats>>.ErrorResult("Grup istatistikleri alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<SystemHealth>> GetSystemHealthAsync()
    {
        try
        {
            var health = new SystemHealth();

            // Check database connection
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1 FROM DUAL");
                health.DatabaseStatus = "HEALTHY";
            }
            catch
            {
                health.DatabaseStatus = "ERROR";
                health.Issues.Add("Veritabanı bağlantı hatası");
            }

            // Check disk space (container-safe)
            var configuredUpload = Environment.GetEnvironmentVariable("UPLOAD_PATH");
            var uploadPath = !string.IsNullOrEmpty(configuredUpload) ? configuredUpload : Path.Combine(Directory.GetCurrentDirectory(), "uploads");

            if (Directory.Exists(uploadPath))
            {
                var root = Path.GetPathRoot(uploadPath);
                if (!string.IsNullOrEmpty(root))
                {
                    try
                    {
                        var drive = new DriveInfo(root);
                        var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);

                        if (freeSpaceGB > 5) // 5GB minimum
                        {
                            health.DiskSpaceStatus = "HEALTHY";
                            health.FreeDiskSpaceGB = freeSpaceGB;
                        }
                        else
                        {
                            health.DiskSpaceStatus = "WARNING";
                            health.FreeDiskSpaceGB = freeSpaceGB;
                            health.Issues.Add($"Düşük disk alanı: {freeSpaceGB:F1} GB kaldı");
                        }
                    }
                    catch (Exception ex)
                    {
                        health.DiskSpaceStatus = "ERROR";
                        health.Issues.Add($"Disk alanı kontrol hatası: {ex.Message}");
                        _logger.LogWarning(ex, "Disk space check failed for path: {Path}", uploadPath);
                    }
                }
                else
                {
                    health.DiskSpaceStatus = "UNKNOWN";
                    health.Issues.Add("Upload path root belirlenemedi");
                }
            }
            else
            {
                health.DiskSpaceStatus = "WARNING";
                health.Issues.Add($"Upload dizini bulunamadı: {uploadPath}");
            }

            // Check recent errors (UTC safe)
            var since = DateTime.UtcNow.AddHours(-24);
            var errorCount = await _context.LogSistem
                .CountAsync(l => l.LogSeviye == "ERROR" && l.LogTarihi >= since);

            if (errorCount == 0)
            {
                health.ErrorStatus = "HEALTHY";
            }
            else if (errorCount < 10)
            {
                health.ErrorStatus = "WARNING";
                health.Issues.Add($"Son 24 saatte {errorCount} hata kaydı");
            }
            else
            {
                health.ErrorStatus = "ERROR";
                health.Issues.Add($"Son 24 saatte çok fazla hata: {errorCount}");
            }

            health.RecentErrorCount = errorCount;

            // Overall status
            if (health.Issues.Count == 0)
                health.OverallStatus = "HEALTHY";
            else if (health.Issues.Count <= 2)
                health.OverallStatus = "WARNING";
            else
                health.OverallStatus = "ERROR";

            health.LastCheckTime = DateTime.UtcNow;

            return ResponseDataModel<SystemHealth>.SuccessResult(health, "Sistem sağlık durumu alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return ResponseDataModel<SystemHealth>.ErrorResult("Sistem sağlık durumu alınırken hata oluştu", 500);
        }
    }

    public async Task<ResponseDataModel<List<TopUser>>> GetTopUsersAsync(int count = 5)
    {
        try
        {
            var topUsers = await _context.Kullanicilar
                .Include(u => u.Rol)
                .Where(u => u.Aktif == "Y")
                .Select(u => new TopUser
                {
                    UserId = u.Id,
                    UserName = u.AdSoyad,
                    Email = u.Email,
                    RoleName = u.Rol!.RolAdi,
                    AnnouncementCount = _context.EpostaDuyurulari.Count(d => d.OlusturanKullaniciId == u.Id),
                    LoginCount = 0, // TEMPORARILY DISABLED - navigation removed
                    LastLoginDate = null // TEMPORARILY DISABLED - navigation removed
                })
                .OrderByDescending(u => u.AnnouncementCount)
                .ThenByDescending(u => u.LoginCount)
                .Take(count)
                .ToListAsync();

            return ResponseDataModel<List<TopUser>>.SuccessResult(topUsers, "En aktif kullanıcılar alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top users");
            return ResponseDataModel<List<TopUser>>.ErrorResult("En aktif kullanıcılar alınırken hata oluştu", 500);
        }
    }
}

// DTOs
public class DashboardStats
{
    public int TotalAnnouncements { get; set; }
    public int DraftAnnouncements { get; set; }
    public int PendingAnnouncements { get; set; }
    public int ApprovedAnnouncements { get; set; }
    public int SentAnnouncements { get; set; }
    public int TotalGroups { get; set; }
    public int ActiveGroups { get; set; }
    public int TotalUsers { get; set; }
    public int TodayAnnouncements { get; set; }
    public int TodayLogins { get; set; }
    public int WeekAnnouncements { get; set; }
    public int WeekSentAnnouncements { get; set; }
}

public class RecentActivity
{
    public string Type { get; set; } = string.Empty; // DUYURU, LOGIN, etc.
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? RelatedId { get; set; }
    public string? IPAddress { get; set; }
}

public class AnnouncementChart
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public string DateString { get; set; } = string.Empty;
}

public class GroupStats
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int AnnouncementCount { get; set; }
}

public class SystemHealth
{
    public string OverallStatus { get; set; } = string.Empty; // HEALTHY, WARNING, ERROR
    public string DatabaseStatus { get; set; } = string.Empty;
    public string DiskSpaceStatus { get; set; } = string.Empty;
    public string ErrorStatus { get; set; } = string.Empty;
    public long FreeDiskSpaceGB { get; set; }
    public int RecentErrorCount { get; set; }
    public DateTime LastCheckTime { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class TopUser
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int AnnouncementCount { get; set; }
    public int LoginCount { get; set; }
    public DateTime? LastLoginDate { get; set; }
}