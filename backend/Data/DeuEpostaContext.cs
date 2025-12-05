using DeuEposta.Models;
using Microsoft.EntityFrameworkCore;

namespace DeuEposta.Data;

public class DeuEpostaContext : DbContext
{
    public DeuEpostaContext(DbContextOptions<DeuEpostaContext> options) : base(options)
    {
    }

    public DbSet<Rol> Roller { get; set; }
    public DbSet<Kullanici> Kullanicilar { get; set; }
    public DbSet<EpostaGrubu> EpostaGruplari { get; set; }
    public DbSet<EpostaGrupUyesi> EpostaGrupUyeleri { get; set; }
    public DbSet<EpostaSablonKategori> EpostaSablonKategorileri { get; set; }
    public DbSet<EpostaSablon> EpostaSablonlari { get; set; }
    public DbSet<EpostaDuyuru> EpostaDuyurulari { get; set; }
    public DbSet<EpostaDuyuruHareket> EpostaDuyuruHareketleri { get; set; }
    public DbSet<EpostaDuyuruAlici> EpostaDuyuruAlicilari { get; set; }
    public DbSet<EpostaDuyuruZamanlama> EpostaDuyuruZamanlamalari { get; set; }
    public DbSet<SistemAyar> SistemAyarlari { get; set; }
    public DbSet<LogLogin> LogLogin { get; set; }
    public DbSet<LogSistem> LogSistem { get; set; }
    public DbSet<EpostaDuyuruGonderimLog> EpostaDuyuruGonderimLoglari { get; set; }
    public DbSet<Dosya> Dosyalar { get; set; }
    public DbSet<DuyuruListeView> VDuyurularListe { get; set; }
    public DbSet<DashboardOzetView> VDashboardOzet { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tablo adları
        modelBuilder.Entity<Kullanici>().ToTable("KULLANICILAR");
        modelBuilder.Entity<Rol>().ToTable("ROLLER");
        modelBuilder.Entity<EpostaGrubu>().ToTable("EPOSTA_GRUPLARI");
        modelBuilder.Entity<EpostaGrupUyesi>().ToTable("EPOSTA_GRUP_UYELERI");
        modelBuilder.Entity<EpostaSablonKategori>().ToTable("EPOSTA_SABLON_KATEGORILERI");
        modelBuilder.Entity<EpostaSablon>().ToTable("EPOSTA_SABLONLARI");
        modelBuilder.Entity<EpostaDuyuru>().ToTable("EPOSTA_DUYURULARI");
        modelBuilder.Entity<EpostaDuyuruHareket>().ToTable("EPOSTA_DUYURU_HAREKETLERI");
        modelBuilder.Entity<EpostaDuyuruAlici>().ToTable("EPOSTA_DUYURU_ALICILARI");
        modelBuilder.Entity<EpostaDuyuruZamanlama>().ToTable("EPOSTA_DUYURU_ZAMANLAMALAR");
        modelBuilder.Entity<Dosya>().ToTable("DOSYALAR");
        modelBuilder.Entity<SistemAyar>().ToTable("SISTEM_AYARLARI");
        modelBuilder.Entity<LogLogin>().ToTable("LOG_LOGIN");
        modelBuilder.Entity<LogSistem>().ToTable("LOG_SISTEM");
        modelBuilder.Entity<EpostaDuyuruGonderimLog>().ToTable("EPOSTA_DUYURU_GONDERIM_LOG");

        // View'ler
        modelBuilder.Entity<DuyuruListeView>().ToView("V_DUYURULAR_LISTE").HasNoKey();
        ConfigureDuyuruListeView(modelBuilder);

        modelBuilder.Entity<DashboardOzetView>().ToView("V_DASHBOARD_OZET").HasNoKey();

        // Sequence'lar için Oracle configuration
        modelBuilder.HasSequence("SEQ_ROLLER");
        modelBuilder.HasSequence("SEQ_KULLANICILAR");
        modelBuilder.HasSequence("SEQ_EPOSTA_GRUPLARI");
        modelBuilder.HasSequence("SEQ_EPOSTA_GRUP_UYELERI");
        modelBuilder.HasSequence("SEQ_EPOSTA_SABLON_KATEGORILERI");
        modelBuilder.HasSequence("SEQ_EPOSTA_SABLONLARI");
        modelBuilder.HasSequence("SEQ_EPOSTA_DUYURULARI");
        modelBuilder.HasSequence("SEQ_EPOSTA_DUYURU_HAREKETLERI");
        modelBuilder.HasSequence("SEQ_EPOSTA_DUYURU_ALICILARI");
        modelBuilder.HasSequence("SEQ_EPOSTA_DUYURU_ZAMANLAMALAR");
        modelBuilder.HasSequence("SEQ_DOSYALAR");
        modelBuilder.HasSequence("SEQ_SISTEM_AYARLARI");
        modelBuilder.HasSequence("SEQ_LOG_LOGIN");
        modelBuilder.HasSequence("SEQ_LOG_SISTEM");
        modelBuilder.HasSequence("SEQ_DUYURU_GONDERIM_LOG");

        // Primary Key'ler ve default değerler
        ConfigureRol(modelBuilder);
        ConfigureKullanici(modelBuilder);
        ConfigureEpostaGrubu(modelBuilder);
        ConfigureEpostaGrupUyesi(modelBuilder);
        ConfigureEpostaSablonKategori(modelBuilder);
        ConfigureEpostaSablon(modelBuilder);
        ConfigureEpostaDuyuru(modelBuilder);
        ConfigureEpostaDuyuruHareket(modelBuilder);
        ConfigureEpostaDuyuruAlici(modelBuilder);
        ConfigureEpostaDuyuruZamanlama(modelBuilder);
        ConfigureSistemAyar(modelBuilder);
        ConfigureDosya(modelBuilder);
        ConfigureLogLogin(modelBuilder);
        ConfigureLogSistem(modelBuilder);
        ConfigureEpostaDuyuruGonderimLog(modelBuilder);
    }

    private void ConfigureRol(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Rol>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_ROLLER").HasColumnName("ID");
        entity.Property(e => e.RolKodu).HasMaxLength(20).IsRequired().IsUnicode(false).HasColumnName("ROL_KODU");
        entity.Property(e => e.RolAdi).HasMaxLength(100).IsRequired().IsUnicode(false).HasColumnName("ROL_ADI");
        entity.Property(e => e.Aciklama).HasMaxLength(500).IsUnicode(false).HasColumnName("ACIKLAMA");
        entity.Property(e => e.YetkiSeviyesi).HasColumnName("YETKI_SEVIYESI");
        entity.Property(e => e.Aktif)
            .HasMaxLength(1)
            .HasDefaultValue("Y")
            .IsUnicode(false)
            .IsFixedLength()
            .HasColumnName("AKTIF");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");

        entity.HasIndex(e => e.RolKodu).IsUnique();
    }

    private void ConfigureKullanici(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Kullanici>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_KULLANICILAR").HasColumnName("ID");
        entity.Property(e => e.KullaniciAdi).HasMaxLength(100).IsRequired().IsUnicode(false).HasColumnName("KULLANICI_ADI");
        entity.Property(e => e.AdSoyad).HasMaxLength(200).IsRequired().IsUnicode(false).HasColumnName("AD_SOYAD");
        entity.Property(e => e.Email).HasMaxLength(200).IsRequired().IsUnicode(false).HasColumnName("EMAIL");
        entity.Property(e => e.Departman).HasMaxLength(100).IsUnicode(false).HasColumnName("DEPARTMAN");
        entity.Property(e => e.Unvan).HasMaxLength(100).IsUnicode(false).HasColumnName("UNVAN");
        entity.Property(e => e.GorevYeri).HasColumnName("GOREV_YERI");
        entity.Property(e => e.GorevYeriAdi).HasMaxLength(200).IsUnicode(false).HasColumnName("GOREV_YERI_ADI");
        entity.Property(e => e.RolId).HasColumnName("ROL_ID");
        // entity.Property(e => e.ParolaHash).HasMaxLength(500).IsUnicode(false).HasColumnName("PAROLA_HASH"); // REMOVED - LDAP only
        entity.Property(e => e.Aktif)
            .HasMaxLength(1)
            .HasDefaultValue("Y")
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("AKTIF");
        entity.Property(e => e.SonGirisTarihi).HasColumnName("SON_GIRIS_TARIHI");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");

        entity.HasIndex(e => e.Email).IsUnique();
        entity.HasIndex(e => e.KullaniciAdi).IsUnique();
        entity.HasIndex(e => e.GorevYeri);

        // Foreign Key Relationship
        entity.HasOne(e => e.Rol)
            .WithMany(r => r.Kullanicilar)
            .HasForeignKey(e => e.RolId);
    }

    private void ConfigureEpostaGrubu(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EpostaGrubu>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_EPOSTA_GRUPLARI").HasColumnName("ID");
        entity.Property(e => e.GrupAdi).HasMaxLength(100).IsRequired().IsUnicode(false).HasColumnName("GRUP_ADI");
        entity.Property(e => e.Aciklama).HasMaxLength(500).IsUnicode(false).HasColumnName("ACIKLAMA");
        entity.Property(e => e.GrupTipi).HasMaxLength(20).IsRequired().IsUnicode(false).HasColumnName("GRUP_TIPI");
        // KaynakTipi removed - now using GrupTipi only
        entity.Property(e => e.ViewAdi).HasMaxLength(100).IsUnicode(false).HasColumnName("VIEW_ADI");
        entity.Property(e => e.FilterKosulu).HasMaxLength(500).IsUnicode(false).HasColumnName("FILTER_KOSULU");

        // DEBIS gruplar için listeci email adresi
        entity.Property(e => e.ListeciEmail).HasMaxLength(200).IsUnicode(false).HasColumnName("LISTECI_EMAIL");

        // Grup üye sayısı (STATIK: trigger günceller, DINAMIK: backend hesaplar)
        entity.Property(e => e.UyeSayisi).HasDefaultValue(0).HasColumnName("UYE_SAYISI");

        entity.Property(e => e.Aktif)
            .HasMaxLength(1)
            .HasDefaultValue("Y")
            .IsUnicode(false)
            .IsFixedLength()
            .HasColumnName("AKTIF");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");

        entity.HasIndex(e => e.GrupAdi).IsUnique();
    }

    private void ConfigureEpostaGrupUyesi(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EpostaGrupUyesi>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_EPOSTA_GRUP_UYELERI").HasColumnName("ID");
        entity.Property(e => e.GrupId).HasColumnName("GRUP_ID");
        entity.Property(e => e.Email).HasMaxLength(200).IsRequired().IsUnicode(false).HasColumnName("EMAIL");
        entity.Property(e => e.AdSoyad).HasMaxLength(200).IsUnicode(false).HasColumnName("AD_SOYAD");
        entity.Property(e => e.Departman).HasMaxLength(100).IsUnicode(false).HasColumnName("DEPARTMAN");
        entity.Property(e => e.Durum).HasMaxLength(20).IsUnicode(false).HasDefaultValue("AKTIF").HasColumnName("DURUM");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");
        entity.Property(e => e.EklenmeTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("EKLENME_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");

        entity.HasOne(e => e.Grup)
            .WithMany(g => g.Uyeler)
            .HasForeignKey(e => e.GrupId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => new { e.GrupId, e.Email }).IsUnique();
    }

    private void ConfigureEpostaSablonKategori(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EpostaSablonKategori>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_EPOSTA_SABLON_KATEGORILERI").HasColumnName("ID");
        entity.Property(e => e.KategoriAdi).HasMaxLength(100).IsRequired().IsUnicode(false).HasColumnName("KATEGORI_ADI");
        entity.Property(e => e.Aciklama).HasMaxLength(500).IsUnicode(false).HasColumnName("ACIKLAMA");
        entity.Property(e => e.Renk).HasMaxLength(7).HasDefaultValue("#1976d2").IsUnicode(false).HasColumnName("RENK");
        entity.Property(e => e.Ikon).HasMaxLength(50).HasDefaultValue("label").IsUnicode(false).HasColumnName("IKON");
        entity.Property(e => e.SiraNo).HasDefaultValue(0).HasColumnName("SIRA_NO");
        entity.Property(e => e.Aktif)
            .HasMaxLength(1)
            .HasDefaultValue("Y")
            .IsUnicode(false)
            .IsFixedLength()
            .HasColumnName("AKTIF");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");

        entity.HasIndex(e => e.KategoriAdi).IsUnique();
    }

    private void ConfigureEpostaSablon(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EpostaSablon>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_EPOSTA_SABLONLARI").HasColumnName("ID");
        entity.Property(e => e.SablonAdi).HasMaxLength(100).IsRequired().IsUnicode(false).HasColumnName("SABLON_ADI");
        entity.Property(e => e.KonuSablonu).HasMaxLength(500).IsUnicode(false).HasColumnName("KONU_SABLONU");
        entity.Property(e => e.IcerikSablonu).IsRequired().HasColumnType("CLOB").HasColumnName("ICERIK_SABLONU");
        entity.Property(e => e.KategoriId).HasColumnName("KATEGORI_ID");
        entity.Property(e => e.Varsayilan).HasMaxLength(1).HasDefaultValue("N").IsUnicode(false).IsFixedLength().HasColumnName("VARSAYILAN");
        entity.Property(e => e.Aktif)
            .HasMaxLength(1)
            .HasDefaultValue("Y")
            .IsUnicode(false)
            .IsFixedLength()
            .HasColumnName("AKTIF");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");

        // Foreign Key
        // FIX: OnDelete(DeleteBehavior.SetNull) → Restrict (KATEGORI_ID NOT NULL olduğu için)
        // Kategori silinirken şablon varsa hata verir (veri bütünlüğü korunur)
        // SQL: 03_TABLES.sql:155-157'deki NOT ile uyumlu
        entity.HasOne(e => e.Kategori)
            .WithMany(k => k.Sablonlar)
            .HasForeignKey(e => e.KategoriId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        entity.HasIndex(e => e.SablonAdi).IsUnique();
    }

    private void ConfigureEpostaDuyuru(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EpostaDuyuru>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_EPOSTA_DUYURULARI").HasColumnName("ID");
        entity.Property(e => e.Konu).HasMaxLength(500).IsRequired().IsUnicode(false).HasColumnName("KONU");
        entity.Property(e => e.Icerik).IsRequired().HasColumnType("CLOB").HasColumnName("ICERIK");
        entity.Property(e => e.Aciklama).HasMaxLength(1000).IsUnicode(false).HasColumnName("ACIKLAMA");
        entity.Property(e => e.IcerikTipi).HasMaxLength(30).HasDefaultValue("EMAIL").IsUnicode(false).HasColumnName("ICERIK_TIPI");
        entity.Property(e => e.DuyuruKategorisi).HasMaxLength(50).HasDefaultValue("PERSONEL").IsUnicode(false).HasColumnName("DUYURU_KATEGORISI");
        entity.Property(e => e.GondericiKategori).HasMaxLength(50).HasDefaultValue("EMAIL_PERSONEL").IsUnicode(false).HasColumnName("GONDERICI_KATEGORI");
        entity.Property(e => e.BannerDosyaId).HasColumnName("BANNER_DOSYA_ID");
        entity.Property(e => e.SablonId).HasColumnName("SABLON_ID");
        entity.Property(e => e.OlusturanKullaniciId).HasColumnName("OLUSTURAN_KULLANICI_ID");
        entity.Property(e => e.Durum).HasMaxLength(30).HasDefaultValue("TASLAK").IsUnicode(false).HasColumnName("DURUM");
        entity.Property(e => e.IlkOnaylayanKullaniciId).HasColumnName("ILK_ONAYLAYAN_KULLANICI_ID");
        entity.Property(e => e.SonOnaylayanKullaniciId).HasColumnName("SON_ONAYLAYAN_KULLANICI_ID");
        entity.Property(e => e.GercekGonderimTarihi).HasColumnName("GERCEK_GONDERIM_TARIHI");
        entity.Property(e => e.ToplamAliciSayisi).HasDefaultValue(0).HasColumnName("TOPLAM_ALICI_SAYISI");
        entity.Property(e => e.BasariliGonderimSayisi).HasDefaultValue(0).HasColumnName("BASARILI_GONDERIM_SAYISI");
        entity.Property(e => e.BasarisizGonderimSayisi).HasDefaultValue(0).HasColumnName("BASARISIZ_GONDERIM_SAYISI");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");

        // Foreign Keys with navigation properties
        entity.HasOne(e => e.Sablon)
            .WithMany(s => s.Duyurular)
            .HasForeignKey(e => e.SablonId);

        entity.HasOne(e => e.OlusturanKullanici)
            .WithMany(k => k.OlusturulanDuyurular)
            .HasForeignKey(e => e.OlusturanKullaniciId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();

        entity.HasOne(e => e.IlkOnaylayanKullanici)
            .WithMany(k => k.IlkOnaylananDuyurular)
            .HasForeignKey(e => e.IlkOnaylayanKullaniciId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        entity.HasOne(e => e.SonOnaylayanKullanici)
            .WithMany(k => k.SonOnaylananDuyurular)
            .HasForeignKey(e => e.SonOnaylayanKullaniciId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        entity.HasOne(e => e.BannerDosya)
            .WithMany()
            .HasForeignKey(e => e.BannerDosyaId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasMany(e => e.EkDosyalar)
            .WithOne(d => d.Duyuru)
            .HasForeignKey(d => d.DuyuruId)
            .OnDelete(DeleteBehavior.SetNull);

        // NOT: Hareketler ilişkisi EpostaDuyuruHareket configuration'ında tanımlı (çift tanımdan kaçınmak için)

        // Indexes for performance
        entity.HasIndex(e => e.Durum);
        entity.HasIndex(e => e.IcerikTipi);
        entity.HasIndex(e => e.OlusturanKullaniciId);
        entity.HasIndex(e => e.IlkOnaylayanKullaniciId);
        entity.HasIndex(e => e.SonOnaylayanKullaniciId);
        entity.HasIndex(e => e.OlusturmaTarihi);
    }

    private void ConfigureEpostaDuyuruHareket(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EpostaDuyuruHareket>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_EPOSTA_DUYURU_HAREKETLERI").HasColumnName("ID");
        entity.Property(e => e.DuyuruId).IsRequired().HasColumnName("DUYURU_ID");
        entity.Property(e => e.OncekiDurum).HasMaxLength(30).IsUnicode(false).HasColumnName("ONCEKI_DURUM");
        entity.Property(e => e.YeniDurum).HasMaxLength(30).IsRequired().IsUnicode(false).HasColumnName("YENI_DURUM");
        entity.Property(e => e.IslemTipi).HasMaxLength(30).IsRequired().IsUnicode(false).HasColumnName("ISLEM_TIPI");
        entity.Property(e => e.KullaniciId).HasColumnName("KULLANICI_ID");
        entity.Property(e => e.Aciklama).HasMaxLength(500).IsUnicode(false).HasColumnName("ACIKLAMA");
        entity.Property(e => e.SecilenOnaylayiciId).HasColumnName("SECILEN_ONAYLAYICI_ID");
        entity.Property(e => e.IslemTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("ISLEM_TARIHI");

        // Foreign Keys - Explicit configuration to avoid shadow properties
        entity.HasOne(e => e.Duyuru)
            .WithMany(d => d.Hareketler)
            .HasForeignKey(e => e.DuyuruId)
            .HasConstraintName("FK_HAREKET_DUYURU")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        entity.HasOne(e => e.Kullanici)
            .WithMany()
            .HasForeignKey(e => e.KullaniciId)
            .HasConstraintName("FK_HAREKET_KULLANICI")
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        entity.HasOne(e => e.SecilenOnaylayici)
            .WithMany()
            .HasForeignKey(e => e.SecilenOnaylayiciId)
            .HasConstraintName("FK_HAREKET_ONAYLAYICI")
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Indexes
        entity.HasIndex(e => e.DuyuruId);
        entity.HasIndex(e => e.YeniDurum);
        entity.HasIndex(e => e.IslemTipi);
        entity.HasIndex(e => e.IslemTarihi);
    }

    private void ConfigureEpostaDuyuruAlici(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EpostaDuyuruAlici>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_EPOSTA_DUYURU_ALICILARI").HasColumnName("ID");
        entity.Property(e => e.DuyuruId).HasColumnName("DUYURU_ID");
        entity.Property(e => e.GrupId).HasColumnName("GRUP_ID");
        entity.Property(e => e.AliciTipi).HasMaxLength(20).IsRequired().HasDefaultValue("GRUP").IsUnicode(false).HasColumnName("ALICI_TIPI");
        entity.Property(e => e.AliciKategorisi).HasMaxLength(10).IsRequired().HasDefaultValue("BCC").IsUnicode(false).HasColumnName("ALICI_KATEGORISI");
        entity.Property(e => e.Email).HasMaxLength(200).IsUnicode(false).HasColumnName("EMAIL");
        entity.Property(e => e.AdSoyad).HasMaxLength(200).IsUnicode(false).HasColumnName("AD_SOYAD");
        entity.Property(e => e.GonderimDurumu).HasMaxLength(20).HasDefaultValue("BEKLIYOR").IsUnicode(false).HasColumnName("GONDERIM_DURUMU");
        entity.Property(e => e.HataMesaji).HasMaxLength(500).IsUnicode(false).HasColumnName("HATA_MESAJI");
        entity.Property(e => e.GonderimTarihi).HasColumnName("GONDERIM_TARIHI");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");

        // Foreign Keys with navigation properties
        entity.HasOne(e => e.Duyuru)
            .WithMany(d => d.Alicilar)
            .HasForeignKey(e => e.DuyuruId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Grup)
            .WithMany(g => g.DuyuruAlicilari)
            .HasForeignKey(e => e.GrupId)
            .IsRequired(false);

        // Business rule: ALICI_TIPI'ne göre GrupId veya Email dolu olmalı
        // GRUP tipi -> GRUP_ID dolu, EMAIL grup üyelerinden gelir
        // MANUEL tipi -> GRUP_ID null, EMAIL manuel girilir
        // Check constraint yerine business logic'te kontrol edilecek (daha esnek)
        entity.ToTable(t => t.HasCheckConstraint("CK_Recipient_Type_Valid",
            "(ALICI_TIPI = 'GRUP' AND GRUP_ID IS NOT NULL) OR (ALICI_TIPI = 'MANUEL' AND GRUP_ID IS NULL AND EMAIL IS NOT NULL)"));

        // Indexes for performance
        entity.HasIndex(e => e.DuyuruId);
        entity.HasIndex(e => e.GrupId);
        entity.HasIndex(e => e.AliciKategorisi);
    }

    private void ConfigureSistemAyar(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SistemAyar>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_SISTEM_AYARLARI").HasColumnName("ID");
        entity.Property(e => e.AyarKategori).HasMaxLength(50).IsRequired().IsUnicode(false).HasColumnName("AYAR_KATEGORI");
        entity.Property(e => e.AyarAnahtar).HasMaxLength(100).IsRequired().IsUnicode(false).HasColumnName("AYAR_ANAHTAR");
        entity.Property(e => e.AyarDeger).HasMaxLength(4000).IsUnicode(false).HasColumnName("AYAR_DEGER");
        entity.Property(e => e.AyarAciklama).HasMaxLength(500).IsUnicode(false).HasColumnName("AYAR_ACIKLAMA");
        entity.Property(e => e.GorevYeri).HasColumnName("GOREV_YERI");
        entity.Property(e => e.Gizli)
            .HasMaxLength(1)
            .HasDefaultValueSql("'N'")
            .IsUnicode(false)
            .IsFixedLength()
            .HasColumnName("GIZLI");
        entity.Property(e => e.Aktif)
            .HasMaxLength(1)
            .HasDefaultValueSql("'Y'")
            .IsUnicode(false)
            .IsFixedLength()
            .HasColumnName("AKTIF");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");

        entity.HasIndex(e => new { e.AyarKategori, e.AyarAnahtar }).IsUnique();
        entity.HasIndex(e => e.GorevYeri);
    }

    private void ConfigureDosya(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Dosya>();
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_DOSYALAR").HasColumnName("ID");
        entity.Property(e => e.SessionId).HasMaxLength(100).IsUnicode(false).HasColumnName("SESSION_ID");
        entity.Property(e => e.DuyuruId).HasColumnName("DUYURU_ID");
        entity.Property(e => e.DosyaAdi).HasMaxLength(255).IsRequired().IsUnicode(false).HasColumnName("DOSYA_ADI");
        entity.Property(e => e.DosyaYolu).HasMaxLength(500).IsRequired().IsUnicode(false).HasColumnName("DOSYA_YOLU");
        entity.Property(e => e.DosyaTipi).HasMaxLength(100).IsRequired().IsUnicode(false).HasColumnName("DOSYA_TIPI");
        entity.Property(e => e.DosyaKategorisi).HasMaxLength(50).HasDefaultValue("ATTACHMENT").IsUnicode(false).HasColumnName("DOSYA_KATEGORISI");
        entity.Property(e => e.DosyaBoyutu).HasColumnName("DOSYA_BOYUTU");
        entity.Property(e => e.DosyaHash).HasMaxLength(64).IsUnicode(false).HasColumnName("DOSYA_HASH");
        entity.Property(e => e.Aciklama).HasMaxLength(500).IsUnicode(false).HasColumnName("ACIKLAMA");
        entity.Property(e => e.YukleyenKullaniciId).HasColumnName("YUKLEYEN_KULLANICI_ID");
        entity.Property(e => e.Aktif)
            .HasMaxLength(1)
            .HasDefaultValue("Y")
            .IsUnicode(false)
            .IsFixedLength()
            .HasColumnName("AKTIF");
        entity.Property(e => e.YuklemeTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("YUKLEME_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");

        // Foreign Keys
        entity.HasOne(e => e.Duyuru)
            .WithMany(d => d.EkDosyalar)
            .HasForeignKey(e => e.DuyuruId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(e => e.YukleyenKullanici)
            .WithMany()
            .HasForeignKey(e => e.YukleyenKullaniciId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        entity.HasIndex(e => e.DuyuruId);
        entity.HasIndex(e => e.YukleyenKullaniciId);
        entity.HasIndex(e => e.DosyaKategorisi);
        entity.HasIndex(e => e.Aktif);
    }

    private void ConfigureLogLogin(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LogLogin>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_LOG_LOGIN").HasColumnName("ID");
        entity.Property(e => e.KullaniciId).HasColumnName("KULLANICI_ID");
        entity.Property(e => e.KullaniciAdi).HasMaxLength(100).IsUnicode(false).HasColumnName("KULLANICI_ADI");
        entity.Property(e => e.Email).HasMaxLength(200).IsUnicode(false).HasColumnName("EMAIL");
        entity.Property(e => e.IpAdres).HasMaxLength(45).IsUnicode(false).HasColumnName("IP_ADRES");
        entity.Property(e => e.UserAgent).HasMaxLength(500).IsUnicode(false).HasColumnName("USER_AGENT");
        entity.Property(e => e.GirisTuru).HasMaxLength(20).IsRequired().IsUnicode(false).HasColumnName("GIRIS_TURU");
        entity.Property(e => e.Basarili).HasMaxLength(1).IsRequired().IsUnicode(false).HasDefaultValue("Y").HasColumnName("BASARILI");
        entity.Property(e => e.HataMesaji).HasMaxLength(500).IsUnicode(false).HasColumnName("HATA_MESAJI");
        entity.Property(e => e.GirisTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("GIRIS_TARIHI");

        entity.HasOne(e => e.Kullanici)
            .WithMany(k => k.LoginLoglari)
            .HasForeignKey(e => e.KullaniciId)
            .OnDelete(DeleteBehavior.SetNull);
        //     .IsRequired(false);
    }

    private void ConfigureLogSistem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LogSistem>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_LOG_SISTEM").HasColumnName("ID");
        entity.Property(e => e.KullaniciId).HasColumnName("KULLANICI_ID");
        entity.Property(e => e.LogSeviye).HasMaxLength(10).IsRequired().IsUnicode(false).HasColumnName("LOG_SEVIYE");
        entity.Property(e => e.Kategori).HasMaxLength(50).IsRequired().IsUnicode(false).HasColumnName("KATEGORI");
        entity.Property(e => e.Islem).HasMaxLength(100).IsRequired().IsUnicode(false).HasColumnName("ISLEM");
        entity.Property(e => e.Detay).IsUnicode(true).HasColumnName("DETAY"); // CLOB
        entity.Property(e => e.IpAdres).HasMaxLength(45).IsUnicode(false).HasColumnName("IP_ADRES");
        entity.Property(e => e.UserAgent).HasMaxLength(500).IsUnicode(false).HasColumnName("USER_AGENT");
        entity.Property(e => e.LogTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("LOG_TARIHI");

        entity.HasOne(e => e.Kullanici)
            .WithMany(k => k.SistemLoglari)
            .HasForeignKey(e => e.KullaniciId);
    }

    private void ConfigureEpostaDuyuruGonderimLog(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EpostaDuyuruGonderimLog>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_DUYURU_GONDERIM_LOG").HasColumnName("ID");
        entity.Property(e => e.DuyuruId).IsRequired().HasColumnName("DUYURU_ID");
        entity.Property(e => e.AliciEmail).HasMaxLength(254).IsRequired().IsUnicode(false).HasColumnName("ALICI_EMAIL");
        entity.Property(e => e.AliciAdSoyad).HasMaxLength(100).IsUnicode(false).HasColumnName("ALICI_AD_SOYAD");
        entity.Property(e => e.AliciKategorisi).HasMaxLength(10).IsRequired().IsUnicode(false).HasColumnName("ALICI_KATEGORISI");
        entity.Property(e => e.GonderimDurumu).HasMaxLength(20).IsRequired().IsUnicode(false).HasColumnName("GONDERIM_DURUMU");
        entity.Property(e => e.HataMesaji).HasMaxLength(1000).IsUnicode(false).HasColumnName("HATA_MESAJI");
        entity.Property(e => e.GonderimTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("GONDERIM_TARIHI");
        entity.Property(e => e.AliciId).HasColumnName("ALICI_ID");

        entity.HasOne(e => e.Duyuru)
            .WithMany()
            .HasForeignKey(e => e.DuyuruId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Alici)
            .WithMany()
            .HasForeignKey(e => e.AliciId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(e => e.DuyuruId);
        entity.HasIndex(e => e.AliciEmail);
        entity.HasIndex(e => e.GonderimTarihi);
    }

    private void ConfigureEpostaDuyuruZamanlama(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EpostaDuyuruZamanlama>();

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseSequence("SEQ_EPOSTA_DUYURU_ZAMANLAMALAR").HasColumnName("ID");
        entity.Property(e => e.DuyuruId).IsRequired().HasColumnName("DUYURU_ID");
        entity.Property(e => e.ZamanlanmaTarihi).IsRequired().HasColumnName("ZAMANLANAN_TARIH");
        entity.Property(e => e.Durum).HasMaxLength(50).IsRequired().HasDefaultValue("BEKLEMEDE").IsUnicode(false).HasColumnName("DURUM");
        entity.Property(e => e.GonderimTarihi).HasColumnName("GONDERIM_TARIHI");
        entity.Property(e => e.HangfireJobId).HasMaxLength(200).IsUnicode(false).HasColumnName("HANGFIRE_JOB_ID");
        entity.Property(e => e.HataMesaji).HasMaxLength(1000).IsUnicode(false).HasColumnName("HATA_MESAJI");
        entity.Property(e => e.AliciSayisi).HasDefaultValue(0).HasColumnName("ALICI_SAYISI");
        entity.Property(e => e.OlusturanKullaniciId).IsRequired().HasColumnName("OLUSTURAN_KULLANICI_ID");
        entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("SYSDATE").HasColumnName("OLUSTURMA_TARIHI");
        entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");
        entity.Property(e => e.IptalNotu).HasMaxLength(500).IsUnicode(false).HasColumnName("IPTAL_NOTU");

        // Foreign Keys
        entity.HasOne(e => e.Duyuru)
            .WithMany()
            .HasForeignKey(e => e.DuyuruId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.OlusturanKullanici)
            .WithMany()
            .HasForeignKey(e => e.OlusturanKullaniciId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        entity.HasIndex(e => e.DuyuruId);
        entity.HasIndex(e => e.Durum);
        entity.HasIndex(e => e.ZamanlanmaTarihi);
        entity.HasIndex(e => e.HangfireJobId);
    }

    private void ConfigureDuyuruListeView(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DuyuruListeView>(entity =>
        {
            // Oracle view column mapping (UPPERCASE -> PascalCase)
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Konu).HasColumnName("KONU");
            entity.Property(e => e.Durum).HasColumnName("DURUM");
            entity.Property(e => e.OlusturanKullaniciId).HasColumnName("OLUSTURAN_KULLANICI_ID");
            entity.Property(e => e.OlusturanAdSoyad).HasColumnName("OLUSTURAN_AD_SOYAD");
            entity.Property(e => e.OnaylayanKullaniciId).HasColumnName("ONAYLAYAN_KULLANICI_ID");
            entity.Property(e => e.OnaylayanAdSoyad).HasColumnName("ONAYLAYAN_AD_SOYAD");
            entity.Property(e => e.OnayTarihi).HasColumnName("ONAY_TARIHI");
            entity.Property(e => e.OnayNotu).HasColumnName("ONAY_NOTU");
            entity.Property(e => e.IlkOnaylayanKullaniciId).HasColumnName("ILK_ONAYLAYAN_KULLANICI_ID");
            entity.Property(e => e.IlkOnaylayanAdSoyad).HasColumnName("ILK_ONAYLAYAN_AD_SOYAD");
            entity.Property(e => e.SonOnaylayanKullaniciId).HasColumnName("SON_ONAYLAYAN_KULLANICI_ID");
            entity.Property(e => e.SonOnaylayanAdSoyad).HasColumnName("SON_ONAYLAYAN_AD_SOYAD");
            entity.Property(e => e.RedNedeni).HasColumnName("RED_NEDENI");
            entity.Property(e => e.RedTarihi).HasColumnName("RED_TARIHI");
            entity.Property(e => e.ToplamAliciSayisi).HasColumnName("TOPLAM_ALICI_SAYISI");
            entity.Property(e => e.BasariliGonderimSayisi).HasColumnName("BASARILI_GONDERIM_SAYISI");
            entity.Property(e => e.BasarisizGonderimSayisi).HasColumnName("BASARISIZ_GONDERIM_SAYISI");
            entity.Property(e => e.OlusturmaTarihi).HasColumnName("OLUSTURMA_TARIHI");
            entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEME_TARIHI");
            entity.Property(e => e.GercekGonderimTarihi).HasColumnName("GERCEK_GONDERIM_TARIHI");
            entity.Property(e => e.BasariYuzdesi).HasColumnName("BASARI_YUZDESI").HasColumnType("DECIMAL(5,2)");
            entity.Property(e => e.ToplamDosyaSayisi).HasColumnName("TOPLAM_DOSYA_SAYISI");
            entity.Property(e => e.ZamanlanmisMi).HasColumnName("ZAMANLANMIS_MI");
            entity.Property(e => e.ZamanlamaSayisi).HasColumnName("ZAMANLAMA_SAYISI");
        });
    }
}