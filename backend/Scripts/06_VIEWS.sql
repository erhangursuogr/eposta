-- ==================================================
-- DEÜ DUYURU YÖNETİM SİSTEMİ - ORACLE DATABASE
-- 04_VIEWS.sql - Sistem View'leri
-- ==================================================
-- Dashboard ve liste view'leri
-- ==================================================

PROMPT ========================================
PROMPT DEÜ Duyuru Sistemi - System Views
PROMPT ========================================

-- =============================================
-- 1. DUYURULAR LISTE VIEW
-- =============================================
PROMPT 1. V_DUYURULAR_LISTE view oluşturuluyor...

CREATE OR REPLACE VIEW V_DUYURULAR_LISTE AS
SELECT
    -- Temel bilgiler
    d.ID,
    d.KONU,
    d.ACIKLAMA,
    d.ICERIK_TIPI,
    d.DURUM,
    d.DUYURU_KATEGORISI,
    d.GONDERICI_KATEGORI,

    -- Oluşturan kullanıcı
    d.OLUSTURAN_KULLANICI_ID,
    ko.AD_SOYAD AS OLUSTURAN_AD_SOYAD,

    -- İlk onaylayan (Koordinatör)
    d.ILK_ONAYLAYAN_KULLANICI_ID,
    kilk.AD_SOYAD AS ILK_ONAYLAYAN_AD_SOYAD,

    -- Son onaylayan (Manager)
    d.SON_ONAYLAYAN_KULLANICI_ID,
    kson.AD_SOYAD AS SON_ONAYLAYAN_AD_SOYAD,

    -- Backward compatibility aliases (API değişmesin)
    d.SON_ONAYLAYAN_KULLANICI_ID AS ONAYLAYAN_KULLANICI_ID,
    kson.AD_SOYAD AS ONAYLAYAN_AD_SOYAD,

    -- Kontrolör bilgileri (EPOSTA_DUYURU_HAREKETLERI tablosundan - optimize edilmiş)
    hareketler.KOORDINATOR_KULLANICI_ID,
    hareketler.KOORDINATOR_AD_SOYAD,
    hareketler.KOORDINATOR_ONAY_TARIHI,
    hareketler.KOORDINATOR_ONAY_NOTU,

    -- Kontrolör red bilgileri
    hareketler.KOORDINATOR_RED_TARIHI,
    hareketler.KOORDINATOR_RED_NEDENI,

    -- Manager onay bilgileri
    hareketler.ONAY_TARIHI,
    hareketler.ONAY_NOTU,

    -- Manager red bilgileri
    hareketler.RED_TARIHI,
    hareketler.RED_NEDENI,

    -- Alıcı istatistikleri (sadece EMAIL için)
    d.TOPLAM_ALICI_SAYISI,
    d.BASARILI_GONDERIM_SAYISI,
    d.BASARISIZ_GONDERIM_SAYISI,

    -- Tarih bilgileri
    d.OLUSTURMA_TARIHI,
    d.GUNCELLEME_TARIHI,
    d.GERCEK_GONDERIM_TARIHI,

    -- Hesaplanan alanlar
    CASE
        WHEN d.TOPLAM_ALICI_SAYISI > 0 THEN
            ROUND((d.BASARILI_GONDERIM_SAYISI * 100.0) / d.TOPLAM_ALICI_SAYISI, 2)
        ELSE 0
    END AS BASARI_YUZDESI,

    NVL(dosya_counts.DOSYA_SAYISI, 0) AS TOPLAM_DOSYA_SAYISI,
    CASE WHEN NVL(zamanlama_counts.ZAMANLAMA_SAYISI, 0) > 0 THEN 1 ELSE 0 END AS ZAMANLANMIS_MI,
    NVL(zamanlama_counts.ZAMANLAMA_SAYISI, 0) AS ZAMANLAMA_SAYISI

FROM EPOSTA_DUYURULARI d
    LEFT JOIN KULLANICILAR ko ON d.OLUSTURAN_KULLANICI_ID = ko.ID
    LEFT JOIN KULLANICILAR kilk ON d.ILK_ONAYLAYAN_KULLANICI_ID = kilk.ID
    LEFT JOIN KULLANICILAR kson ON d.SON_ONAYLAYAN_KULLANICI_ID = kson.ID

    -- PERFORMANS OPTİMİZASYON: 4 ayrı subquery yerine TEK subquery ile tüm onay/red bilgileri
    -- ROW_NUMBER() sadece 1 kez çalışıyor (4 kez değil)
    LEFT JOIN (
        SELECT
            DUYURU_ID,
            -- Koordinatör onay bilgileri
            MAX(CASE WHEN ISLEM_TIPI = 'ONAYLAMA' AND YENI_DURUM = 'SON_ONAY_BEKLIYOR' THEN KULLANICI_ID END) AS KOORDINATOR_KULLANICI_ID,
            MAX(CASE WHEN ISLEM_TIPI = 'ONAYLAMA' AND YENI_DURUM = 'SON_ONAY_BEKLIYOR' THEN KULLANICI_AD_SOYAD END) AS KOORDINATOR_AD_SOYAD,
            MAX(CASE WHEN ISLEM_TIPI = 'ONAYLAMA' AND YENI_DURUM = 'SON_ONAY_BEKLIYOR' THEN ISLEM_TARIHI END) AS KOORDINATOR_ONAY_TARIHI,
            MAX(CASE WHEN ISLEM_TIPI = 'ONAYLAMA' AND YENI_DURUM = 'SON_ONAY_BEKLIYOR' THEN ACIKLAMA END) AS KOORDINATOR_ONAY_NOTU,
            -- Koordinatör red bilgileri
            MAX(CASE WHEN ISLEM_TIPI = 'REDDETME' AND ONCEKI_DURUM = 'ILK_ONAY_BEKLIYOR' THEN ISLEM_TARIHI END) AS KOORDINATOR_RED_TARIHI,
            MAX(CASE WHEN ISLEM_TIPI = 'REDDETME' AND ONCEKI_DURUM = 'ILK_ONAY_BEKLIYOR' THEN ACIKLAMA END) AS KOORDINATOR_RED_NEDENI,
            -- Manager onay bilgileri
            MAX(CASE WHEN ISLEM_TIPI = 'ONAYLAMA' AND YENI_DURUM = 'ONAYLANDI' THEN ISLEM_TARIHI END) AS ONAY_TARIHI,
            MAX(CASE WHEN ISLEM_TIPI = 'ONAYLAMA' AND YENI_DURUM = 'ONAYLANDI' THEN ACIKLAMA END) AS ONAY_NOTU,
            -- Manager red bilgileri
            MAX(CASE WHEN ISLEM_TIPI = 'REDDETME' AND ONCEKI_DURUM = 'SON_ONAY_BEKLIYOR' THEN ISLEM_TARIHI END) AS RED_TARIHI,
            MAX(CASE WHEN ISLEM_TIPI = 'REDDETME' AND ONCEKI_DURUM = 'SON_ONAY_BEKLIYOR' THEN ACIKLAMA END) AS RED_NEDENI
        FROM (
            SELECT
                h.DUYURU_ID,
                h.ISLEM_TIPI,
                h.YENI_DURUM,
                h.ONCEKI_DURUM,
                h.KULLANICI_ID,
                k.AD_SOYAD AS KULLANICI_AD_SOYAD,
                h.ISLEM_TARIHI,
                h.ACIKLAMA,
                ROW_NUMBER() OVER (
                    PARTITION BY h.DUYURU_ID, h.ISLEM_TIPI,
                        CASE
                            WHEN h.ISLEM_TIPI = 'ONAYLAMA' THEN h.YENI_DURUM
                            WHEN h.ISLEM_TIPI = 'REDDETME' THEN h.ONCEKI_DURUM
                        END
                    ORDER BY h.ISLEM_TARIHI DESC
                ) AS rn
            FROM EPOSTA_DUYURU_HAREKETLERI h
            LEFT JOIN KULLANICILAR k ON h.KULLANICI_ID = k.ID
            WHERE (h.ISLEM_TIPI = 'ONAYLAMA' AND h.YENI_DURUM IN ('SON_ONAY_BEKLIYOR', 'ONAYLANDI'))
               OR (h.ISLEM_TIPI = 'REDDETME' AND h.ONCEKI_DURUM IN ('ILK_ONAY_BEKLIYOR', 'SON_ONAY_BEKLIYOR'))
        ) subq
        WHERE rn = 1
        GROUP BY DUYURU_ID
    ) hareketler ON d.ID = hareketler.DUYURU_ID

    -- Dosya sayısı
    LEFT JOIN (
        SELECT DUYURU_ID, COUNT(*) AS DOSYA_SAYISI
        FROM DOSYALAR
        WHERE AKTIF = 'Y'
        GROUP BY DUYURU_ID
    ) dosya_counts ON d.ID = dosya_counts.DUYURU_ID

    -- Zamanlama sayısı
    LEFT JOIN (
        SELECT DUYURU_ID, COUNT(*) AS ZAMANLAMA_SAYISI
        FROM EPOSTA_DUYURU_ZAMANLAMALAR
        WHERE DURUM = 'BEKLEMEDE'
        GROUP BY DUYURU_ID
    ) zamanlama_counts ON d.ID = zamanlama_counts.DUYURU_ID;

-- =============================================
-- 2. DASHBOARD ÖZET VIEW
-- =============================================
PROMPT 2. V_DASHBOARD_OZET view oluşturuluyor...

CREATE OR REPLACE VIEW V_DASHBOARD_OZET AS
WITH
    -- PERFORMANS: Duyuru durumlarını tek sorguda hesapla
    duyuru_stats AS (
        SELECT
            COUNT(*) AS TOPLAM_DUYURU,
            SUM(CASE WHEN DURUM = 'TASLAK' THEN 1 ELSE 0 END) AS TASLAK_SAYISI,
            SUM(CASE WHEN DURUM = 'ILK_ONAY_BEKLIYOR' THEN 1 ELSE 0 END) AS ILK_ONAY_BEKLIYOR_SAYISI,
            SUM(CASE WHEN DURUM = 'SON_ONAY_BEKLIYOR' THEN 1 ELSE 0 END) AS SON_ONAY_BEKLIYOR_SAYISI,
            SUM(CASE WHEN DURUM = 'ONAYLANDI' THEN 1 ELSE 0 END) AS ONAYLANDI_SAYISI,
            SUM(CASE WHEN DURUM = 'GONDERILDI' THEN 1 ELSE 0 END) AS GONDERILDI_SAYISI,
            SUM(CASE WHEN DURUM = 'REDDEDILDI' THEN 1 ELSE 0 END) AS REDDEDILDI_SAYISI,
            -- İçerik tipi bazlı istatistikler
            SUM(CASE WHEN ICERIK_TIPI = 'EMAIL' THEN 1 ELSE 0 END) AS EMAIL_SAYISI,
            SUM(CASE WHEN ICERIK_TIPI LIKE 'SOSYAL_MEDYA%' THEN 1 ELSE 0 END) AS SOSYAL_MEDYA_SAYISI
        FROM EPOSTA_DUYURULARI
    ),
    -- PERFORMANS: Kullanıcı istatistiklerini tek sorguda hesapla
    kullanici_stats AS (
        SELECT
            COUNT(*) AS AKTIF_KULLANICI_SAYISI,
            SUM(CASE WHEN r.ROL_KODU = 'ADMIN' THEN 1 ELSE 0 END) AS ADMIN_SAYISI
        FROM KULLANICILAR k
        LEFT JOIN ROLLER r ON k.ROL_ID = r.ID
        WHERE k.AKTIF = 'Y'
    ),
    -- PERFORMANS: Grup istatistiklerini tek sorguda hesapla
    grup_stats AS (
        SELECT
            COUNT(*) AS AKTIF_GRUP_SAYISI,
            SUM(CASE WHEN GRUP_TIPI = 'DINAMIK' THEN 1 ELSE 0 END) AS DINAMIK_GRUP_SAYISI
        FROM EPOSTA_GRUPLARI
        WHERE AKTIF = 'Y'
    ),
    -- PERFORMANS: Son 30 gün alıcı istatistiklerini tek sorguda hesapla (2 subquery → 1 query)
    alici_30gun_stats AS (
        SELECT
            COUNT(*) AS SON_30GUN_ALICI,
            SUM(CASE WHEN a.GONDERIM_DURUMU = 'GONDERILDI' THEN 1 ELSE 0 END) AS SON_30GUN_BASARILI
        FROM EPOSTA_DUYURU_ALICILARI a
        JOIN EPOSTA_DUYURULARI d ON a.DUYURU_ID = d.ID
        WHERE d.OLUSTURMA_TARIHI > SYSDATE - 30
    )
SELECT
    -- Duyuru istatistikleri (çift onay sistemi)
    ds.TASLAK_SAYISI,
    ds.ILK_ONAY_BEKLIYOR_SAYISI,
    ds.SON_ONAY_BEKLIYOR_SAYISI,
    ds.ONAYLANDI_SAYISI,
    ds.GONDERILDI_SAYISI,
    ds.REDDEDILDI_SAYISI,
    ds.TOPLAM_DUYURU,

    -- İçerik tipi istatistikleri
    ds.EMAIL_SAYISI,
    ds.SOSYAL_MEDYA_SAYISI,

    -- Kullanıcı istatistikleri
    ks.AKTIF_KULLANICI_SAYISI,
    ks.ADMIN_SAYISI,

    -- Grup istatistikleri
    gs.AKTIF_GRUP_SAYISI,
    gs.DINAMIK_GRUP_SAYISI,

    -- Alıcı istatistikleri (son 30 gün)
    a30.SON_30GUN_ALICI,
    a30.SON_30GUN_BASARILI,

    SYSDATE AS RAPOR_TARIHI
FROM duyuru_stats ds, kullanici_stats ks, grup_stats gs, alici_30gun_stats a30;

-- =============================================
-- 3. DUYURU HAREKET GEÇMİŞİ VIEW (Timeline)
-- =============================================
PROMPT 3. V_DUYURU_HAREKET_GECMISI view oluşturuluyor...

CREATE OR REPLACE VIEW V_DUYURU_HAREKET_GECMISI AS
SELECT
    h.ID,
    h.DUYURU_ID,
    d.KONU,
    d.ICERIK_TIPI,

    -- Hareket bilgileri
    h.ONCEKI_DURUM,
    h.YENI_DURUM,
    h.ISLEM_TIPI,

    -- Kullanıcı bilgileri (İşlemi yapan)
    h.KULLANICI_ID,
    k.AD_SOYAD AS KULLANICI_AD_SOYAD,
    k.EMAIL AS KULLANICI_EMAIL,
    r.ROL_ADI AS KULLANICI_ROL,
    r.ROL_KODU AS KULLANICI_ROL_KODU,
    h.ACIKLAMA,

    -- Seçilen onaylayıcı (Kontrolörün seçtiği manager)
    h.SECILEN_ONAYLAYICI_ID,
    ko.AD_SOYAD AS SECILEN_ONAYLAYICI_AD_SOYAD,
    ko.EMAIL AS SECILEN_ONAYLAYICI_EMAIL,
    h.ISLEM_TARIHI,

    -- Timeline için sıra numarası (duyuru bazında)
    ROW_NUMBER() OVER (PARTITION BY h.DUYURU_ID ORDER BY h.ISLEM_TARIHI) AS SIRA_NO,

    -- Geçen süre (bir sonraki işleme kadar) - GÜN cinsinden
    ROUND(
        LEAD(h.ISLEM_TARIHI) OVER (PARTITION BY h.DUYURU_ID ORDER BY h.ISLEM_TARIHI) - h.ISLEM_TARIHI,
        2
    ) AS SURE_GUN,

    -- Durum açıklamaları (frontend için)
    CASE h.ISLEM_TIPI
        WHEN 'OLUSTURMA' THEN 'Duyuru oluşturuldu'
        WHEN 'ONAYA_GONDERME' THEN 'Onaya gönderildi'
        WHEN 'ONAYLAMA' THEN
            CASE
                WHEN h.YENI_DURUM = 'SON_ONAY_BEKLIYOR' THEN 'Kontrolör onayladı'
                WHEN h.YENI_DURUM = 'ONAYLANDI' THEN 'Manager onayladı'
                ELSE 'Onaylandı'
            END
        WHEN 'REDDETME' THEN
            CASE
                WHEN h.ONCEKI_DURUM = 'ILK_ONAY_BEKLIYOR' THEN 'Kontrolör reddetti'
                WHEN h.ONCEKI_DURUM = 'SON_ONAY_BEKLIYOR' THEN 'Manager reddetti'
                ELSE 'Reddedildi'
            END
        WHEN 'IPTAL' THEN 'İptal edildi'
        WHEN 'GONDERIM' THEN 'Gönderildi'
        WHEN 'DUZENLEME' THEN 'Düzenlendi'
        ELSE h.ISLEM_TIPI
    END AS ISLEM_ACIKLAMA

FROM EPOSTA_DUYURU_HAREKETLERI h
    LEFT JOIN EPOSTA_DUYURULARI d ON h.DUYURU_ID = d.ID
    LEFT JOIN KULLANICILAR k ON h.KULLANICI_ID = k.ID
    LEFT JOIN ROLLER r ON k.ROL_ID = r.ID
    LEFT JOIN KULLANICILAR ko ON h.SECILEN_ONAYLAYICI_ID = ko.ID;

-- =============================================
-- 4. MATERIALIZED VIEW - DUYURULAR LISTE (PERFORMANS)
-- =============================================
-- PROMPT 4. Materialized view oluşturuluyor (V_DUYURULAR_LISTE için performans)...

-- NOT: Materialized View kullanmak için CREATE MATERIALIZED VIEW ve QUERY REWRITE
-- yetkilerine ihtiyaç var. Yetki alındığında yorum satırlarını kaldırın.

/*
-- Materialized View: V_DUYURULAR_LISTE'nin pre-computed versiyonu
CREATE MATERIALIZED VIEW MV_DUYURULAR_LISTE
BUILD IMMEDIATE           -- Hemen oluştur
REFRESH FAST ON COMMIT    -- Her COMMIT'te değişen satırları güncelle
ENABLE QUERY REWRITE      -- Oracle otomatik olarak V_DUYURULAR_LISTE yerine MV kullanır
AS
SELECT
    -- Temel bilgiler
    d.ID,
    d.KONU,
    d.ACIKLAMA,
    d.ICERIK_TIPI,
    d.DURUM,
    d.DUYURU_KATEGORISI,
    d.GONDERICI_KATEGORI,

    -- Oluşturan kullanıcı
    d.OLUSTURAN_KULLANICI_ID,
    ko.AD_SOYAD AS OLUSTURAN_AD_SOYAD,

    -- İlk onaylayan (Koordinatör)
    d.ILK_ONAYLAYAN_KULLANICI_ID,
    kilk.AD_SOYAD AS ILK_ONAYLAYAN_AD_SOYAD,

    -- Son onaylayan (Manager)
    d.SON_ONAYLAYAN_KULLANICI_ID,
    kson.AD_SOYAD AS SON_ONAYLAYAN_AD_SOYAD,

    -- Backward compatibility aliases
    d.SON_ONAYLAYAN_KULLANICI_ID AS ONAYLAYAN_KULLANICI_ID,
    kson.AD_SOYAD AS ONAYLAYAN_AD_SOYAD,

    -- Koordinatör bilgileri
    koor_onay.KULLANICI_ID AS KOORDINATOR_KULLANICI_ID,
    koor_onay.KULLANICI_AD_SOYAD AS KOORDINATOR_AD_SOYAD,
    koor_onay.ISLEM_TARIHI AS KOORDINATOR_ONAY_TARIHI,
    koor_onay.ACIKLAMA AS KOORDINATOR_ONAY_NOTU,

    -- Koordinatör red bilgileri
    koor_red.ISLEM_TARIHI AS KOORDINATOR_RED_TARIHI,
    koor_red.ACIKLAMA AS KOORDINATOR_RED_NEDENI,

    -- Manager onay bilgileri
    man_onay.ISLEM_TARIHI AS ONAY_TARIHI,
    man_onay.ACIKLAMA AS ONAY_NOTU,

    -- Manager red bilgileri
    man_red.ISLEM_TARIHI AS RED_TARIHI,
    man_red.ACIKLAMA AS RED_NEDENI,

    -- Alıcı istatistikleri
    d.TOPLAM_ALICI_SAYISI,
    d.BASARILI_GONDERIM_SAYISI,
    d.BASARISIZ_GONDERIM_SAYISI,

    -- Tarih bilgileri
    d.OLUSTURMA_TARIHI,
    d.GUNCELLEME_TARIHI,
    d.GERCEK_GONDERIM_TARIHI,

    -- Hesaplanan alanlar
    CASE
        WHEN d.TOPLAM_ALICI_SAYISI > 0 THEN
            ROUND((d.BASARILI_GONDERIM_SAYISI * 100.0) / d.TOPLAM_ALICI_SAYISI, 2)
        ELSE 0
    END AS BASARI_YUZDESI,

    NVL(dosya_counts.DOSYA_SAYISI, 0) AS TOPLAM_DOSYA_SAYISI,
    CASE WHEN NVL(zamanlama_counts.ZAMANLAMA_SAYISI, 0) > 0 THEN 1 ELSE 0 END AS ZAMANLANMIS_MI,
    NVL(zamanlama_counts.ZAMANLAMA_SAYISI, 0) AS ZAMANLAMA_SAYISI

FROM EPOSTA_DUYURULARI d
    LEFT JOIN KULLANICILAR ko ON d.OLUSTURAN_KULLANICI_ID = ko.ID
    LEFT JOIN KULLANICILAR kilk ON d.ILK_ONAYLAYAN_KULLANICI_ID = kilk.ID
    LEFT JOIN KULLANICILAR kson ON d.SON_ONAYLAYAN_KULLANICI_ID = kson.ID

    -- Koordinatör onay bilgileri
    LEFT JOIN (
        SELECT h.DUYURU_ID, h.KULLANICI_ID, k.AD_SOYAD AS KULLANICI_AD_SOYAD, h.ISLEM_TARIHI, h.ACIKLAMA,
               ROW_NUMBER() OVER (PARTITION BY h.DUYURU_ID ORDER BY h.ISLEM_TARIHI DESC) AS rn
        FROM EPOSTA_DUYURU_HAREKETLERI h
        LEFT JOIN KULLANICILAR k ON h.KULLANICI_ID = k.ID
        WHERE h.ISLEM_TIPI = 'ONAYLAMA' AND h.YENI_DURUM = 'SON_ONAY_BEKLIYOR'
    ) koor_onay ON d.ID = koor_onay.DUYURU_ID AND koor_onay.rn = 1

    -- Koordinatör red bilgileri
    LEFT JOIN (
        SELECT h.DUYURU_ID, h.ISLEM_TARIHI, h.ACIKLAMA,
               ROW_NUMBER() OVER (PARTITION BY h.DUYURU_ID ORDER BY h.ISLEM_TARIHI DESC) AS rn
        FROM EPOSTA_DUYURU_HAREKETLERI h
        WHERE h.ISLEM_TIPI = 'REDDETME' AND h.ONCEKI_DURUM = 'ILK_ONAY_BEKLIYOR'
    ) koor_red ON d.ID = koor_red.DUYURU_ID AND koor_red.rn = 1

    -- Manager onay bilgileri
    LEFT JOIN (
        SELECT h.DUYURU_ID, h.ISLEM_TARIHI, h.ACIKLAMA,
               ROW_NUMBER() OVER (PARTITION BY h.DUYURU_ID ORDER BY h.ISLEM_TARIHI DESC) AS rn
        FROM EPOSTA_DUYURU_HAREKETLERI h
        WHERE h.ISLEM_TIPI = 'ONAYLAMA' AND h.YENI_DURUM = 'ONAYLANDI'
    ) man_onay ON d.ID = man_onay.DUYURU_ID AND man_onay.rn = 1

    -- Manager red bilgileri
    LEFT JOIN (
        SELECT h.DUYURU_ID, h.ISLEM_TARIHI, h.ACIKLAMA,
               ROW_NUMBER() OVER (PARTITION BY h.DUYURU_ID ORDER BY h.ISLEM_TARIHI DESC) AS rn
        FROM EPOSTA_DUYURU_HAREKETLERI h
        WHERE h.ISLEM_TIPI = 'REDDETME' AND h.ONCEKI_DURUM = 'SON_ONAY_BEKLIYOR'
    ) man_red ON d.ID = man_red.DUYURU_ID AND man_red.rn = 1

    -- Dosya sayısı
    LEFT JOIN (
        SELECT DUYURU_ID, COUNT(*) AS DOSYA_SAYISI
        FROM DOSYALAR
        WHERE AKTIF = 'Y'
        GROUP BY DUYURU_ID
    ) dosya_counts ON d.ID = dosya_counts.DUYURU_ID

    -- Zamanlama sayısı
    LEFT JOIN (
        SELECT DUYURU_ID, COUNT(*) AS ZAMANLAMA_SAYISI
        FROM EPOSTA_DUYURU_ZAMANLAMALAR
        WHERE DURUM = 'BEKLEMEDE'
        GROUP BY DUYURU_ID
    ) zamanlama_counts ON d.ID = zamanlama_counts.DUYURU_ID;

-- Materialized View Log oluştur (FAST REFRESH için gerekli)
CREATE MATERIALIZED VIEW LOG ON EPOSTA_DUYURULARI
WITH ROWID, SEQUENCE (
    ID, KONU, ACIKLAMA, ICERIK_TIPI, DURUM,
    OLUSTURAN_KULLANICI_ID, ILK_ONAYLAYAN_KULLANICI_ID, SON_ONAYLAYAN_KULLANICI_ID,
    TOPLAM_ALICI_SAYISI, BASARILI_GONDERIM_SAYISI, BASARISIZ_GONDERIM_SAYISI,
    OLUSTURMA_TARIHI, GUNCELLEME_TARIHI, GERCEK_GONDERIM_TARIHI
)
INCLUDING NEW VALUES;

CREATE MATERIALIZED VIEW LOG ON KULLANICILAR
WITH ROWID, SEQUENCE (ID, AD_SOYAD)
INCLUDING NEW VALUES;

CREATE MATERIALIZED VIEW LOG ON EPOSTA_DUYURU_HAREKETLERI
WITH ROWID, SEQUENCE (
    DUYURU_ID, KULLANICI_ID, ISLEM_TIPI, YENI_DURUM, ONCEKI_DURUM,
    ISLEM_TARIHI, ACIKLAMA
)
INCLUDING NEW VALUES;

CREATE MATERIALIZED VIEW LOG ON DOSYALAR
WITH ROWID, SEQUENCE (DUYURU_ID, AKTIF)
INCLUDING NEW VALUES;

CREATE MATERIALIZED VIEW LOG ON EPOSTA_DUYURU_ZAMANLAMALAR
WITH ROWID, SEQUENCE (DUYURU_ID, DURUM)
INCLUDING NEW VALUES;

-- Materialized View için index oluştur
CREATE INDEX IDX_MV_DUYURULAR_ID ON MV_DUYURULAR_LISTE(ID);
CREATE INDEX IDX_MV_DUYURULAR_DURUM ON MV_DUYURULAR_LISTE(DURUM);
CREATE INDEX IDX_MV_DUYURULAR_TARIH ON MV_DUYURULAR_LISTE(OLUSTURMA_TARIHI);
*/

COMMIT;

PROMPT ========================================
PROMPT Sistem view'leri başarıyla oluşturuldu!
PROMPT - V_DUYURULAR_LISTE (base view)
PROMPT - V_DASHBOARD_OZET
PROMPT - V_DUYURU_HAREKET_GECMISI
PROMPT NOT: MV_DUYURULAR_LISTE için yorum satırları kaldırılabilir
PROMPT ========================================

-- Kontrol
PROMPT Oluşturulan view'ler:
SELECT view_name
FROM user_views
WHERE view_name IN ('V_DUYURULAR_LISTE', 'V_DASHBOARD_OZET', 'V_DUYURU_HAREKET_GECMISI')
ORDER BY view_name;

SELECT 'VIEWS completed successfully' AS STATUS FROM DUAL;
