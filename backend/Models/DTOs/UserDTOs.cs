namespace DeuEposta.Models.DTOs;

public class UserListView
{
    public int Id { get; set; }
    public string KullaniciAdi { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Departman { get; set; }
    public string? Unvan { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string RolKodu { get; set; } = string.Empty;
    public string RolAdi { get; set; } = string.Empty;
    public string Aktif { get; set; } = string.Empty;
    public DateTime? SonGirisTarihi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}

public class UserDetailView
{
    public int Id { get; set; }
    public string KullaniciAdi { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Departman { get; set; }
    public string? Unvan { get; set; }
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string RolKodu { get; set; } = string.Empty;
    public string RolAdi { get; set; } = string.Empty;
    public string Aktif { get; set; } = string.Empty;
    public DateTime? SonGirisTarihi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public int RolId { get; set; }
    public string Aktif { get; set; } = "Y";
}

public class UpdateUserRequest
{
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Departman { get; set; }
    public string? Unvan { get; set; }
    public int RolId { get; set; }
    public string Aktif { get; set; } = "Y";
}

public class UserStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int AdminCount { get; set; }
    public int ManagerCount { get; set; }
    public int ModeratorCount { get; set; }
    public int EditorCount { get; set; }
    public int ViewerCount { get; set; }
}

public class ApproverView
{
    public int Id { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Departman { get; set; }
    public string? Unvan { get; set; }
    public string RolKodu { get; set; } = string.Empty;
    public string RolAdi { get; set; } = string.Empty;
}