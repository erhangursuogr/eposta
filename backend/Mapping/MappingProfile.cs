using AutoMapper;
using DeuEposta.Models;
using DeuEposta.Models.DTOs;
using DeuEposta.Services;

namespace DeuEposta.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Template mappings
        CreateMap<EpostaSablon, TemplateDetailView>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SablonAdi, opt => opt.MapFrom(src => src.SablonAdi))
            .ForMember(dest => dest.KonuSablonu, opt => opt.MapFrom(src => src.KonuSablonu))
            .ForMember(dest => dest.IcerikSablonu, opt => opt.MapFrom(src => src.IcerikSablonu))
            .ForMember(dest => dest.KategoriId, opt => opt.MapFrom(src => src.KategoriId))
            .ForMember(dest => dest.Varsayilan, opt => opt.MapFrom(src => src.Varsayilan))
            .ForMember(dest => dest.Aktif, opt => opt.MapFrom(src => src.Aktif))
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.MapFrom(src => src.OlusturmaTarihi))
            .ForMember(dest => dest.GuncellemeTarihi, opt => opt.MapFrom(src => src.GuncellemeTarihi))
            .ForMember(dest => dest.KullanimSayisi, opt => opt.Ignore()) // Service'de manuel set edilecek
            .ForMember(dest => dest.Kategori, opt => opt.MapFrom(src => src.Kategori != null ? new TemplateCategoryInfo
            {
                Id = src.Kategori.Id,
                KategoriAdi = src.Kategori.KategoriAdi,
                Renk = src.Kategori.Renk,
                Ikon = src.Kategori.Ikon
            } : null));

        CreateMap<CreateTemplateRequest, EpostaSablon>()
            .ForMember(dest => dest.SablonAdi, opt => opt.MapFrom(src => src.Ad))
            .ForMember(dest => dest.KonuSablonu, opt => opt.MapFrom(src => src.Konu))
            .ForMember(dest => dest.IcerikSablonu, opt => opt.MapFrom(src => src.Icerik))
            .ForMember(dest => dest.KategoriId, opt => opt.MapFrom(src => src.KategoriId))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Varsayilan, opt => opt.Ignore())
            .ForMember(dest => dest.Aktif, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.GuncellemeTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.Kategori, opt => opt.Ignore())
            .ForMember(dest => dest.Duyurular, opt => opt.Ignore());

        CreateMap<UpdateTemplateRequest, EpostaSablon>()
            .ForMember(dest => dest.SablonAdi, opt => opt.MapFrom(src => src.Ad))
            .ForMember(dest => dest.KonuSablonu, opt => opt.MapFrom(src => src.Konu))
            .ForMember(dest => dest.IcerikSablonu, opt => opt.MapFrom(src => src.Icerik))
            .ForMember(dest => dest.KategoriId, opt => opt.MapFrom(src => src.KategoriId))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Varsayilan, opt => opt.Ignore())
            .ForMember(dest => dest.Aktif, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.GuncellemeTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.Kategori, opt => opt.Ignore())
            .ForMember(dest => dest.Duyurular, opt => opt.Ignore());

        // Announcement mappings
        CreateMap<EpostaDuyuru, AnnouncementDetailView>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Konu, opt => opt.MapFrom(src => src.Konu))
            .ForMember(dest => dest.Icerik, opt => opt.MapFrom(src => src.Icerik))
            .ForMember(dest => dest.DuyuruDurumu, opt => opt.MapFrom(src => src.Durum))
            .ForMember(dest => dest.DuyuruKategorisi, opt => opt.MapFrom(src => src.DuyuruKategorisi))
            .ForMember(dest => dest.GondericiKategori, opt => opt.MapFrom(src => src.GondericiKategori))
            .ForMember(dest => dest.GonderimTarihi, opt => opt.MapFrom(src => src.GercekGonderimTarihi))
            .ForMember(dest => dest.OnayTarihi, opt => opt.MapFrom(src =>
                src.Hareketler
                    .Where(h => h.IslemTipi == "ONAYLAMA" && h.YeniDurum == "ONAYLANDI")
                    .OrderByDescending(h => h.IslemTarihi)
                    .Select(h => h.IslemTarihi)
                    .FirstOrDefault()))
            .ForMember(dest => dest.OnayNotu, opt => opt.MapFrom(src =>
                src.Hareketler
                    .Where(h => h.IslemTipi == "ONAYLAMA" && h.YeniDurum == "ONAYLANDI")
                    .OrderByDescending(h => h.IslemTarihi)
                    .Select(h => h.Aciklama)
                    .FirstOrDefault()))
            .ForMember(dest => dest.SablonId, opt => opt.MapFrom(src => src.SablonId))
            .ForMember(dest => dest.SablonAdi, opt => opt.MapFrom(src => src.Sablon != null ? src.Sablon.SablonAdi : null))
            .ForMember(dest => dest.OlusturanKullaniciId, opt => opt.MapFrom(src => src.OlusturanKullaniciId))
            .ForMember(dest => dest.OlusturanKullaniciAdi, opt => opt.MapFrom(src => src.OlusturanKullanici != null ? src.OlusturanKullanici.AdSoyad : null))
            .ForMember(dest => dest.OnaylayanKullaniciId, opt => opt.MapFrom(src => src.SonOnaylayanKullaniciId))
            .ForMember(dest => dest.OnaylayanKullaniciAdi, opt => opt.MapFrom(src => src.SonOnaylayanKullanici != null ? src.SonOnaylayanKullanici.AdSoyad : null))
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.MapFrom(src => src.OlusturmaTarihi))
            .ForMember(dest => dest.GuncellemeTarihi, opt => opt.MapFrom(src => src.GuncellemeTarihi));

        CreateMap<CreateAnnouncementRequest, EpostaDuyuru>()
            .ForMember(dest => dest.Konu, opt => opt.MapFrom(src => src.Konu))
            .ForMember(dest => dest.Icerik, opt => opt.MapFrom(src => src.Icerik))
            .ForMember(dest => dest.SablonId, opt => opt.MapFrom(src => src.SablonId))
            .ForMember(dest => dest.BannerDosyaId, opt => opt.MapFrom(src => src.BannerDosyaId))
            .ForMember(dest => dest.DuyuruKategorisi, opt => opt.MapFrom(src => src.DuyuruKategorisi))
            .ForMember(dest => dest.GondericiKategori, opt => opt.MapFrom(src => src.GondericiKategori))
            .ForMember(dest => dest.SonOnaylayanKullaniciId, opt => opt.MapFrom(src => src.OnaylayanKullaniciId))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturanKullaniciId, opt => opt.Ignore())
            .ForMember(dest => dest.Durum, opt => opt.Ignore())
            .ForMember(dest => dest.GercekGonderimTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.ToplamAliciSayisi, opt => opt.Ignore())
            .ForMember(dest => dest.BasariliGonderimSayisi, opt => opt.Ignore())
            .ForMember(dest => dest.BasarisizGonderimSayisi, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.GuncellemeTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.Sablon, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturanKullanici, opt => opt.Ignore())
            .ForMember(dest => dest.SonOnaylayanKullanici, opt => opt.Ignore())
            .ForMember(dest => dest.BannerDosya, opt => opt.Ignore())
            .ForMember(dest => dest.EkDosyalar, opt => opt.Ignore())
            .ForMember(dest => dest.Alicilar, opt => opt.Ignore());

        CreateMap<UpdateAnnouncementRequest, EpostaDuyuru>()
            .ForMember(dest => dest.Konu, opt => opt.MapFrom(src => src.Konu))
            .ForMember(dest => dest.Icerik, opt => opt.MapFrom(src => src.Icerik))
            .ForMember(dest => dest.SablonId, opt => opt.MapFrom(src => src.SablonId))
            .ForMember(dest => dest.BannerDosyaId, opt => opt.MapFrom(src => src.BannerDosyaId))
            .ForMember(dest => dest.DuyuruKategorisi, opt => opt.MapFrom(src => src.DuyuruKategorisi))
            .ForMember(dest => dest.GondericiKategori, opt => opt.MapFrom(src => src.GondericiKategori))
            .ForMember(dest => dest.SonOnaylayanKullaniciId, opt => opt.MapFrom(src => src.OnaylayanKullaniciId))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturanKullaniciId, opt => opt.Ignore())
            .ForMember(dest => dest.Durum, opt => opt.Ignore())
            .ForMember(dest => dest.GercekGonderimTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.ToplamAliciSayisi, opt => opt.Ignore())
            .ForMember(dest => dest.BasariliGonderimSayisi, opt => opt.Ignore())
            .ForMember(dest => dest.BasarisizGonderimSayisi, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.GuncellemeTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.Sablon, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturanKullanici, opt => opt.Ignore())
            .ForMember(dest => dest.SonOnaylayanKullanici, opt => opt.Ignore())
            .ForMember(dest => dest.BannerDosya, opt => opt.Ignore())
            .ForMember(dest => dest.EkDosyalar, opt => opt.Ignore())
            .ForMember(dest => dest.Alicilar, opt => opt.Ignore());

        // File mappings
        CreateMap<Dosya, FileDetailView>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DosyaAdi, opt => opt.MapFrom(src => src.DosyaAdi))
            .ForMember(dest => dest.DosyaYolu, opt => opt.MapFrom(src => src.DosyaYolu))
            .ForMember(dest => dest.DosyaTipi, opt => opt.MapFrom(src => src.DosyaTipi))
            .ForMember(dest => dest.DosyaBoyutu, opt => opt.MapFrom(src => src.DosyaBoyutu))
            .ForMember(dest => dest.DuyuruId, opt => opt.MapFrom(src => src.DuyuruId))
            .ForMember(dest => dest.Konu, opt => opt.MapFrom(src => src.Duyuru != null ? src.Duyuru.Konu : null))
            .ForMember(dest => dest.YuklemeTarihi, opt => opt.MapFrom(src => src.YuklemeTarihi));

        // Template reverse mapping (DTO -> Entity)
        CreateMap<TemplateDetailView, EpostaSablon>();

        // Announcement reverse mapping (DTO -> Entity)
        CreateMap<AnnouncementDetailView, EpostaDuyuru>()
            .ForMember(dest => dest.Durum, opt => opt.MapFrom(src => src.DuyuruDurumu))
            .ForMember(dest => dest.GercekGonderimTarihi, opt => opt.MapFrom(src => src.GonderimTarihi))
            .ForMember(dest => dest.Sablon, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturanKullanici, opt => opt.Ignore())
            .ForMember(dest => dest.SonOnaylayanKullanici, opt => opt.Ignore())
            .ForMember(dest => dest.BannerDosya, opt => opt.Ignore())
            .ForMember(dest => dest.EkDosyalar, opt => opt.Ignore())
            .ForMember(dest => dest.Alicilar, opt => opt.Ignore());

        // File reverse mapping (DTO -> Entity)
        CreateMap<FileDetailView, Dosya>()
            .ForMember(dest => dest.Duyuru, opt => opt.Ignore())
            .ForMember(dest => dest.YukleyenKullanici, opt => opt.Ignore());

        // Email Group mappings
        CreateMap<EpostaGrubu, EmailGroupListDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.GrupAdi, opt => opt.MapFrom(src => src.GrupAdi))
            .ForMember(dest => dest.Aciklama, opt => opt.MapFrom(src => src.Aciklama))
            .ForMember(dest => dest.GrupTipi, opt => opt.MapFrom(src =>
                DeuEposta.Models.Enums.GrupTipiExtensions.ParseSafely(src.GrupTipi))) // String to Enum conversion
            .ForMember(dest => dest.UyeSayisi, opt => opt.MapFrom(src => src.Uyeler.Count(u => u.Durum == "AKTIF")))
            .ForMember(dest => dest.Aktif, opt => opt.MapFrom(src => src.Aktif)) // String olarak map et
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.MapFrom(src => src.OlusturmaTarihi));

        CreateMap<EpostaGrubu, EmailGroupDetailDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.GrupAdi, opt => opt.MapFrom(src => src.GrupAdi))
            .ForMember(dest => dest.Aciklama, opt => opt.MapFrom(src => src.Aciklama))
            .ForMember(dest => dest.GrupTipi, opt => opt.MapFrom(src =>
                DeuEposta.Models.Enums.GrupTipiExtensions.ParseSafely(src.GrupTipi))) // String to Enum conversion
            .ForMember(dest => dest.ViewAdi, opt => opt.MapFrom(src => src.ViewAdi))
            .ForMember(dest => dest.FilterKosulu, opt => opt.MapFrom(src => src.FilterKosulu))
            .ForMember(dest => dest.Aktif, opt => opt.MapFrom(src => src.Aktif)) // String olarak map et
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.MapFrom(src => src.OlusturmaTarihi))
            .ForMember(dest => dest.GuncellemeTarihi, opt => opt.MapFrom(src => src.GuncellemeTarihi))
            .ForMember(dest => dest.UyeSayisi, opt => opt.Ignore()) // Service'de hesaplanacak
            .ForMember(dest => dest.Uyeler, opt => opt.Ignore()); // Service'de doldurulacak

        // CreateEmailGroupDto -> EpostaGrubu mapping (enum to string conversion)
        CreateMap<CreateEmailGroupDto, EpostaGrubu>()
            .ForMember(dest => dest.GrupAdi, opt => opt.MapFrom(src => src.GrupAdi))
            .ForMember(dest => dest.Aciklama, opt => opt.MapFrom(src => src.Aciklama))
            .ForMember(dest => dest.GrupTipi, opt => opt.MapFrom(src => src.GrupTipi.ToString())) // Enum to string
            .ForMember(dest => dest.ViewAdi, opt => opt.MapFrom(src => src.ViewAdi))
            .ForMember(dest => dest.FilterKosulu, opt => opt.MapFrom(src => src.FilterKosulu))
            .ForMember(dest => dest.ListeciEmail, opt => opt.MapFrom(src => src.ListeciEmail))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Aktif, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.GuncellemeTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.Uyeler, opt => opt.Ignore())
            .ForMember(dest => dest.DuyuruAlicilari, opt => opt.Ignore());

        // User mappings
        CreateMap<Kullanici, UserListView>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.KullaniciAdi, opt => opt.MapFrom(src => src.KullaniciAdi))
            .ForMember(dest => dest.AdSoyad, opt => opt.MapFrom(src => src.AdSoyad))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Departman, opt => opt.MapFrom(src => src.Departman))
            .ForMember(dest => dest.Unvan, opt => opt.MapFrom(src => src.Unvan))
            .ForMember(dest => dest.RolAdi, opt => opt.MapFrom(src => src.Rol != null ? src.Rol.RolAdi : null))
            .ForMember(dest => dest.Aktif, opt => opt.MapFrom(src => src.Aktif == "Y"))
            .ForMember(dest => dest.SonGirisTarihi, opt => opt.MapFrom(src => src.SonGirisTarihi))
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.MapFrom(src => src.OlusturmaTarihi));

        CreateMap<UpdateUserRequest, Kullanici>()
            .ForMember(dest => dest.AdSoyad, opt => opt.MapFrom(src => src.AdSoyad))
            .ForMember(dest => dest.Departman, opt => opt.MapFrom(src => src.Departman))
            .ForMember(dest => dest.Unvan, opt => opt.MapFrom(src => src.Unvan))
            .ForMember(dest => dest.RolId, opt => opt.MapFrom(src => src.RolId))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.KullaniciAdi, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.Aktif, opt => opt.Ignore())
            .ForMember(dest => dest.SonGirisTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturmaTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.GuncellemeTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.Rol, opt => opt.Ignore())
            .ForMember(dest => dest.LoginLoglari, opt => opt.Ignore())
            .ForMember(dest => dest.SistemLoglari, opt => opt.Ignore())
            .ForMember(dest => dest.OlusturulanDuyurular, opt => opt.Ignore())
            .ForMember(dest => dest.IlkOnaylananDuyurular, opt => opt.Ignore())
            .ForMember(dest => dest.SonOnaylananDuyurular, opt => opt.Ignore());
    }
}