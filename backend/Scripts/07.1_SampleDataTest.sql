-- =============================================
-- 1.1. EMAIL İMZA TANIMLARI (SIGNATURE HTML)
-- =============================================
-- Email sonuna eklenecek HTML imzalar - AYAR_DEGER içinde HTML kodu
-- İmzasız kategori örneği (boş string)
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_IMZA', 'GENEL_DUYURU_IMZASIZ', '','Genel Duyurular - ( İmza Yok )','N', 'Y');

SET DEFINE OFF;

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF, GOREV_YERI) VALUES
('EMAIL_IMZA', 'GENEL_DUYURU_IMZALI', '
<style>
/* Mobil Cihazlar için Kurallar */
@media only screen and (max-width: 600px) {
    .mobile-hidden { display: none !important; width: 0 !important; height: 0 !important; overflow: hidden !important; line-height: 0 !important; font-size: 0 !important; }
    .mobile-full-width { display: block !important; width: 100% !important; text-align: center !important; padding: 0 !important; }
    .mobile-padding { padding-top: 10px !important; }
}
</style>

<div style="font-size: 1px; line-height: 50px; height: 50px; display: block;">&nbsp;</div>

<table width="100%" cellpadding="0" cellspacing="0" border="0" 
       style="width: 100% !important; min-width: 100%; border-top: 1px solid #e1e1e1; padding-top: 20px; font-family: Arial, Helvetica, sans-serif; border-collapse: separate; mso-table-lspace:0pt; mso-table-rspace:0pt;">
    <tr>
        <td valign="middle" class="mobile-hidden" style="width: 100px; padding: 0; margin: 0; line-height: 0; font-size: 0; border: 0;">
            <img src="https://kurumsalduyuru.deu.edu.tr/assets/deu-logo-mavi.png" alt="DEÜ Logo" width="75" height="auto" style="display:block; border:0; outline: none; text-decoration: none; -ms-interpolation-mode: bicubic;">
        </td>
        
        <td valign="middle" class="mobile-full-width" style="text-align: center; color: #333333; font-size: 13px; line-height: 1.5; padding: 0 10px;">
            <strong style="font-size: 15px; color: #1a4f8b;">Dokuz Eylül Üniversitesi Rektörlüğü</strong><br>
            <span style="font-size: 13px;">
                <a href="https://www.deu.edu.tr" style="color:#1a4f8b; text-decoration:none;">www.deu.edu.tr</a> <span style="color:#cccccc;">|</span> <a href="tel:+902324121212" style="color:#1a4f8b; text-decoration:none;">+90 (232) 412 12 12</a>
            </span>
            <p class="mobile-padding" style="margin: 5px 0 0 0; font-size: 11px; color: #777; line-height: 1.4;">Cumhuriyet Bulvarı No: 144, 35210 Alsancak / İZMİR</p>
        </td>
        
        <td valign="middle" class="mobile-hidden" style="width: 100px; text-align: right; padding: 0; margin: 0; line-height: 0; font-size: 0; border: 0;">
            <img src="https://kurumsalduyuru.deu.edu.tr/assets/deulogo-tam akreditasyon.png" alt="DEÜ Sağ Logo" width="80" height="auto" style="display:block; border:0; margin-left: auto; outline: none; text-decoration: none; -ms-interpolation-mode: bicubic;">
        </td>
    </tr>
    
    <tr>
        <td colspan="3" style="padding-top: 20px;">
            <div style="border-top: 1px dotted #dddddd; padding-top: 10px;">
                <p style="margin:0; font-size:10px; color:#888; line-height:1.4; text-align:justify; font-family:Arial, Helvetica, sans-serif;">
                    <strong>Yasal Uyarı:</strong> Bu e-posta ve ekleri, gönderilen kişiye / kuruma özeldir. Eğer bu mesajın alıcısı değilseniz, lütfen göndericiyi bilgilendiriniz ve mesajı sisteminizden siliniz.<br><br>
                    <span style="color:#27ae60;">🌳 Lütfen gerekmediği sürece bu e-postayı yazdırmayınız.</span>
                </p>
            </div>
        </td>
    </tr>
</table>
', 'Genel Duyurular - imzalı', 'N', 'Y',0);

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF, GOREV_YERI) VALUES
('EMAIL_IMZA', 'REKTOR', '<div style="font-size: 1px; line-height: 50px; height: 50px; display: block;">&nbsp;</div>
<div style="font-size: 1px; line-height: 50px; height: 50px; display: block;">&nbsp;</div>           
<div style="font-family: Arial, sans-serif; font-size: 12px; color: #333; margin-top: 20px; padding-top: 10px; border-top: 1px solid #ccc;">
  <p style="margin: 5px 0;"><strong>Prof.Dr. Bayram YILMAZ</strong></p>
  <p style="margin: 5px 0;"><strong>Rektör</strong></p>
</div>', 'Rektör Duyuruları imzalı', 'N', 'Y',0);

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF, GOREV_YERI) VALUES
('EMAIL_IMZA', 'BID', '<div style="font-size: 1px; line-height: 50px; height: 50px; display: block;">&nbsp;</div>
<div style="font-size: 1px; line-height: 50px; height: 50px; display: block;">&nbsp;</div>           
<div style="font-family: Arial, sans-serif; font-size: 12px; color: #333; margin-top: 20px; padding-top: 10px; border-top: 1px solid #ccc;">
<img src="https://kurumsalduyuru.deu.edu.tr/assets/bid_logo.png" alt="DEÜ Logo" width="200" height="auto" style="display:block; border:0; outline: none; text-decoration: none; -ms-interpolation-mode: bicubic;">
</div>', 'Bilgi İşlem Duyuruları imzalı', 'N', 'Y',0);


-- Mühendislik Fakültesi
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF, GOREV_YERI) VALUES
('EMAIL_IMZA', 'MUHENDISLIK',
'<div style="font-size: 1px; line-height: 50px; height: 50px; display: block;">&nbsp;</div>           
<div style="font-family: Arial, sans-serif; font-size: 12px; color: #333; margin-top: 20px; padding-top: 10px; border-top: 1px solid #ccc;">
  <p style="margin: 5px 0;"><strong>Dokuz Eylül Üniversitesi</strong></p>
  <p style="margin: 5px 0;"><strong>Mühendislik Fakültesi Dekanlığı</strong></p>
  <p style="margin: 5px 0;">Tel: (232) 301 7211 - 12,13 | muhendislik@deu.edu.tr</p>
</div>',
'Mühendislik Fakültesi Duyuru imzası',
'N', 'Y',500);

-- Tıp Fakültesi
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF, GOREV_YERI) VALUES
('EMAIL_IMZA', 'TIP',
'<div style="font-size: 1px; line-height: 50px; height: 50px; display: block;">&nbsp;</div>           
<div style="font-family: Arial, sans-serif; font-size: 12px; color: #333; margin-top: 20px; padding-top: 10px; border-top: 1px solid #ccc;">
  <p style="margin: 5px 0;"><strong>Dokuz Eylül Üniversitesi</strong></p>
  <p style="margin: 5px 0;"><strong>Tıp Fakültesi Dekanlığıı</strong></p>
  <p style="margin: 5px 0;">Tel: (232) 412 2222 | tip@deu.edu.tr</p>
</div>',
'Tıp Fakültesi Duyuru imzası',
'N', 'Y',100);


-- =============================================
-- 1.2. EMAIL GÖNDERİCİ KATEGORİLERİ
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_REKTOR', 'FROM_EMAIL', 'bayram.yilmaz@deu.edu.tr', 'Rektör duyuruları gönderici email adresi', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_REKTOR', 'FROM_NAME', 'Rektör', 'Rektör adına gönderilecek duyurular', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_REKTOR', 'SMTP_USERNAME', '', 'SMTP kullanıcı adı (yarın doldurulacak)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_REKTOR', 'SMTP_PASSWORD', '', 'SMTP şifresi (yarın doldurulacak)', 'Y', 'Y');

-- =============================================

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_BID', 'FROM_EMAIL', 'bid@deu.edu.tr', 'Bilgi İşlem duyuruları gönderici email adresi', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_BID', 'FROM_NAME', 'DEÜ Bilgi İşlem', 'Bilgi İşlem duyuruları gönderici görünen adı', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_BID', 'SMTP_USERNAME', '', 'SMTP kullanıcı adı (yarın doldurulacak)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_BID', 'SMTP_PASSWORD', '', 'SMTP şifresi (yarın doldurulacak)', 'Y', 'Y');

-- =============================================

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_DUYURU', 'FROM_EMAIL', 'duyuru@deu.edu.tr', 'Genel duyuruları gönderici email adresi', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_DUYURU', 'FROM_NAME', 'Genel Duyuru', 'Genel duyuruları gönderici görünen adı', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_DUYURU', 'SMTP_USERNAME', '', 'SMTP kullanıcı adı (boş = DefaultCredentials)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('EMAIL_DUYURU', 'SMTP_PASSWORD', '', 'SMTP şifresi (boş = DefaultCredentials)', 'Y', 'Y');

-- =============================================
-- 1.3. SİSTEM - BİLDİRİM KATEGORİSİ EMAIL AYARLARI
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
-- 1.4. ORTAK SMTP AYARLARI (TÜM KATEGORİLER)
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
-- 1.5. GENEL SİSTEM AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DOSYA', 'MAX_DOSYA_BOYUTU_MB', '10', 'Maksimum dosya boyutu (MB)', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DOSYA', 'IZIN_VERILEN_TIPLER', 'jpg,jpeg,png,gif,pdf,doc,docx,xls,xlsx,ppt,pptx,txt,zip,rar', 'İzin verilen dosya uzantıları', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DOSYA', 'DEPOLAMA_KLASORU', 'uploads/', 'Dosyaların depolanacağı klasör', 'N', 'Y');

-- =============================================
-- 1.6. DEBIS AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DEBIS', 'SERVICE_KEY', 'ndlar75aGaQ345a5s6s1a0?63dTTf', 'DEBIS SOAP servis anahtarı - LdapService.cs içinde kullanılıyor (hardcoded yerine)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DEBIS', 'RSA_PUBLIC_KEY', '<RSAKeyValue><Modulus>xOf++aLdynlRjbXGw4p3g04XrVKnSFqVXdwqGQJUOgOVcpOvFZ6oKp+oX7+8RgkLWRjHX+LgGqCzOedrv5FxkwOgb2OJ+rP5YEhRZqtFwCgRrHdYWR7T4p9KdjY36FJ7nYwp+jnZEj4tUhyOt4C5J4L6gT4Zz2z6NdLgHjJ0xBzJ4WYGp9pCx5ZfBJh+tZ0Y6ItFKgGeLOW6dM4sJyqOt+Tl6HdYZZE3Jj6cZWy5c1g3z3JEpx7+3XZ4cY3Y4g3L6EfZFGmCj2T4BzJ1YKgF1zR1L6T2+O0Y7XzJ7hGcO6qKO6+1Y8kZ0Y3zZ0G7CZ1J4pXgKgz0</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>', 'RSA public key - Email/password şifreleme için (appsettings.json yerine)', 'Y', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('DEBIS', 'SOAP_ENDPOINT', 'http://debis.deu.edu.tr/DebisHSService.asmx', 'DEBIS SOAP web servis endpoint URL', 'N', 'Y');

-- =============================================
-- 1.7. CACHE AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('CACHE', 'EMAIL_CONFIG_MINUTES', '30', 'Email configuration cache süresi (dakika) - EmailCategoryService.cs içinde kullanılıyor', 'N', 'Y');

INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('CACHE', 'USER_DATA_MINUTES', '10', 'Kullanıcı verisi cache süresi (dakika) - AuthService.cs içinde kullanılabilir', 'N', 'Y');

-- =============================================
-- 1.8. DOMAIN AYARLARI
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('SECURITY_DOMAIN', 'ALLOWED_EMAIL_DOMAINS', 'deu.edu.tr', 'İzin verilen email domain listesi (virgülle ayrılmış) - Email validasyonunda kullanılır', 'N', 'Y');

-- =============================================
-- 1.9. OTURUM AYARLARI - NORMAL / SSO
-- =============================================
INSERT INTO SISTEM_AYARLARI (AYAR_KATEGORI, AYAR_ANAHTAR, AYAR_DEGER, AYAR_ACIKLAMA, GIZLI, AKTIF) VALUES
('AUTH', 'MODE', '1', 'Authentication mode: 0=Normal LDAP, 1=Keycloak SSO', 'Y', 'Y');

-- =============================================
-- 2. ROLLER
-- =============================================
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

-- Fake kullanıcılar - Sadece örnek duyurular için
-- Gerçek kullanıcılar LDAP ile giriş yapacak ve otomatik oluşacak
INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('erhan', 'Admin Kullanıcı', 'erhan@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'ADMIN'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('erhan.gursu', 'Editör Kullanıcı', 'erhan.gursu@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'EDITOR'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('yasar.dereli', 'Admin Kullanıcı', 'yasar.dereli@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'ADMIN'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('nezih', 'Admin Kullanıcı', 'nezih@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'ADMIN'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('sami', 'Manager Kullanıcı', 'sami@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'MANAGER'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('rukiye', 'Koordinatör Kullanıcı', 'rukiye@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'COORDINATOR'), 'Y', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('derya.top', 'Editör Kullanıcı', 'derya.top@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'EDITOR'), 'N', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('oguz.gumuscap', 'Koordinatör Kullanıcı', 'oguz.gumuscap@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'COORDINATOR'), 'N', SYSDATE);

INSERT INTO KULLANICILAR (KULLANICI_ADI, AD_SOYAD, EMAIL, ROL_ID, AKTIF, OLUSTURMA_TARIHI) VALUES
('emine.altinkaynakabda', 'Manager Kullanıcı', 'emine.altinkaynakabda@deu.edu.tr', (SELECT ID FROM ROLLER WHERE ROL_KODU = 'MANAGER'), 'N', SYSDATE);

-- =============================================
-- 4. EPOSTA GRUPLARI - 4 TİP GRUP SİSTEMİ (YENİLENMİŞ MODEL)
-- =============================================

-- NORMAL Grup - Manuel üye eklenen gruplar, TO/CC/BCC desteklenir
INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, AKTIF) VALUES
('Test Bilgi İşlem Grubu', 'Manuel eklenen üyelerle duyuru gönderimi', 'MANUEL', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, AKTIF) VALUES
('Test Kurumsal İletişim Grubu', 'Manuel eklenen üyelerle duyuru gönderimi', 'MANUEL', 'Y');

INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, LISTECI_EMAIL, AKTIF) VALUES
('Test DEBIS Grubu', 'DEBIS entegrasyonu - Test Grubu', 'DEBIS', 'tum_erhan2025@kordon.adm.deu.edu.tr', 'Y');

-- =============================================
-- 5. EPOSTA GRUP UYELERI
-- =============================================

-- Normal Grup (ID=1) üyeleri - manuel olarak eklenmiş
INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Bilgi İşlem Grubu'), 'erhan@deu.edu.tr', 'Erhan Gürsu 1', 'Test Departmanı', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Bilgi İşlem Grubu'), 'erhan.gursu@deu.edu.tr', 'Erhan Gürsu 2', 'Test Departmanı', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Bilgi İşlem Grubu'), 'rukiye@deu.edu.tr', 'Test Kullanıcı 3', 'Test Departmanı', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Bilgi İşlem Grubu'), 'sami@deu.edu.tr', 'Test Kullanıcı 4', 'Test Departmanı', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'oguz.gumuscap@deu.edu.tr', 'oguz.gumuscap', 'Test Departmanı 1', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'ozan.akgun@deu.edu.tr', 'ozan.akgun', 'Test Departmanı 1', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'arifenes.durak@deu.edu.tr', 'arifenes.durak', 'Test Departmanı 1', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'merve.karagoz@deu.edu.tr', 'merve.karagoz', 'Test Departmanı 1', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'melih.seymen@deu.edu.tr', 'melih.seymen', 'Test Departmanı 1', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'alpkemal.muezzinoglu@deu.edu.tr', 'alpkemal.muezzinoglu', 'Test Departmanı 1', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'ece.morgul@deu.edu.tr', 'aece.morgul', 'Test Departmanı 1', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'emine.altinkaynakabda@deu.edu.tr', 'emine.altinkaynakabda', 'Test Departmanı 1', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'ederya.top@deu.edu.tr', 'derya.top', 'Test Departmanı 1', 'AKTIF');

INSERT INTO EPOSTA_GRUP_UYELERI (GRUP_ID, EMAIL, AD_SOYAD, DEPARTMAN, DURUM) VALUES
((SELECT ID FROM EPOSTA_GRUPLARI WHERE GRUP_ADI = 'Test Kurumsal İletişim Grubu'), 'erhan.gursu@deu.edu.tr', 'erhan.gursu', 'Test Departmanı 1', 'AKTIF');

-- =============================================
-- 6. EPOSTA SABLON KATEGORİLERİ
-- =============================================

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

INSERT INTO EPOSTA_SABLONLARI (KATEGORI_ID, SABLON_ADI, KONU_SABLONU, ICERIK_SABLONU, VARSAYILAN, AKTIF) VALUES
(1,'Personel / Akademisyen için Şablon', 'Prof. Dr. (Ad Soyad) – Vefat ve Cenaze Töreni Bilgisi', 
'<p>Değerli Mensuplarımız,<br><br>
Üniversitemiz (birim adı) öğretim üyesi Prof. Dr. (Ad Soyad)’ın vefatını derin bir üzüntüyle öğrenmiş bulunmaktayız. Merhuma/merhumeye Allah’tan rahmet; ailesine, Üniversitemize ve çalışma arkadaşlarımıza sabır diliyoruz.<br><br>
Cenaze törenine ilişkin bilgiler aşağıda yer almaktadır:<br><br>
Tören Yeri: (Cami / Mezarlık / Salon adı vb.)<br>
Tarih: (Gün, Ay, Yıl)<br>
Saat: (Varsa namaz veya defin saati)<br>
Defin Yeri: (Mezarlık adı – il/ilçe)<br>
</p>','N', 'Y');

-- =============================================
-- 7. ÖRNEK DUYURULAR - FARKLI DURUMLARDA
-- =============================================

-- 1. TASLAK durumunda duyuru (PERSONEL kategorisi) - EMAIL içerik
INSERT INTO EPOSTA_DUYURULARI (KONU, ICERIK, ACIKLAMA, ICERIK_TIPI, DUYURU_KATEGORISI, GONDERICI_KATEGORI, SABLON_ID, OLUSTURAN_KULLANICI_ID, DURUM, TOPLAM_ALICI_SAYISI) VALUES
('DEÜ 2025-2026 Güz Dönemi Akademik Takvimi',
'<h2>Sevgili DEÜ Ailesi,</h2>
<p>2025-2026 Eğitim-Öğretim yılı Güz dönemi <strong>15 Eylül 2025 Pazartesi</strong> günü başlayacaktır.</p>

<h3>📅 Önemli Tarihler:</h3>
<ul>
<li><strong>Ders Kayıtları:</strong> 8-12 Eylül 2025</li>
<li><strong>Dersler Başlangıcı:</strong> 15 Eylül 2025 Pazartesi</li>
<li><strong>Add/Drop İşlemleri:</strong> 15-20 Eylül 2025</li>
<li><strong>Ara Sınav Haftası:</strong> 4-8 Kasım 2025</li>
<li><strong>Final Sınavları:</strong> 16-27 Aralık 2025</li>
</ul>

<h3>📝 Dikkat Edilecek Hususlar:</h3>
<p>• Ders kayıtları öğrenci bilgi sistemi üzerinden yapılacaktır.<br>
• Add/Drop işlemleri için dekanlık onayı gereklidir.<br>
• Devamsızlık durumu takip edilecektir.</p>

<p>Tüm öğrencilerimize başarılı bir eğitim-öğretim yılı dileriz.</p>

<p><strong>Rektörlük</strong><br>
<em>Dokuz Eylül Üniversitesi</em></p>',
'2025-2026 Güz dönemi akademik takvimi ve önemli tarihlerin duyurulması',
'EMAIL',
'REKTOR',
'EMAIL_REKTOR',
(SELECT MIN(ID) FROM EPOSTA_SABLONLARI WHERE VARSAYILAN = 'Y'),
(SELECT MIN(ID) FROM KULLANICILAR WHERE AD_SOYAD = 'Editör Kullanıcı'),
'TASLAK', 0);

-- Duyuru 1: TASLAK (sadece oluşturma hareketi)
INSERT INTO EPOSTA_DUYURU_HAREKETLERI (DUYURU_ID, YENI_DURUM, ISLEM_TIPI, KULLANICI_ID, ACIKLAMA, ISLEM_TARIHI) VALUES
((SELECT ID FROM EPOSTA_DUYURULARI WHERE KONU = 'DEÜ 2025-2026 Güz Dönemi Akademik Takvimi'), 'TASLAK', 'OLUSTURMA', (SELECT MIN(ID) FROM KULLANICILAR WHERE KULLANICI_ADI = 'editor'),
'Yeni duyuru taslağı oluşturuldu', SYSDATE - 5);

COMMIT;
