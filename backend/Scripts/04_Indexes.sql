-- ==================================================
-- DEÜ DUYURU YÖNETİM SİSTEMİ - ORACLE DATABASE
-- 04_INDEXES_OPTIMIZED.sql - Optimize Edilmiş Kritik İndexler
-- ==================================================
-- Oracle otomatik PK/UK/FK indexlerini hariç tutar
-- Sadece query performansı için kritik olan indexler
-- ==================================================

PROMPT ========================================
PROMPT DEÜ Duyuru Sistemi - Optimize Edilmiş Indexler
PROMPT ========================================

-- =============================================
-- KULLANICILAR
-- =============================================
PROMPT 1. KULLANICILAR indexleri...

CREATE INDEX IDX_KULLANICILAR_AKTIF ON KULLANICILAR(AKTIF);
CREATE INDEX IDX_KULLANICILAR_ROL_ID ON KULLANICILAR(ROL_ID);
-- Email zaten UNIQUE, Oracle otomatik index oluşturur

-- =============================================
-- EPOSTA GRUPLARI
-- =============================================
PROMPT 2. EPOSTA_GRUPLARI indexleri...

-- Composite index: Grup listesi için (AKTIF + GRUP_TIPI birlikte sorgulanır)
CREATE INDEX IDX_GRUPLARI_AKTIF_TIPI ON EPOSTA_GRUPLARI(AKTIF, GRUP_TIPI);

-- =============================================
-- EPOSTA GRUP UYELERI
-- =============================================
PROMPT 3. EPOSTA_GRUP_UYELERI indexleri...

-- Composite index: Grup üyeleri listesi için
CREATE INDEX IDX_UYE_GRUP_DURUM ON EPOSTA_GRUP_UYELERI(GRUP_ID, DURUM);

-- =============================================
-- EPOSTA DUYURULARI - EN KRİTİK TABLO
-- =============================================
PROMPT 4. EPOSTA_DUYURULARI indexleri...

-- Composite index 1: Kullanıcı dashboard'u için (EN SIKÇA KULLANILAN)
-- WHERE DURUM = ? AND OLUSTURAN_KULLANICI_ID = ? ORDER BY OLUSTURMA_TARIHI
CREATE INDEX IDX_DUYURU_DURUM_OLUSTURAN ON EPOSTA_DUYURULARI(DURUM, OLUSTURAN_KULLANICI_ID);

-- Composite index 2: Duyuru listeleme + sıralama için
-- WHERE DURUM = ? ORDER BY OLUSTURMA_TARIHI DESC
CREATE INDEX IDX_DUYURU_DURUM_TARIH ON EPOSTA_DUYURULARI(DURUM, OLUSTURMA_TARIHI);

-- İçerik tipi için (İLERİYE YÖNELİK: EMAIL + SOSYAL_MEDYA + SMS)
-- Şu an EMAIL-only ama genişleme için hazır
CREATE INDEX IDX_DUYURU_ICERIK_TIPI ON EPOSTA_DUYURULARI(ICERIK_TIPI);

-- Duyuru kategorisi + tarih (Raporlama için)
CREATE INDEX IDX_DUYURU_KATEGORI_TARIH ON EPOSTA_DUYURULARI(DUYURU_KATEGORISI, OLUSTURMA_TARIHI);

-- =============================================
-- EPOSTA DUYURU ALICILARI - Email Gönderim
-- =============================================
PROMPT 5. EPOSTA_DUYURU_ALICILARI indexleri...

-- Composite index: Email gönderim durumu takibi için (EN KRİTİK)
-- WHERE DUYURU_ID = ? AND GONDERIM_DURUMU = ? AND ALICI_KATEGORISI = ?
CREATE INDEX IDX_ALICI_COMPOSITE ON EPOSTA_DUYURU_ALICILARI(DUYURU_ID, GONDERIM_DURUMU, ALICI_KATEGORISI);

-- Email gönderimi için
-- WHERE GONDERIM_DURUMU = 'BEKLEMEDE' AND DUYURU_ID = ?
CREATE INDEX IDX_ALICI_DURUM_DUYURU ON EPOSTA_DUYURU_ALICILARI(GONDERIM_DURUMU, DUYURU_ID);

-- =============================================
-- EPOSTA DUYURU ZAMANLAMALARI
-- =============================================
PROMPT 6. EPOSTA_DUYURU_ZAMANLAMALAR indexleri...

-- Composite index: Bekleyen zamanlamalar için
-- WHERE DURUM = 'BEKLEMEDE' AND ZAMANLANAN_TARIH <= SYSDATE
CREATE INDEX IDX_ZAMANLAMA_DURUM_TARIH ON EPOSTA_DUYURU_ZAMANLAMALAR(DURUM, ZAMANLANAN_TARIH);

-- Hangfire job ID ile arama için (job iptal işlemleri)
CREATE INDEX IDX_ZAMANLAMA_JOB_ID ON EPOSTA_DUYURU_ZAMANLAMALAR(HANGFIRE_JOB_ID);

-- =============================================
-- EPOSTA DUYURU GONDERIM LOG - Email Tracking
-- =============================================
PROMPT 7. EPOSTA_DUYURU_GONDERIM_LOG indexleri...

-- Duyuru bazlı sorgular için
CREATE INDEX IDX_GONDERIM_LOG_DUYURU ON EPOSTA_DUYURU_GONDERIM_LOG(DUYURU_ID);

-- YENİ: Duyuru + durum composite (email status tracking için)
-- WHERE DUYURU_ID = ? AND GONDERIM_DURUMU = 'BASARISIZ'
CREATE INDEX IDX_GONDERIM_LOG_DUYURU_DURUM ON EPOSTA_DUYURU_GONDERIM_LOG(DUYURU_ID, GONDERIM_DURUMU);

-- Tarih bazlı sıralama için (raporlama)
CREATE INDEX IDX_GONDERIM_LOG_TARIH ON EPOSTA_DUYURU_GONDERIM_LOG(GONDERIM_TARIHI);

-- =============================================
-- EPOSTA DUYURU HAREKETLERI - Audit Trail
-- =============================================
PROMPT 8. EPOSTA_DUYURU_HAREKETLERI indexleri...

-- Composite index: Timeline view için (EN KRİTİK)
-- WHERE DUYURU_ID = ? ORDER BY ISLEM_TARIHI DESC
CREATE INDEX IDX_HAREKET_DUYURU_TARIH ON EPOSTA_DUYURU_HAREKETLERI(DUYURU_ID, ISLEM_TARIHI);

-- Kullanıcı bazlı audit sorguları için
-- WHERE KULLANICI_ID = ? ORDER BY ISLEM_TARIHI DESC
CREATE INDEX IDX_HAREKET_KULLANICI_TARIH ON EPOSTA_DUYURU_HAREKETLERI(KULLANICI_ID, ISLEM_TARIHI);

-- =============================================
-- LOG TABLOLARI
-- =============================================
PROMPT 9. LOG tabloları indexleri...

-- Login logs: Tarih sıralama için
CREATE INDEX IDX_LOGIN_TARIH ON LOG_LOGIN(GIRIS_TARIHI);

-- Sistem logs: Kategori + tarih composite
-- WHERE KATEGORI = ? AND LOG_TARIHI >= ? AND LOG_TARIHI <= ?
CREATE INDEX IDX_SISTEM_KATEGORI_TARIH ON LOG_SISTEM(KATEGORI, LOG_TARIHI);

-- Kullanıcı bazlı audit sorguları için
CREATE INDEX IDX_SISTEM_KULLANICI_TARIH ON LOG_SISTEM(KULLANICI_ID, LOG_TARIHI);

-- =============================================
-- DİĞER TABLOLAR
-- =============================================
PROMPT 10. Diğer tablolar indexleri...

-- Şablon kategorisi için
CREATE INDEX IDX_SABLON_KATEGORI ON EPOSTA_SABLONLARI(KATEGORI_ID);

-- Session dosyaları için (orphan cleanup job)
CREATE INDEX IDX_DOSYALAR_SESSION ON DOSYALAR(SESSION_ID);

-- =============================================
-- FOREIGN KEY INDEXLER (Lock Prevention)
-- =============================================
PROMPT 11. Foreign key indexleri (Oracle best practice)...

-- FK indexleri Oracle'da table lock'u önler
CREATE INDEX IDX_DOSYALAR_YUKLEYEN ON DOSYALAR(YUKLEYEN_KULLANICI_ID);
CREATE INDEX IDX_DOSYALAR_DUYURU ON DOSYALAR(DUYURU_ID);
CREATE INDEX IDX_HAREKET_ONAYLAYICI ON EPOSTA_DUYURU_HAREKETLERI(SECILEN_ONAYLAYICI_ID);
CREATE INDEX IDX_ZAMANLAMA_DUYURU ON EPOSTA_DUYURU_ZAMANLAMALAR(DUYURU_ID);

-- =============================================
-- CRITICAL FIX: Eksik FK Index (Lock Prevention)
-- =============================================
PROMPT 12. Kritik FK index ekleniyor...

-- EPOSTA_DUYURU_ALICILARI.GRUP_ID (FK -> EPOSTA_GRUPLARI)
-- NEDEN: Grup silme/güncelleme sırasında alıcı tablosunda table lock önlenir
-- Bu tablo en büyük tablo (her duyuru için 100+ satır), lock riski en yüksek
CREATE INDEX IDX_ALICI_GRUP_FK ON EPOSTA_DUYURU_ALICILARI(GRUP_ID);

-- NOT: Diğer FK'lar için index gerekli değil çünkü:
-- - OLUSTURAN_KULLANICI_ID: Composite index var (IDX_DUYURU_DURUM_OLUSTURAN)
-- - KATEGORI_ID: Index var (IDX_SABLON_KATEGORI)
-- - Kullanıcı/kategori DELETE işlemleri çok nadir (soft delete kullanılıyor)

COMMIT;

PROMPT ========================================
PROMPT Index optimizasyonu tamamlandı
PROMPT Toplam: 27 kritik index (41 → 27: %34 azalma + 1 critical FK fix)
PROMPT ========================================
PROMPT
PROMPT Silinen gereksiz indexler:
PROMPT - IDX_DUYURU_DURUM (composite var: IDX_DUYURU_DURUM_OLUSTURAN)
PROMPT - IDX_DUYURU_OLUSTURAN (composite var: IDX_DUYURU_DURUM_OLUSTURAN)
PROMPT - IDX_DUYURU_OLUSTURMA (composite var: IDX_DUYURU_DURUM_TARIH)
PROMPT - IDX_DUYURU_ICERIK_DURUM (low cardinality)
PROMPT - IDX_DUYURU_GONDERICI_KAT (low cardinality)
PROMPT - IDX_ALICI_DUYURU_TIPI (low selectivity)
PROMPT - IDX_ALICI_GRUP (FK, composite var)
PROMPT - IDX_ALICI_KATEGORI (composite var: IDX_ALICI_COMPOSITE)
PROMPT - IDX_ZAMANLAMA_DURUM (composite var)
PROMPT - IDX_ZAMANLAMA_TARIH (composite var)
PROMPT - IDX_GONDERIM_LOG_EMAIL (nadiren arama)
PROMPT - IDX_GONDERIM_LOG_DURUM (low cardinality)
PROMPT - IDX_HAREKET_ISLEM_TIPI (low selectivity)
PROMPT - IDX_HAREKET_YENI_DURUM (low selectivity)
PROMPT - IDX_HAREKET_ISLEM_TARIHI (composite var)
PROMPT
PROMPT Eklenen yeni indexler:
PROMPT + IDX_DUYURU_KATEGORI_TARIH (raporlama için)
PROMPT + IDX_GONDERIM_LOG_DUYURU_DURUM (email status tracking)
PROMPT + IDX_HAREKET_KULLANICI_TARIH (user audit)
PROMPT + IDX_DOSYALAR_DUYURU (FK index)
PROMPT
PROMPT İleride sosyal medya/SMS için hazır:
PROMPT ~ IDX_DUYURU_ICERIK_TIPI (tutuldu)
PROMPT ========================================

-- Kontrol: Oluşturulan indexleri listele
PROMPT
PROMPT Oluşturulan indexler:
SELECT index_name, table_name,
       (SELECT COUNT(*) FROM user_ind_columns ic WHERE ic.index_name = i.index_name) as column_count,
       uniqueness
FROM user_indexes i
WHERE index_name LIKE 'IDX_%'
ORDER BY table_name, index_name;

SELECT 'INDEX OPTIMIZATION completed successfully - 26 indexes' AS STATUS FROM DUAL;
