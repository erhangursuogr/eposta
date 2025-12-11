-- ==================================================
-- DEÜ DUYURU YÖNETİM SİSTEMİ - ORACLE DATABASE
-- 01_DROP_ALL_CLEAN.sql - Temizlenmiş Veritabanı Temizleme
-- ==================================================
-- Yeniden kurulum için tüm veritabanı nesnelerini siler
-- ONAY_KURALLARI tablosu artık yok - kaldırıldı
-- UYARI: Bu script tüm verileri kalıcı olarak siler!
-- ==================================================

PROMPT ========================================
PROMPT DEÜ Duyuru Sistemi - Veritabanı Temizleme
PROMPT Tüm nesneler silinecek...
PROMPT ========================================

-- =============================================
-- 1. MATERIALIZED VIEWS VE VIEW LOG'LARI SİL (ŞİMDİLİK YORUM)
-- =============================================
-- PROMPT 1. Materialized views ve view log'ları siliniyor...

-- NOT: Materialized View kullanılmıyorsa bu bölüm yorum olarak kalır
/*
-- Materialized View'leri önce sil
DROP MATERIALIZED VIEW MV_DUYURULAR_LISTE;

-- Materialized View Log'ları sil
DROP MATERIALIZED VIEW LOG ON EPOSTA_DUYURU_ZAMANLAMALAR;
DROP MATERIALIZED VIEW LOG ON DOSYALAR;
DROP MATERIALIZED VIEW LOG ON EPOSTA_DUYURU_HAREKETLERI;
DROP MATERIALIZED VIEW LOG ON KULLANICILAR;
DROP MATERIALIZED VIEW LOG ON EPOSTA_DUYURULARI;
*/

-- =============================================
-- 2. NORMAL VIEWS'LARI SİL
-- =============================================
PROMPT 1. Normal views siliniyor...

-- Dashboard views
DROP VIEW V_DUYURU_HAREKET_GECMISI;
DROP VIEW V_DASHBOARD_OZET;
DROP VIEW V_DUYURULAR_LISTE;

-- =============================================
-- 3. TRIGGERS'LARI SİL
-- =============================================
PROMPT 3. Triggers siliniyor...

-- Duyuru audit trigger
DROP TRIGGER TRG_DUYURULARI_AUDIT;

-- Duyuru alıcı sayısı trigger'ları
DROP TRIGGER TRG_ALICI_SAYISI_STMT;
DROP TRIGGER TRG_ALICI_SAYISI_ROW;
DROP PACKAGE PKG_ALICI_SAYISI;

-- Grup üye sayısı trigger'ları
DROP TRIGGER TRG_GRUP_UYE_SAYISI_STMT;
DROP TRIGGER TRG_GRUP_UYE_SAYISI_ROW;
DROP PACKAGE PKG_GRUP_UYE_SAYISI;

-- =============================================
-- 4. TABLOLARI SİL (Foreign Key Sırasına Göre)
-- =============================================
PROMPT 4. Tablolar siliniyor...

-- En üstteki bağımlı tablolar önce
DROP TABLE LOG_SISTEM CASCADE CONSTRAINTS;
DROP TABLE LOG_LOGIN CASCADE CONSTRAINTS;
DROP TABLE EPOSTA_DUYURU_GONDERIM_LOG CASCADE CONSTRAINTS;
DROP TABLE EPOSTA_DUYURU_HAREKETLERI CASCADE CONSTRAINTS;
DROP TABLE EPOSTA_DUYURU_ALICILARI CASCADE CONSTRAINTS;
DROP TABLE EPOSTA_DUYURU_ZAMANLAMALAR CASCADE CONSTRAINTS;

-- Dosya tablosu (circular reference var)
DROP TABLE DOSYALAR CASCADE CONSTRAINTS;

-- Ana duyuru tablosu
DROP TABLE EPOSTA_DUYURULARI CASCADE CONSTRAINTS;

-- Grup yapısı
DROP TABLE EPOSTA_GRUP_UYELERI CASCADE CONSTRAINTS;
DROP TABLE EPOSTA_GRUPLARI CASCADE CONSTRAINTS;

-- Şablonlar ve ayarlar
DROP TABLE EPOSTA_SABLONLARI CASCADE CONSTRAINTS;
DROP TABLE EPOSTA_SABLON_KATEGORILERI CASCADE CONSTRAINTS;
DROP TABLE SISTEM_AYARLARI CASCADE CONSTRAINTS;

-- En alttaki master tablolar
DROP TABLE KULLANICILAR CASCADE CONSTRAINTS;
DROP TABLE ROLLER CASCADE CONSTRAINTS;

-- ONAY_KURALLARI artık yok - kaldırıldı

-- =============================================
-- 5. SEQUENCES'LARI SİL
-- =============================================
PROMPT 5. Sequences siliniyor...

DROP SEQUENCE SEQ_ROLLER;
DROP SEQUENCE SEQ_KULLANICILAR;
DROP SEQUENCE SEQ_SISTEM_AYARLARI;
DROP SEQUENCE SEQ_EPOSTA_GRUPLARI;
DROP SEQUENCE SEQ_EPOSTA_GRUP_UYELERI;
DROP SEQUENCE SEQ_SABLON_KATEGORILERI;
DROP SEQUENCE SEQ_EPOSTA_SABLONLARI;
DROP SEQUENCE SEQ_EPOSTA_DUYURULARI;
DROP SEQUENCE SEQ_EPOSTA_DUYURU_HAREKETLERI;
DROP SEQUENCE SEQ_EPOSTA_DUYURU_ALICILARI;
DROP SEQUENCE SEQ_EPOSTA_DUYURU_ZAMANLAMALAR;
DROP SEQUENCE SEQ_DOSYALAR;
DROP SEQUENCE SEQ_DUYURU_GONDERIM_LOG;
DROP SEQUENCE SEQ_LOG_LOGIN;
DROP SEQUENCE SEQ_LOG_SISTEM;

-- ONAY_KURALLARI sequence'ı artık yok - kaldırıldı

COMMIT;

PROMPT ========================================
PROMPT Veritabanı temizleme tamamlandı!
PROMPT ONAY_KURALLARI tablosu kaldırıldı
PROMPT ========================================

-- Kontrol sorguları
PROMPT Kalan tablolar (boş olmalı):
SELECT table_name FROM user_tables
WHERE table_name IN ('ROLLER', 'KULLANICILAR', 'EPOSTA_GRUPLARI', 'EPOSTA_GRUP_UYELERI',
                     'EPOSTA_SABLONLARI', 'EPOSTA_DUYURULARI', 'EPOSTA_DUYURU_HAREKETLERI',
                     'EPOSTA_DUYURU_ALICILARI', 'DOSYALAR', 'SISTEM_AYARLARI',
                     'LOG_LOGIN', 'LOG_SISTEM');

PROMPT

PROMPT Kalan sequences (boş olmalı):
SELECT sequence_name FROM user_sequences WHERE sequence_name LIKE 'SEQ_%';

PROMPT

PROMPT Kalan views (boş olmalı):  
SELECT view_name FROM user_views WHERE view_name LIKE 'V_%';

PROMPT

PROMPT Kalan triggers (boş olmalı):
SELECT trigger_name FROM user_triggers WHERE trigger_name LIKE 'TRG_%';

PROMPT

PROMPT Kalan indexler:
SELECT COUNT(*) as index_count FROM user_indexes WHERE index_name LIKE 'IDX_%';

SELECT 'CLEAN DROP_ALL completed successfully' AS STATUS FROM DUAL;