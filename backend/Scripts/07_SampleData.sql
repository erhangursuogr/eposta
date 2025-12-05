-- ==================================================
-- DEÜ DUYURU YÖNETİM SİSTEMİ - COMPLETE SAMPLE DATA
-- ==================================================
-- TAMAMEN YENİ SAMPLE DATA - PK/FK SORUNLARI ÇÖZÜLDÜ
-- Tüm test senaryoları için eksiksiz veri seti
-- ==================================================

PROMPT ========================================
PROMPT DEÜ Duyuru Sistemi - KOMPLE SAMPLE DATA
PROMPT ========================================

-- =============================================
-- 1. SISTEM AYARLARI (DİNAMİK EMAIL KATEGORİLERİ DAHİL)
-- =============================================
PROMPT 1. Sistem ayarları ve dinamik email kategorileri ekleniyor...

-- =============================================
-- 1.1. EMAIL İMZA TANIMLARI (SIGNATURE HTML)
-- =============================================
-- Email sonuna eklenecek HTML imzalar - AYAR_DEGER içinde HTML kodu
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_IMZA', 'PERSONEL', '<br/><br/><p style="margin-top:20px;padding-top:10px;border-top:1px solid #ccc;font-size:12px;color:#666;">Saygılarımızla,<br/>DEÜ Personel Daire Başkanlığı</p>', 'Personel email imzası - AYAR_DEGER alanında imza HTML kodu', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_IMZA', 'REKTORLUK', '<br/><br/><p style="margin-top:20px;padding-top:10px;border-top:1px solid #ccc;font-size:12px;color:#666;">Prof.Dr. Bayram YILMAZ<br/>Dokuz Eylül Üniversitesi</p>', 'Rektörlük email imzası - AYAR_DEGER alanında imza HTML kodu', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_IMZA', 'OGRENCI', '<br/><br/><p style="margin-top:20px;padding-top:10px;border-top:1px solid #ccc;font-size:12px;color:#666;">Saygılarımızla,<br/>DEÜ Akademik İşler</p>', 'Öğrenci email imzası - AYAR_DEGER alanında imza HTML kodu', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_IMZA', 'BID', '<br/><br/><p style="margin-top:20px;padding-top:10px;border-top:1px solid #ccc;font-size:12px;color:#666;">Saygılarımızla,<br/>Dokuz Eylül Üniversitesi<br/>Bilgi İşlem Dairesi</p>', 'BİD email imzası - AYAR_DEGER alanında imza HTML kodu', 'N', 'Y');

-- Mühendislik Fakültesi
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_IMZA', 'MUHENDISLIK',
'<div style="font-family: Arial, sans-serif; font-size: 12px; color: #333; margin-top: 20px; padding-top: 10px; border-top: 1px solid #ccc;">
  <p style="margin: 5px 0;"><strong>Mühendislik Fakültesi</strong></p>
  <p style="margin: 5px 0;">Dokuz Eylül Üniversitesi</p>
  <p style="margin: 5px 0;">Tel: (232) 412 XXXX | muhendislik@deu.edu.tr</p>
</div>',
'Mühendislik Fakültesi email imzası - AYAR_DEGER alanında imza HTML kodu',
'N', 'Y');

-- Tıp Fakültesi
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_IMZA', 'TIP',
'<div style="font-family: Arial, sans-serif; font-size: 12px; color: #333; margin-top: 20px; padding-top: 10px; border-top: 1px solid #ccc;">
  <p style="margin: 5px 0;"><strong>Tıp Fakültesi</strong></p>
  <p style="margin: 5px 0;">Dokuz Eylül Üniversitesi</p>
  <p style="margin: 5px 0;">Tel: (232) 412 YYYY | tip@deu.edu.tr</p>
</div>',
'Tıp Fakültesi email imzası - AYAR_DEGER alanında imza HTML kodu',
'N', 'Y');

-- İmzasız kategori örneği (boş string)
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_IMZA', 'GENEL_DUYURU', '',
'Genel duyurular (imzasız) - AYAR_DEGER boş = imza yok',
'N', 'Y');
-- =============================================
-- 1.2. PERSONEL KATEGORİSİ EMAIL AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_PERSONEL', 'FROM_EMAIL', 'duyuru@deu.edu.tr', 'Personel duyuruları gönderici email adresi', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_PERSONEL', 'FROM_NAME', 'DEÜ Duyuru Sistemi', 'Personel duyuruları gönderici görünen adı', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_PERSONEL', 'SMTP_USERNAME', '', 'SMTP kullanıcı adı (boş = DefaultCredentials)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_PERSONEL', 'SMTP_PASSWORD', '', 'SMTP şifresi (boş = DefaultCredentials)', 'Y', 'Y');

-- =============================================
-- 1.3. REKTÖRLÜK KATEGORİSİ EMAIL AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_REKTORLUK', 'FROM_EMAIL', 'bayram.yilmaz@deu.edu.tr', 'Rektörlük duyuruları gönderici email adresi', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_REKTORLUK', 'FROM_NAME', 'DEÜ Rektörlük', 'Rektörlük duyuruları gönderici görünen adı', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_REKTORLUK', 'SMTP_USERNAME', '', 'SMTP kullanıcı adı (yarın doldurulacak)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_REKTORLUK', 'SMTP_PASSWORD', '', 'SMTP şifresi (yarın doldurulacak)', 'Y', 'Y');

-- =============================================
-- 1.4. ÖĞRENCİ KATEGORİSİ EMAIL AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_OGRENCI', 'FROM_EMAIL', 'epostadestek@deu.edu.tr', 'Öğrenci duyuruları gönderici email adresi', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_OGRENCI', 'FROM_NAME', 'DEÜ Duyuru Destek', 'Öğrenci duyuruları gönderici görünen adı', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_OGRENCI', 'SMTP_USERNAME', '', 'SMTP kullanıcı adı (yarın doldurulacak)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_OGRENCI', 'SMTP_PASSWORD', '', 'SMTP şifresi (yarın doldurulacak)', 'Y', 'Y');

-- =============================================
-- 1.5. BİD KATEGORİSİ EMAIL AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_BID', 'FROM_EMAIL', 'bid.yardim@deu.edu.tr', 'BİD duyuruları gönderici email adresi', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_BID', 'FROM_NAME', 'DEÜ Bilgi İşlem', 'BİD duyuruları gönderici görünen adı', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_BID', 'SMTP_USERNAME', '', 'SMTP kullanıcı adı (yarın doldurulacak)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_BID', 'SMTP_PASSWORD', '', 'SMTP şifresi (yarın doldurulacak)', 'Y', 'Y');

-- =============================================
-- 1.5.1. SİSTEM BİLDİRİM KATEGORİSİ EMAIL AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_SISTEM', 'FROM_EMAIL', 'duyuru@deu.edu.tr', 'Sistem bildirimleri gönderici email adresi (onay/red/iptal bildirimleri)', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_SISTEM', 'FROM_NAME', 'DEÜ Duyuru Sistemi', 'Sistem bildirimleri gönderici görünen adı', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_SISTEM', 'SMTP_USERNAME', '', 'SMTP kullanıcı adı (boş = DefaultCredentials)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_SISTEM', 'SMTP_PASSWORD', '', 'SMTP şifresi (boş = DefaultCredentials)', 'Y', 'Y');

-- =============================================
-- 1.6. ORTAK SMTP AYARLARI (TÜM KATEGORİLER)
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_ORTAK', 'SMTP_SERVER', 'giden.posta.deu.edu.tr', 'Tüm kategoriler için SMTP sunucu adresi', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_ORTAK', 'SMTP_PORT', '25', 'Tüm kategoriler için SMTP port numarası', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_ORTAK', 'ENABLE_SSL', 'N', 'SMTP SSL/TLS kullan (DEÜ internal için kapalı)', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_ORTAK', 'USE_DEFAULT_CREDENTIALS', 'Y', 'Varsayılan kimlik bilgileri kullan', 'N', 'Y');

-- =============================================
-- 1.7. GENEL SİSTEM AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DOSYA', 'MAX_DOSYA_BOYUTU_MB', '10', 'Maksimum dosya boyutu (MB)', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DOSYA', 'IZIN_VERILEN_TIPLER', 'jpg,jpeg,png,gif,pdf,doc,docx,xls,xlsx,ppt,pptx,txt,zip,rar', 'İzin verilen dosya uzantıları', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DOSYA', 'DEPOLAMA_KLASORU', 'uploads/', 'Dosyaların depolanacağı klasör', 'N', 'Y');

-- =============================================
-- 1.8. DEBIS AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DEBIS', 'SERVICE_KEY', 'ndlar75aGaQ345a5s6s1a0?63dTTf', 'DEBIS SOAP servis anahtarı - LdapService.cs içinde kullanılıyor (hardcoded yerine)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DEBIS', 'RSA_PUBLIC_KEY', '<RSAKeyValue><Modulus>xOf++aLdynlRjbXGw4p3g04XrVKnSFqVXdwqGQJUOgOVcpOvFZ6oKp+oX7+8RgkLWRjHX+LgGqCzOedrv5FxkwOgb2OJ+rP5YEhRZqtFwCgRrHdYWR7T4p9KdjY36FJ7nYwp+jnZEj4tUhyOt4C5J4L6gT4Zz2z6NdLgHjJ0xBzJ4WYGp9pCx5ZfBJh+tZ0Y6ItFKgGeLOW6dM4sJyqOt+Tl6HdYZZE3Jj6cZWy5c1g3z3JEpx7+3XZ4cY3Y4g3L6EfZFGmCj2T4BzJ1YKgF1zR1L6T2+O0Y7XzJ7hGcO6qKO6+1Y8kZ0Y3zZ0G7CZ1J4pXgKgz0</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>', 'RSA public key - Email/password şifreleme için (appsettings.json yerine)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DEBIS', 'SOAP_ENDPOINT', 'http://debis.deu.edu.tr/DebisHSService.asmx', 'DEBIS SOAP web servis endpoint URL', 'N', 'Y');
-- =============================================
-- 1.9. CACHE AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('CACHE', 'EMAIL_CONFIG_MINUTES', '30', 'Email configuration cache süresi (dakika) - EmailCategoryService.cs içinde kullanılıyor', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('CACHE', 'USER_DATA_MINUTES', '10', 'Kullanıcı verisi cache süresi (dakika) - AuthService.cs içinde kullanılabilir', 'N', 'Y');
-- =============================================
-- 1.10. DOMAIN AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('SECURITY_DOMAIN', 'ALLOWED_EMAIL_DOMAINS', 'deu.edu.tr', 'İzin verilen email domain listesi (virgülle ayrılmış) - Email validasyonunda kullanılır', 'N', 'Y');

-- =============================================
-- 2. ROLLER
-- =============================================
PROMPT 2. Roller ekleniyor...

INSERT INTO ROLLER (ROL_KODU, ROL_ADI, ACIKLAMA, YETKI_SEVIYESI, AKTIF) VALUES
('ADMIN', 'Sistem Yöneticisi', 'Tüm işlemler: Oluştur + Düzenle + Onayla + Gönder + Sistem ayarları + Kullanıcı yönetimi', 10, 'Y');

INSERT INTO ROLLER (ROL_KODU, ROL_ADI, ACIKLAMA, YETKI_SEVIYESI, AKTIF) VALUES
('MANAGER', 'Üst Onaylayıcı', 'Son onay seviyesi: Kontrolörün onayladığı duyuruları onayla/reddet', 8, 'Y');

INSERT INTO ROLLER (ROL_KODU, ROL_ADI, ACIKLAMA, YETKI_SEVIYESI, AKTIF) VALUES
('COORDINATOR', 'Kontrolör', 'İlk onay seviyesi: Duyuruları incele, manager seçimi yap, onayla/reddet', 7, 'Y');

INSERT INTO ROLLER (ROL_KODU, ROL_ADI, ACIKLAMA, YETKI_SEVIYESI, AKTIF) VALUES
('EDITOR', 'Duyuru Editörü', 'Duyuru operasyonları: Oluştur + Düzenle + Onaya gönder + Gönder', 6, 'Y');

INSERT INTO ROLLER (ROL_KODU, ROL_ADI, ACIKLAMA, YETKI_SEVIYESI, AKTIF) VALUES
('VIEWER', 'Görüntüleyici', 'Sadece okuma yetkisi: Duyuruları görüntüleyebilir', 1, 'Y');

-- =============================================
-- 3. KULLANICI YÖNETİMİ
-- =============================================
PROMPT 3. Kullanıcı yönetimi hazırlanıyor...

-- Fake kullanıcılar - Sadece örnek duyurular için
-- Gerçek kullanıcılar LDAP ile giriş yapacak ve otomatik oluşacak

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('admin', 'Admin Kullanıcı', 'admin@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'ADMIN'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('manager', 'Manager Kullanıcı', 'manager@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'MANAGER'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('editor', 'Editor Kullanıcı', 'editor@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'EDITOR'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('erhan', 'Admin Kullanıcı', 'erhan@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'ADMIN'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('erhan.gursu', 'Admin Kullanıcı', 'erhan.gursu@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'EDITOR'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('yasar', 'Admin Kullanıcı', 'yasar@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'ADMIN'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('nezih', 'Admin Kullanıcı', 'nezih@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'ADMIN'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('sami', 'Manager Kullanıcı', 'sami@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'MANAGER'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('rukiye', 'Koordinatör Kullanıcı', 'rukiye@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'COORDINATOR'), 'Y', SYSDATE);

-- =============================================
-- 4. EPOSTA GRUPLARI - 4 TİP GRUP SİSTEMİ (YENİLENMİŞ MODEL)
-- =============================================
PROMPT 4. Email grupları (yeni model yapısı) ekleniyor...

-- NORMAL Grup - Manuel üye eklenen gruplar, TO/CC/BCC desteklenir
INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, AKTIF) VALUES
('Test Normal Grubu', 'Manuel eklenen üyelerle normal email gönderimi', 'NORMAL', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, AKTIF) VALUES
('Fakülte Dekanları', 'Tüm fakülte dekanları grubu - manuel üye yönetimi', 'NORMAL', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, AKTIF) VALUES
('Daire Başkanları', 'İdari daire başkanları grubu', 'NORMAL', 'Y');

-- STATIK Gruplar - Dış dosya/Excel tabanlı, sadece BCC
INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, VIEW_ADI, FILTER_KOSULU, AKTIF) VALUES
('Akademik Personel (Excel)', 'Excel dosyasından akademik personel listesi', 'STATIK', 'akademik_personel.xlsx', 'DURUM = AKTIF', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, VIEW_ADI, FILTER_KOSULU, AKTIF) VALUES
('İdari Personel (Excel)', 'Excel dosyasından idari personel listesi', 'STATIK', 'idari_personel.xlsx', 'DURUM = AKTIF', 'Y');

-- DINAMIK Gruplar - SQL view/sorgu tabanlı, sadece BCC
INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, VIEW_ADI, FILTER_KOSULU, AKTIF) VALUES
('Aktif Lisans Öğrencileri', 'Veritabanından dinamik lisans öğrenci listesi', 'DINAMIK', 'V_OGRENCI_LISANS', NULL, 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, VIEW_ADI, FILTER_KOSULU, AKTIF) VALUES
('Lisansüstü Öğrencileri', 'Veritabanından dinamik yüksek lisans/doktora öğrencileri', 'DINAMIK', 'V_OGRENCI_LISANSUSTU', NULL, 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, VIEW_ADI, FILTER_KOSULU, AKTIF) VALUES
('Tüm Akademik Personel', 'Veritabanından dinamik akademik personel listesi', 'DINAMIK', 'V_EMAIL_LISTE', 'GRUP=AKADEMIK', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, VIEW_ADI, FILTER_KOSULU, AKTIF) VALUES
('Tüm İdari Personel', 'Veritabanından dinamik idari personel listesi', 'DINAMIK', 'V_EMAIL_LISTE', 'GRUP=IDARI', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, VIEW_ADI, FILTER_KOSULU, AKTIF) VALUES
('Tüm Aktif Personel', 'Veritabanından tüm aktif personel listesi', 'DINAMIK', 'V_PERSONEL_AKTIF', NULL, 'Y');

-- DEBIS Gruplar - Listeci email entegrasyonu için, sadece BCC
INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, LISTECI_EMAIL, AKTIF) VALUES
('Akademik DEBIS Listesi', 'DEBIS entegrasyonu - akademik personel', 'DEBIS', 'akademik_44_seftali@deu.edu.tr', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, LISTECI_EMAIL, AKTIF) VALUES
('İdari DEBIS Listesi', 'DEBIS entegrasyonu - idari personel', 'DEBIS', 'idari_33_armut@deu.edu.tr', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, LISTECI_EMAIL, AKTIF) VALUES
('Öğrenci DEBIS Listesi', 'DEBIS entegrasyonu - tüm öğrenciler', 'DEBIS', 'ogrenci_55_kiraz@deu.edu.tr', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, LISTECI_EMAIL, AKTIF) VALUES
('Müdürler DEBIS Listesi', 'DEBIS entegrasyonu - daire başkanı ve müdürler', 'DEBIS', 'mudurler_77_elma@deu.edu.tr', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, LISTECI_EMAIL, AKTIF) VALUES
('Genel DEBIS Listesi', 'DEBIS entegrasyonu - tüm personel ve öğrenciler', 'DEBIS', 'tum_erhan2025@kordon.adm.deu.edu.tr', 'Y');

-- =============================================
-- 5. EPOSTA GRUP UYELERI
-- =============================================
PROMPT 5. Grup üyeleri ekleniyor...

-- Normal Grup (ID=1) üyeleri - manuel olarak eklenmiş
INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Normal Grubu'), 'erhan@deu.edu.tr', 'Erhan Gürsu 1', 'Mühendislik Fakültesi', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Normal Grubu'), 'erhan.gursu@deu.edu.tr', 'Erhan Gürsu 2', 'İktisadi ve İdari Bilimler Fakültesi', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Normal Grubu'), 'rukiye@deu.edu.tr', 'Test Kullanıcı 1', 'Test Departmanı', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Normal Grubu'), 'sami@deu.edu.tr', 'Test Kullanıcı 3', 'Test Departmanı', 'AKTIF');

-- Fakülte Dekanları grubu üyeleri
INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Fakülte Dekanları'), 'dekan.muhendislik@deu.edu.tr', 'Prof. Dr. Mühendislik Dekanı', 'Mühendislik Fakültesi', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Fakülte Dekanları'), 'dekan.iibf@deu.edu.tr', 'Prof. Dr. İİBF Dekanı', 'İktisadi ve İdari Bilimler Fakültesi', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Fakülte Dekanları'), 'dekan.tip@deu.edu.tr', 'Prof. Dr. Tıp Dekanı', 'Tıp Fakültesi', 'AKTIF');


-- Örnek kategoriler ekle
INSERT INTO EPOSTA_SABLON_KATEGORILERI (ID, KATEGORI_ADI, ACIKLAMA, RENK, IKON, SIRA_NO, AKTIF)
VALUES (SEQ_SABLON_KATEGORILERI.NEXTVAL, 'Vefat', 'Vefat duyuruları', '#424242', 'sentiment_very_dissatisfied', 1, 'Y');

INSERT INTO EPOSTA_SABLON_KATEGORILERI (ID, KATEGORI_ADI, ACIKLAMA, RENK, IKON, SIRA_NO, AKTIF)
VALUES (SEQ_SABLON_KATEGORILERI.NEXTVAL, 'Kutlama', 'Kutlama ve tebrik duyuruları', '#4caf50', 'celebration', 2, 'Y');

INSERT INTO EPOSTA_SABLON_KATEGORILERI (ID, KATEGORI_ADI, ACIKLAMA, RENK, IKON, SIRA_NO, AKTIF)
VALUES (SEQ_SABLON_KATEGORILERI.NEXTVAL, 'Başarı', 'Başarı ve ödül duyuruları', '#ff9800', 'emoji_events', 3, 'Y');

INSERT INTO EPOSTA_SABLON_KATEGORILERI (ID, KATEGORI_ADI, ACIKLAMA, RENK, IKON, SIRA_NO, AKTIF)
VALUES (SEQ_SABLON_KATEGORILERI.NEXTVAL, 'Haber', 'Genel haber duyuruları', '#2196f3', 'newspaper', 4, 'Y');

INSERT INTO EPOSTA_SABLON_KATEGORILERI (ID, KATEGORI_ADI, ACIKLAMA, RENK, IKON, SIRA_NO, AKTIF)
VALUES (SEQ_SABLON_KATEGORILERI.NEXTVAL, 'Davet', 'Etkinlik davetleri', '#9c27b0', 'event', 5, 'Y');

INSERT INTO EPOSTA_SABLON_KATEGORILERI (ID, KATEGORI_ADI, ACIKLAMA, RENK, IKON, SIRA_NO, AKTIF)
VALUES (SEQ_SABLON_KATEGORILERI.NEXTVAL, 'Bilgilendirme', 'Bilgilendirme mesajları', '#00bcd4', 'info', 6, 'Y');

INSERT INTO EPOSTA_SABLON_KATEGORILERI (ID, KATEGORI_ADI, ACIKLAMA, RENK, IKON, SIRA_NO, AKTIF)
VALUES (SEQ_SABLON_KATEGORILERI.NEXTVAL, 'Uyarı', 'Uyarı ve duyuru mesajları', '#f44336', 'warning', 7, 'Y');

-- =============================================
-- 6. EPOSTA SABLONLARI
-- =============================================
PROMPT 6. Email şablonları ekleniyor...

INSERT INTO EPOSTA_SABLONLARI (KATEGORI_ID, SABLON_ADI, KONU_SABLONU, ICERIK_SABLONU, VARSAYILAN, AKTIF) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'Horizon Europe Başarısı'),'Varsayılan DEÜ Şablonu', '[DEÜ] {{konu}}', 
'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd;">
    <div style="background-color: #0066cc; color: white; padding: 20px; text-align: center;">
        <h1 style="margin: 0; font-size: 24px;">Dokuz Eylül Üniversitesi</h1>
        <h2 style="margin: 10px 0 0 0; font-size: 18px; font-weight: normal;">{{konu}}</h2>
    </div>
    <div style="padding: 20px; background-color: #f9f9f9; line-height: 1.6;">
        {{icerik}}
    </div>
    <div style="background-color: #333; color: white; padding: 15px; text-align: center; font-size: 12px;">
        <p style="margin: 0;">Bu email DEÜ Duyuru Yönetim Sistemi tarafından gönderilmiştir.</p>
        <p style="margin: 5px 0 0 0;">© 2024 Dokuz Eylül Üniversitesi - Tüm hakları saklıdır.</p>
    </div>
</div>','Y', 'Y');

INSERT INTO EPOSTA_SABLONLARI (KATEGORI_ID, SABLON_ADI, KONU_SABLONU, ICERIK_SABLONU, VARSAYILAN, AKTIF) VALUES
(2,'Kutlama ve Tebrik Şablonu', '[DEÜ Kutlama] {{konu}}', 
'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd;">
    <div style="background-color: #28a745; color: white; padding: 20px; text-align: center;">
        <h1 style="margin: 0; font-size: 24px;">🎉 Dokuz Eylül Üniversitesi</h1>
        <h2 style="margin: 10px 0 0 0; font-size: 18px; font-weight: normal;">{{konu}}</h2>
    </div>
    <div style="padding: 20px; background-color: #f8f9fa; line-height: 1.6;">
        {{icerik}}
    </div>
    <div style="background-color: #333; color: white; padding: 15px; text-align: center; font-size: 12px;">
        <p style="margin: 0;">Bu kutlama mesajı DEÜ Duyuru Yönetim Sistemi tarafından gönderilmiştir.</p>
        <p style="margin: 5px 0 0 0;">© 2024 Dokuz Eylül Üniversitesi</p>
    </div>
</div>','N', 'Y');

INSERT INTO EPOSTA_SABLONLARI (KATEGORI_ID, SABLON_ADI, KONU_SABLONU, ICERIK_SABLONU, VARSAYILAN, AKTIF) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'),'Önemli Duyuru Şablonu', '[DEÜ ÖNEMLİ] {{konu}}', 
'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 2px solid #ffc107;">
    <div style="background-color: #ffc107; color: #212529; padding: 20px; text-align: center;">
        <h1 style="margin: 0; font-size: 24px;">⚠️ Dokuz Eylül Üniversitesi</h1>
        <h2 style="margin: 10px 0 0 0; font-size: 18px; font-weight: bold; color: #856404;">ÖNEMLİ: {{konu}}</h2>
    </div>
    <div style="padding: 20px; background-color: #fff3cd; line-height: 1.6; border-left: 4px solid #ffc107;">
        {{icerik}}
    </div>
    <div style="background-color: #6c757d; color: white; padding: 15px; text-align: center; font-size: 12px;">
        <p style="margin: 0;">Bu önemli duyuru DEÜ Duyuru Yönetim Sistemi tarafından gönderilmiştir.</p>
        <p style="margin: 5px 0 0 0;">Lütfen dikkatlice okuyunuz.</p>
    </div>
</div>','N', 'Y');

INSERT INTO EPOSTA_SABLONLARI (KATEGORI_ID, SABLON_ADI, KONU_SABLONU, ICERIK_SABLONU, VARSAYILAN, AKTIF) VALUES
(7,'Acil Durum Şablonu', '[DEÜ ACİL] {{konu}}', 
'<div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 2px solid #dc3545;">
    <div style="background-color: #dc3545; color: white; padding: 20px; text-align: center;">
        <h1 style="margin: 0; font-size: 24px;">🚨 Dokuz Eylül Üniversitesi</h1>
        <h2 style="margin: 10px 0 0 0; font-size: 18px; font-weight: bold;">ACİL: {{konu}}</h2>
    </div>
    <div style="padding: 20px; background-color: #f8d7da; line-height: 1.6; border-left: 4px solid #dc3545;">
        {{icerik}}
    </div>
    <div style="background-color: #721c24; color: white; padding: 15px; text-align: center; font-size: 12px;">
        <p style="margin: 0; font-weight: bold;">ACİL DURUM BİLGİLENDİRMESİ</p>
        <p style="margin: 5px 0 0 0;">Bu mesaj acil durum protokolü çerçevesinde gönderilmiştir.</p>
    </div>
</div>','N', 'Y');

-- =============================================
-- 7. ÖRNEK DUYURULAR - FARKLI DURUMLARDA
-- =============================================
PROMPT 7. Örnek duyurular ekleniyor...

-- 1. TASLAK durumunda duyuru (PERSONEL kategorisi) - EMAIL içerik
INSERT INTO EPOSTA_DUYURULARI (KONU, ICERIK, ACIKLAMA, ICERIK_TIPI, DUYURU_KATEGORISI, GONDERICI_KATEGORI, SABLON_ID, OLUSTURAN_KULLANICI_ID, DURUM, TOPLAM_ALICI_SAYISI) VALUES
('DEÜ 2024-2025 Güz Dönemi Akademik Takvimi',
'<h2>Sevgili DEÜ Ailesi,</h2>
<p>2024-2025 Eğitim-Öğretim yılı Güz dönemi <strong>16 Eylül 2024 Pazartesi</strong> günü başlayacaktır.</p>

<h3>📅 Önemli Tarihler:</h3>
<ul>
<li><strong>Ders Kayıtları:</strong> 9-13 Eylül 2024</li>
<li><strong>Dersler Başlangıcı:</strong> 16 Eylül 2024 Pazartesi</li>
<li><strong>Add/Drop İşlemleri:</strong> 16-20 Eylül 2024</li>
<li><strong>Ara Sınav Haftası:</strong> 4-8 Kasım 2024</li>
<li><strong>Final Sınavları:</strong> 16-27 Aralık 2024</li>
</ul>

<h3>📝 Dikkat Edilecek Hususlar:</h3>
<p>• Ders kayıtları öğrenci bilgi sistemi üzerinden yapılacaktır.<br>
• Add/Drop işlemleri için dekanlık onayı gereklidir.<br>
• Devamsızlık durumu takip edilecektir.</p>

<p>Tüm öğrencilerimize başarılı bir eğitim-öğretim yılı dileriz.</p>

<p><strong>Rektörlük</strong><br>
<em>Dokuz Eylül Üniversitesi</em></p>',
'2024-2025 Güz dönemi akademik takvimi ve önemli tarihlerin duyurulması',
'EMAIL',
'REKTORLUK',
'EMAIL_REKTORLUK',
(SELECT ID FROM EPOSTA_SABLONLARI WHERE VARSAYILAN = 'Y'),
(SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'TASLAK', 0);

-- 2. ILK_ONAY_BEKLIYOR durumunda duyuru (BID kategorisi) - EMAIL içerik
INSERT INTO EPOSTA_DUYURULARI (KONU, ICERIK, ACIKLAMA, ICERIK_TIPI, DUYURU_KATEGORISI, GONDERICI_KATEGORI, SABLON_ID, OLUSTURAN_KULLANICI_ID, DURUM, TOPLAM_ALICI_SAYISI) VALUES
('DEÜ Bilgi Sistemleri Planlı Bakım Duyurusu',
'<h2>Değerli Kullanıcılar,</h2>
<p>Bilgi sistemlerimizde <strong>planlı bakım çalışması</strong> yapılacaktır.</p>

<h3>🔧 Bakım Detayları:</h3>
<p><strong>📅 Tarih:</strong> 20 Eylül 2024 Cuma - 21 Eylül 2024 Cumartesi<br>
<strong>🕐 Saat:</strong> 22:00 - 06:00 (8 saat)</strong></p>

<h3>⚠️ Etkilenecek Sistemler:</h3>
<ul>
<li>🎓 Öğrenci Bilgi Sistemi (OBS)</li>
<li>👥 Personel Bilgi Sistemi (PBS)</li>
<li>📧 Kurumsal E-posta Sistemi</li>
<li>📚 Kütüphane Sistemi</li>
<li>💰 Mali İşler Sistemi</li>
</ul>

<h3>🚫 Önemli Uyarı:</h3>
<p style="background-color: #fff3cd; padding: 10px; border-left: 4px solid #ffc107;">
<strong>Bu süre zarfında yukarıdaki sistemlere erişim mümkün olmayacaktır.</strong><br>
Lütfen bu durumu göz önünde bulundurarak işlemlerinizi planlayınız.
</p>

<p>Yaşanabilecek aksaklıklar için şimdiden özür dileriz.</p>

<p><strong>Bilgi İşlem Daire Başkanlığı</strong><br>
<em>Tel: 0232 xxx xxxx | Email: bilgiislem@deu.edu.tr</em></p>',
'Planlı sistem bakımı: OBS, PBS, e-posta ve diğer sistemlerde 8 saat kesinti olacak',
'EMAIL',
'BID',
'EMAIL_BID',
(SELECT ID FROM EPOSTA_SABLONLARI WHERE SABLON_ADI = 'Önemli Duyuru Şablonu'),
(SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'),
'ILK_ONAY_BEKLIYOR', 0);

-- 3. SON_ONAY_BEKLIYOR durumunda duyuru (REKTORLUK kategorisi) - EMAIL içerik
INSERT INTO EPOSTA_DUYURULARI (KONU, ICERIK, ACIKLAMA, ICERIK_TIPI, DUYURU_KATEGORISI, GONDERICI_KATEGORI, SABLON_ID, OLUSTURAN_KULLANICI_ID, SON_ONAYLAYAN_KULLANICI_ID, DURUM, TOPLAM_ALICI_SAYISI) VALUES
('DEÜ''nin Horizon Europe Projesi Kabul Edildi',
'<h2>Sevgili DEÜ Ailesi,</h2>
<p>🎉 <strong>Büyük bir başarıya imza attık!</strong></p>

<p>Mühendislik Fakültemiz öğretim üyelerinin konsorsiyum lideri olduğu
<strong>"Sürdürülebilir Enerji Teknolojileri"</strong> projesi
<strong>Horizon Europe</strong> programı kapsamında kabul edilmiştir.</p>

<h3>📊 Proje Detayları:</h3>
<ul>
<li><strong>Proje Bütçesi:</strong> 2.5 Milyon Euro</li>
<li><strong>Süre:</strong> 3 Yıl (2024-2027)</li>
<li><strong>Ortak Ülkeler:</strong> Türkiye, Almanya, Fransa, İtalya</li>
<li><strong>Konsorsiyum Lideri:</strong> Prof. Dr. Ahmet Yılmaz</li>
</ul>

<h3>🎯 Proje Kapsamı:</h3>
<p>Bu proje ile yenilenebilir enerji teknolojileri alanında
Avrupa''nın öncü araştırma merkezlerinden biri olmayı hedefliyoruz.</p>

<p>Bu başarıdan dolayı tüm ekibimizi kutluyor,
üniversitemize hayırlı olmasını diliyoruz.</p>

<p><strong>Rektör Prof. Dr. Bayram Yılmaz</strong><br>
<em>Dokuz Eylül Üniversitesi</em></p>',
'Horizon Europe projesi kabul kutlaması - tüm üniversite camiasına duyurulacak',
'EMAIL',
'REKTORLUK',
'EMAIL_REKTORLUK',
(SELECT ID FROM EPOSTA_SABLONLARI WHERE SABLON_ADI = 'Kutlama ve Tebrik Şablonu'),
(SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
(SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'),
'SON_ONAY_BEKLIYOR', 0);

-- 4. GONDERILDI durumunda duyuru (BID kategorisi) - EMAIL içerik
INSERT INTO EPOSTA_DUYURULARI (KONU, ICERIK, ACIKLAMA, ICERIK_TIPI, DUYURU_KATEGORISI, GONDERICI_KATEGORI, SABLON_ID, OLUSTURAN_KULLANICI_ID, SON_ONAYLAYAN_KULLANICI_ID, GERCEK_GONDERIM_TARIHI, DURUM, TOPLAM_ALICI_SAYISI, BASARILI_GONDERIM_SAYISI) VALUES
('ACİL: DEÜ Kampüsü Yangın Tatbikatı',
'<h2>🚨 DEÜ Kampüsü Acil Durum Tatbikatı</h2>

<p><strong>Tarih:</strong> 25 Eylül 2024 Çarşamba<br>
<strong>Saat:</strong> 14:00 - 15:00<br>
<strong>Kapsam:</strong> Tüm Kampüs Alanı</p>

<h3>⚠️ DİKKAT EDİLMESİ GEREKENLER:</h3>

<ol>
<li><strong>Alarm Sesi:</strong> Saat 14:00''da kampüs genelinde alarm sesi duyulacaktır.</li>
<li><strong>Tahliye:</strong> Tüm personel ve öğrenciler derhal binalardan çıkmalıdır.</li>
<li><strong>Toplanma Alanları:</strong> Önceden belirlenen toplanma alanlarına gidilmelidir.</li>
<li><strong>Asansör Kullanımı:</strong> Tatbikat süresince asansörler kullanılmayacaktır.</li>
</ol>

<h3>📍 Toplanma Alanları:</h3>
<ul>
<li><strong>Rektörlük:</strong> Ana kapı önü açık alan</li>
<li><strong>Mühendislik Fakültesi:</strong> Fakülte parkı</li>
<li><strong>Tıp Fakültesi:</strong> Hastane bahçesi</li>
<li><strong>İİBF:</strong> Dekanlık önü meydan</li>
</ul>

<h3>🚫 UYARI:</h3>
<p style="background-color: #f8d7da; padding: 15px; border: 1px solid #dc3545; border-radius: 5px;">
<strong>Bu bir tatbikattır!</strong> Ancak gerçek bir acil durum gibi hareket edilmelidir.
Tatbikat süresince kampüs dışına çıkış-giriş kısıtlanacaktır.
</p>

<p><strong>Güvenlik Müdürlüğü</strong><br>
<strong>Personel Daire Başkanlığı</strong><br>
<em>Dokuz Eylül Üniversitesi</em></p>',
'Yangın ve acil durum tahliye tatbikatı - kampüs geneli',
'EMAIL',
'BID',
'EMAIL_BID',
(SELECT ID FROM EPOSTA_SABLONLARI WHERE SABLON_ADI = 'Acil Durum Şablonu'),
(SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'),
(SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'),
SYSDATE - 1,
'GONDERILDI', 1250, 1248);

-- 5. REDDEDILDI durumunda duyuru örneği - EMAIL içerik
INSERT INTO EPOSTA_DUYURULARI (KONU, ICERIK, ACIKLAMA, ICERIK_TIPI, DUYURU_KATEGORISI, GONDERICI_KATEGORI, SABLON_ID, OLUSTURAN_KULLANICI_ID, DURUM, TOPLAM_ALICI_SAYISI) VALUES
('DEÜ Bahar Şenliği 2024',
'<h2>Sevgili Öğrenciler,</h2>
<p>DEÜ Bahar Şenliği <strong>15-17 Mayıs 2024</strong> tarihlerinde düzenlenecektir.</p>

<h3>🎉 Etkinlikler:</h3>
<ul>
<li>Konserler</li>
<li>Yarışmalar</li>
<li>Sergiler</li>
</ul>

<p>Detaylı program yakında duyurulacaktır.</p>',
'Bahar şenliği etkinlik duyurusu - öğrencilere gönderilecek',
'EMAIL',
'OGRENCI',
'EMAIL_OGRENCI',
(SELECT ID FROM EPOSTA_SABLONLARI WHERE SABLON_ADI = 'Modern Duyuru Şablonu'),
(SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'REDDEDILDI', 0);

-- 6. SOSYAL MEDYA içerik örneği - ONAYLANDI durumunda
INSERT INTO EPOSTA_DUYURULARI (KONU, ICERIK, ACIKLAMA, ICERIK_TIPI, DUYURU_KATEGORISI, GONDERICI_KATEGORI, SABLON_ID, OLUSTURAN_KULLANICI_ID, SON_ONAYLAYAN_KULLANICI_ID, DURUM, TOPLAM_ALICI_SAYISI) VALUES
('Horizon Europe Başarısı',
'🎉 DEÜ Horizon Europe''da!

Mühendislik Fakültemizin "Sürdürülebilir Enerji Teknolojileri" projesi Horizon Europe tarafından kabul edildi!

📊 2.5 Milyon Euro bütçe
🌍 4 ülke ortaklığı
👨‍🔬 Prof. Dr. Ahmet Yılmaz liderliğinde

Detaylar: www.deu.edu.tr/haberler/horizon-europe-2024

#DEÜ #HorizonEurope #Araştırma #İnovasyon',
'Horizon Europe projesi sosyal medya paylaşımı - X, Instagram, LinkedIn',
'SOSYAL_MEDYA',
'REKTORLUK',
'EMAIL_REKTORLUK',
(SELECT ID FROM EPOSTA_SABLONLARI WHERE VARSAYILAN = 'Y'),
(SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
(SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'),
'ONAYLANDI', 0);

-- =============================================
-- 7A. DUYURU HAREKETLERİ (AUDIT TRAIL)
-- =============================================
PROMPT 7A. Duyuru hareketleri (audit trail) ekleniyor...

-- Duyuru 1: TASLAK (sadece oluşturma hareketi)
INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ 2024-2025 Güz Dönemi Akademik Takvimi'), 'TASLAK', 'OLUSTURMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'Yeni duyuru taslağı oluşturuldu', SYSDATE - 5);

-- Duyuru 2: ILK_ONAY_BEKLIYOR (oluşturma + onaya gönderme)
INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ Bilgi Sistemleri Planlı Bakım Duyurusu'), 'TASLAK', 'OLUSTURMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'),
'Sistem bakımı duyurusu oluşturuldu', SYSDATE - 4);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ Bilgi Sistemleri Planlı Bakım Duyurusu'), 'TASLAK', 'ILK_ONAY_BEKLIYOR', 'ONAYA_GONDERME', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'),
'Kontrolör onayına gönderildi', SYSDATE - 4 + 1/24);

-- Duyuru 3: SON_ONAY_BEKLIYOR (oluşturma + ilk onay + Kontrolör onayı + manager seçimi)
INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), 'TASLAK', 'OLUSTURMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'Proje başarısı duyurusu oluşturuldu',SYSDATE - 3);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), 'TASLAK', 'ILK_ONAY_BEKLIYOR', 'ONAYA_GONDERME', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'Kontrolör onayına gönderildi', SYSDATE - 3 + 2/24);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, SECILEN_ONAYLAYICI_ID, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), 'ILK_ONAY_BEKLIYOR', 'SON_ONAY_BEKLIYOR', 'ONAYLAMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'koordinator'),
'Kontrolör onayladı ve manager seçti', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'), SYSDATE - 2);

-- Duyuru 4: GONDERILDI (tam workflow: oluşturma + ilk onay + Kontrolör onayı + manager onayı + gönderim)
INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), 'TASLAK', 'OLUSTURMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'),
'Yangın tatbikatı duyurusu oluşturuldu', SYSDATE - 3);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), 'TASLAK', 'ILK_ONAY_BEKLIYOR', 'ONAYA_GONDERME', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'),
'Kontrolör onayına gönderildi', SYSDATE - 3 + 1/24);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, SECILEN_ONAYLAYICI_ID, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), 'ILK_ONAY_BEKLIYOR', 'SON_ONAY_BEKLIYOR', 'ONAYLAMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'koordinator'),
'Kontrolör onayladı, acil durum duyurusu olduğu için hemen manager onayına gönderildi', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'), SYSDATE - 2);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), 'SON_ONAY_BEKLIYOR', 'ONAYLANDI', 'ONAYLAMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'),
'Manager final onayı verdi', SYSDATE - 2 + 2/24);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), 'ONAYLANDI', 'GONDERILDI', 'GONDERIM', NULL,
'Email gönderimi başarıyla tamamlandı (1248/1250)', SYSDATE - 1);

-- Duyuru 5: REDDEDILDI (oluşturma + onaya gönderme + Kontrolör reddi)
INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ Bahar Şenliği 2024'), 'TASLAK', 'OLUSTURMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'Bahar şenliği duyurusu oluşturuldu', SYSDATE - 4);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ Bahar Şenliği 2024'), 'TASLAK', 'ILK_ONAY_BEKLIYOR', 'ONAYA_GONDERME', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'Kontrolör onayına gönderildi', SYSDATE - 4 + 2/24);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ Bahar Şenliği 2024'), 'ILK_ONAY_BEKLIYOR', 'REDDEDILDI', 'REDDETME', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'koordinator'),
'İçerik yetersiz. Etkinlik detayları, saatler ve mekanlar eklenmeli. Ayrıca kayıt bilgileri eksik.', SYSDATE - 3);

-- Duyuru 6: SOSYAL MEDYA - ONAYLANDI (oluşturma + onaya gönderme + Kontrolör onayı + manager onayı)
INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'Horizon Europe Başarısı'), 'TASLAK', 'OLUSTURMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'Sosyal medya içeriği oluşturuldu', SYSDATE - 2);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'Horizon Europe Başarısı'), 'TASLAK', 'ILK_ONAY_BEKLIYOR', 'ONAYA_GONDERME', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'Kontrolör onayına gönderildi', SYSDATE - 2 + 1/24);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, SECILEN_ONAYLAYICI_ID, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'Horizon Europe Başarısı'), 'ILK_ONAY_BEKLIYOR', 'SON_ONAY_BEKLIYOR', 'ONAYLAMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'koordinator'),
'Kontrolör onayladı, sosyal medya içeriği için manager seçildi', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'),  SYSDATE - 1);

INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, ONCEKI_DURUM, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'Horizon Europe Başarısı'), 'SON_ONAY_BEKLIYOR', 'ONAYLANDI', 'ONAYLAMA', (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'),
'Manager final onayı verdi. Sosyal medya içeriği gönderilmeyecek, sadece onaylandı.', SYSDATE - 1 + 2/24);

-- =============================================
-- 8. DUYURU ALICILARI
-- =============================================
PROMPT 8. Duyuru alıcıları ekleniyor...

-- 1. duyuru için normal grup alıcıları - EMAIL NULL çünkü GRUP tipi
INSERT INTO EPOSTA_DUYURU_ALICILARI (DUYURU_ID, GRUP_ID, ALICI_TIPI, ALICI_KATEGORISI, GONDERIM_DURUMU) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ 2024-2025 Güz Dönemi Akademik Takvimi'), (SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Normal Grubu'), 'GRUP', 'TO', 'BEKLIYOR');

-- 2. duyuru için DEBIS grup (BCC) - EMAIL NULL çünkü GRUP tipi
INSERT INTO EPOSTA_DUYURU_ALICILARI (DUYURU_ID, GRUP_ID, ALICI_TIPI, ALICI_KATEGORISI, GONDERIM_DURUMU) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ Bilgi Sistemleri Planlı Bakım Duyurusu'), (SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'İdari DEBIS Listesi'), 'GRUP', 'BCC', 'BEKLIYOR');

-- 2. duyuru için manuel alıcı (CC) - EMAIL zorunlu çünkü MANUEL tipi
INSERT INTO EPOSTA_DUYURU_ALICILARI (DUYURU_ID, ALICI_TIPI, ALICI_KATEGORISI, EMAIL, AD_SOYAD, GONDERIM_DURUMU) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ Bilgi Sistemleri Planlı Bakım Duyurusu'), 'MANUEL', 'CC', 'rektorluk@deu.edu.tr', 'Rektörlük', 'BEKLIYOR');

-- 3. duyuru için DİNAMİK GRUP kullanımı - V_EMAIL_AKADEMIK view'dan alıcıları otomatik çekecek
INSERT INTO EPOSTA_DUYURU_ALICILARI (DUYURU_ID, GRUP_ID, ALICI_TIPI, ALICI_KATEGORISI, GONDERIM_DURUMU) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), (SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Tüm Akademik Personel'), 'GRUP', 'BCC', 'BEKLIYOR');

-- 4. duyuru için tüm personel (BCC) - GÖNDERİLDİ - EMAIL NULL çünkü GRUP tipi
INSERT INTO EPOSTA_DUYURU_ALICILARI (DUYURU_ID, GRUP_ID, ALICI_TIPI, ALICI_KATEGORISI, GONDERIM_DURUMU, GONDERIM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), (SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Akademik DEBIS Listesi'), 'GRUP', 'BCC', 'GONDERILDI', SYSDATE - 1);

INSERT INTO EPOSTA_DUYURU_ALICILARI (DUYURU_ID, GRUP_ID, ALICI_TIPI, ALICI_KATEGORISI, GONDERIM_DURUMU, GONDERIM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), (SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'İdari DEBIS Listesi'), 'GRUP', 'BCC', 'GONDERILDI', SYSDATE - 1);

-- =============================================
-- 9. EPOSTA DUYURU ZAMANLAMALARI (Recurring Schedules)
-- =============================================
PROMPT 9. Duyuru zamanlamaları (recurring schedules) ekleniyor...

-- ONAYLANDI durumundaki duyuru (ID=3) için çoklu zamanlama - recurring monthly announcement
-- Her ayın 1'inde gönderilmesi planlanmış
INSERT INTO EPOSTA_DUYURU_ZAMANLAMALAR (DUYURU_ID, ZAMANLANAN_TARIH, DURUM, ALICI_SAYISI, OLUSTURAN_KULLANICI_ID) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), TO_DATE('2024-10-01 09:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'BEKLEMEDE', 0, (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'));

INSERT INTO EPOSTA_DUYURU_ZAMANLAMALAR (DUYURU_ID, ZAMANLANAN_TARIH, DURUM, ALICI_SAYISI, OLUSTURAN_KULLANICI_ID) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), TO_DATE('2024-11-01 09:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'BEKLEMEDE', 0, (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'));

INSERT INTO EPOSTA_DUYURU_ZAMANLAMALAR (DUYURU_ID, ZAMANLANAN_TARIH, DURUM, ALICI_SAYISI, OLUSTURAN_KULLANICI_ID) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), TO_DATE('2024-12-01 09:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'BEKLEMEDE', 0, (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'));

-- ONAYLANDI durumundaki duyuru (ID=3) için geçmiş tarihli zamanlama - already sent
INSERT INTO EPOSTA_DUYURU_ZAMANLAMALAR (DUYURU_ID, ZAMANLANAN_TARIH, DURUM, GONDERIM_TARIHI, ALICI_SAYISI, OLUSTURAN_KULLANICI_ID) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), SYSDATE - 5, 'GONDERILDI', SYSDATE - 5, 1250, (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'));

-- GONDERILDI durumundaki duyuru (ID=4) için geçmiş zamanlama - completed
INSERT INTO EPOSTA_DUYURU_ZAMANLAMALAR (DUYURU_ID, ZAMANLANAN_TARIH, DURUM, GONDERIM_TARIHI, ALICI_SAYISI, OLUSTURAN_KULLANICI_ID, HANGFIRE_JOB_ID) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), SYSDATE - 1, 'GONDERILDI', SYSDATE - 1, 1250, (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'), 'job_12345_completed');

-- Weekly recurring schedule example - her hafta Pazartesi 10:00
INSERT INTO EPOSTA_DUYURU_ZAMANLAMALAR (DUYURU_ID, ZAMANLANAN_TARIH, DURUM, ALICI_SAYISI, OLUSTURAN_KULLANICI_ID, HANGFIRE_JOB_ID) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), TO_DATE('2024-09-23 10:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'BEKLEMEDE', 0, (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'), 'job_weekly_123');

INSERT INTO EPOSTA_DUYURU_ZAMANLAMALAR (DUYURU_ID, ZAMANLANAN_TARIH, DURUM, ALICI_SAYISI, OLUSTURAN_KULLANICI_ID, HANGFIRE_JOB_ID) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), TO_DATE('2024-09-30 10:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'BEKLEMEDE', 0, (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'), 'job_weekly_124');

-- İptal edilmiş zamanlama örneği
INSERT INTO EPOSTA_DUYURU_ZAMANLAMALAR (DUYURU_ID, ZAMANLANAN_TARIH, DURUM, ALICI_SAYISI, OLUSTURAN_KULLANICI_ID, IPTAL_NOTU) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), TO_DATE('2024-09-25 14:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'IPTAL', 0, (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'), 'Yönetici talebi ile iptal edildi');

-- Hata durumunda zamanlama örneği
INSERT INTO EPOSTA_DUYURU_ZAMANLAMALAR (DUYURU_ID, ZAMANLANAN_TARIH, DURUM, ALICI_SAYISI, OLUSTURAN_KULLANICI_ID, HATA_MESAJI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ''nin Horizon Europe Projesi Kabul Edildi'), SYSDATE - 2, 'HATA', 0, (SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'), 'SMTP connection timeout - gönderim tekrar denenecek');

-- =============================================
-- 10. ÖRNEK DOSYALAR
-- =============================================
PROMPT 10. Örnek dosya kayıtları ekleniyor...

INSERT INTO DOSYALAR (YUKLEYEN_KULLANICI_ID, DOSYA_ADI, DOSYA_YOLU, DOSYA_TIPI, DOSYA_KATEGORISI, DOSYA_BOYUTU, ACIKLAMA, AKTIF) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'), 'Yangın Tatbikatı Haritası.pdf', 'uploads/2024/09/yangin_tatbikat_haritasi.pdf', 'application/pdf', 'ATTACHMENT', 524288, 'Kampüs yangın tatbikatı toplanma alanları haritası', 'Y');

INSERT INTO DOSYALAR (YUKLEYEN_KULLANICI_ID, DOSYA_ADI, DOSYA_YOLU, DOSYA_TIPI, DOSYA_KATEGORISI, DOSYA_BOYUTU, ACIKLAMA, AKTIF) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'), 'Akademik Takvim 2024-2025.pdf', 'uploads/2024/09/akademik_takvim_2024_2025.pdf', 'application/pdf', 'ATTACHMENT', 1048576, '2024-2025 eğitim öğretim yılı akademik takvimi', 'Y');

INSERT INTO DOSYALAR (YUKLEYEN_KULLANICI_ID, DOSYA_ADI, DOSYA_YOLU, DOSYA_TIPI, DOSYA_KATEGORISI, DOSYA_BOYUTU, ACIKLAMA, AKTIF) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'), 'DEÜ Logo Banner.png', 'uploads/2024/09/deu_logo_banner.png', 'image/png', 'BANNER', 256000, 'Duyurular için DEÜ logo banner görseli', 'Y');

-- =============================================
-- 11. EPOSTA GÖNDERIM LOG KAYITLARI
-- =============================================
PROMPT 11. Email gönderim logları ekleniyor...

-- Yangın Tatbikatı duyurusu (ID=4) için başarılı gönderim logları
INSERT INTO EPOSTA_DUYURU_GONDERIM_LOG (DUYURU_ID, ALICI_EMAIL, ALICI_AD_SOYAD, ALICI_KATEGORISI, GONDERIM_DURUMU, GONDERIM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), 'akademik44_seftali@deu.edu.tr', 'Akademik DEBIS Listesi', 'BCC', 'BASARILI', SYSDATE - 1);

INSERT INTO EPOSTA_DUYURU_GONDERIM_LOG (DUYURU_ID, ALICI_EMAIL, ALICI_AD_SOYAD, ALICI_KATEGORISI, GONDERIM_DURUMU, GONDERIM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), 'idari33_armut@deu.edu.tr', 'İdari DEBIS Listesi', 'BCC', 'BASARILI', SYSDATE - 1);

INSERT INTO EPOSTA_DUYURU_GONDERIM_LOG (DUYURU_ID, ALICI_EMAIL, ALICI_AD_SOYAD, ALICI_KATEGORISI, GONDERIM_DURUMU, GONDERIM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), 'rektorluk@deu.edu.tr', 'Rektörlük', 'CC', 'BASARILI', SYSDATE - 1);

-- Test için bir başarısız gönderim örneği
INSERT INTO EPOSTA_DUYURU_GONDERIM_LOG (DUYURU_ID, ALICI_EMAIL, ALICI_AD_SOYAD, ALICI_KATEGORISI, GONDERIM_DURUMU, HATA_MESAJI, GONDERIM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'ACİL: DEÜ Kampüsü Yangın Tatbikatı'), 'bounced_email@invalid.domain', 'Geçersiz Email Test', 'BCC', 'BASARISIZ', 'SMTP Error: Recipient address rejected', SYSDATE - 1);

-- =============================================
-- 12. ÖRNEK LOG KAYITLARI
-- =============================================
PROMPT 12. Örnek sistem logları ekleniyor...

-- Başarılı login logları
INSERT INTO LOG_LOGIN (KULLANICI_ID, KULLANICI_ADI, EMAIL, IP_ADRES, USER_AGENT, GIRIS_TURU, BASARILI, GIRIS_TARIHI) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'), 'admin', 'admin@deu.edu.tr', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36', 'LDAP', 'Y', SYSDATE - 1);

INSERT INTO LOG_LOGIN (KULLANICI_ID, KULLANICI_ADI, EMAIL, IP_ADRES, USER_AGENT, GIRIS_TURU, BASARILI, GIRIS_TARIHI) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'manager'), 'manager', 'manager@deu.edu.tr', '192.168.1.101', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36', 'LDAP', 'Y', SYSDATE - 0.8);

INSERT INTO LOG_LOGIN (KULLANICI_ID, KULLANICI_ADI, EMAIL, IP_ADRES, USER_AGENT, GIRIS_TURU, BASARILI, GIRIS_TARIHI) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'), 'editor', 'editor@deu.edu.tr', '192.168.1.102', 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)', 'LDAP', 'Y', SYSDATE - 0.3);

INSERT INTO LOG_LOGIN (KULLANICI_ID, KULLANICI_ADI, EMAIL, IP_ADRES, USER_AGENT, GIRIS_TURU, BASARILI, GIRIS_TARIHI) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'viewer'), 'viewer', 'viewer@deu.edu.tr', '192.168.1.103', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36', 'LDAP', 'Y', SYSDATE - 0.1);

-- Başarısız login denemesi
INSERT INTO LOG_LOGIN (KULLANICI_ID, KULLANICI_ADI, EMAIL, IP_ADRES, USER_AGENT, GIRIS_TURU, BASARILI, GIRIS_TARIHI, HATA_MESAJI) VALUES
(NULL, 'hacker', 'hacker@gmail.com', '192.168.1.200', 'curl/7.68.0', 'API', 'N', SYSDATE - 2, 'Geçersiz email domain');

-- Sistem logları
INSERT INTO LOG_SISTEM (KULLANICI_ID, LOG_SEVIYE, KATEGORI, ISLEM, DETAY, IP_ADRES, USER_AGENT) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'), 'INFO', 'SYSTEM', 'Database Setup', 'Complete sample data loaded successfully with corrected role system', '192.168.1.100', 'Database Setup Script');

INSERT INTO LOG_SISTEM (KULLANICI_ID, LOG_SEVIYE, KATEGORI, ISLEM, DETAY, IP_ADRES, USER_AGENT) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'admin'), 'INFO', 'USER', 'User Management', 'Test users created with LDAP authentication only', '192.168.1.100', 'Database Setup Script');

INSERT INTO LOG_SISTEM (KULLANICI_ID, LOG_SEVIYE, KATEGORI, ISLEM, DETAY, IP_ADRES, USER_AGENT) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'), 'INFO', 'EMAIL', 'Announcement Created', 'New announcement created: Başarı Kutlaması - Uluslararası Proje', '192.168.1.102', 'Mozilla/5.0');

INSERT INTO LOG_SISTEM (KULLANICI_ID, LOG_SEVIYE, KATEGORI, ISLEM, DETAY, IP_ADRES, USER_AGENT) VALUES
((SELECT ID FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'), 'INFO', 'EMAIL', 'Announcement Sent', 'Yangın Tatbikatı Duyurusu sent to 1248 recipients successfully', '192.168.1.102', 'Mozilla/5.0');

INSERT INTO LOG_SISTEM (KULLANICI_ID, LOG_SEVIYE, KATEGORI, ISLEM, DETAY, IP_ADRES, USER_AGENT) VALUES
(NULL, 'WARN', 'SYSTEM', 'Invalid Domain', 'Login attempt with non-university email: hacker@gmail.com', '192.168.1.200', 'curl/7.68.0');

COMMIT;

PROMPT ========================================
PROMPT COMPLETE SAMPLE DATA BAŞARIYLA YÜKLENDİ
PROMPT ========================================

-- Final kontrol sorguları
PROMPT Yüklenen veri özetleri:
SELECT 'SISTEM_AYARLARI' AS TABLO, COUNT(*) AS KAYIT_SAYISI FROM SISTEM_AYARLARI
UNION ALL
SELECT 'ROLLER', COUNT(*) FROM ROLLER
UNION ALL
SELECT 'KULLANICILAR', COUNT(*) FROM KULLANICILAR
UNION ALL
SELECT 'EPOSTA_GRUPLARI', COUNT(*) FROM EPOSTA_GRUPLARI
UNION ALL
SELECT 'EPOSTA_GRUP_UYELERI', COUNT(*) FROM EPOSTA_GRUP_UYELERI
UNION ALL
SELECT 'EPOSTA_SABLONLARI', COUNT(*) FROM EPOSTA_SABLONLARI
UNION ALL
SELECT 'EPOSTA_DUYURULARI', COUNT(*) FROM EPOSTA_DUYURULARI
UNION ALL
SELECT 'EPOSTA_DUYURU_ALICILARI', COUNT(*) FROM EPOSTA_DUYURU_ALICILARI
UNION ALL
SELECT 'EPOSTA_DUYURU_ZAMANLAMALAR', COUNT(*) FROM EPOSTA_DUYURU_ZAMANLAMALAR
UNION ALL
SELECT 'EPOSTA_DUYURU_GONDERIM_LOG', COUNT(*) FROM EPOSTA_DUYURU_GONDERIM_LOG
UNION ALL
SELECT 'DOSYALAR', COUNT(*) FROM DOSYALAR
UNION ALL
SELECT 'LOG_LOGIN', COUNT(*) FROM LOG_LOGIN
UNION ALL
SELECT 'LOG_SISTEM', COUNT(*) FROM LOG_SISTEM;

PROMPT Test kullanıcı bilgileri (LDAP authentication only):
SELECT 
    k.ID,
    k.KULLANICI_ADI,
    k.AD_SOYAD,
    k.EMAIL,
    r.ROL_KODU,
    k.AKTIF,
    'LDAP Authentication Required' as AUTHENTICATION_TYPE
FROM KULLANICILAR k
JOIN ROLLER r ON k.ROL_ID = r.ID
ORDER BY r.YETKI_SEVIYESI DESC, k.KULLANICI_ADI;

PROMPT Email grupları özeti (yeni model):
SELECT
    GRUP_ADI,
    GRUP_TIPI,
    CASE
        WHEN GRUP_TIPI = 'NORMAL' THEN 'Manuel Üye Yönetimi'
        WHEN GRUP_TIPI = 'STATIK' THEN 'Excel/Dosya Tabanlı - View: ' || COALESCE(VIEW_ADI, 'N/A')
        WHEN GRUP_TIPI = 'DINAMIK' THEN 'SQL Sorgu Tabanlı - View: ' || COALESCE(VIEW_ADI, 'N/A')
        WHEN GRUP_TIPI = 'DEBIS' THEN 'Listeci Email: ' || COALESCE(LISTECI_EMAIL, 'N/A')
        ELSE 'Bilinmeyen Tip'
    END as GRUP_ACIKLAMA,
    AKTIF
FROM EPOSTA_GRUPLARI
ORDER BY GRUP_TIPI, GRUP_ADI;

PROMPT Duyuru durumları özeti:
SELECT 
    DURUM,
    COUNT(*) as ADET,
    AVG(TOPLAM_ALICI_SAYISI) as ORT_ALICI_SAYISI
FROM EPOSTA_DUYURULARI
GROUP BY DURUM
ORDER BY 
    CASE DURUM
        WHEN 'GONDERILDI' THEN 1
        WHEN 'ONAYLANDI' THEN 2
        WHEN 'ONAY_BEKLIYOR' THEN 3
        WHEN 'TASLAK' THEN 4
        ELSE 5
    END;

SELECT '🎉 COMPLETE SAMPLE DATA READY FOR TESTING! 🎉' AS STATUS FROM DUAL;