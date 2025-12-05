# DEÜ Duyuru Yönetim Sistemi - Database Kurulum Kılavuzu

## 📋 Kurulum Sırası

Database scriptlerini **mutlaka** aşağıdaki sırayla çalıştırın:

### 1️⃣ Temizlik (Opsiyonel - Yeniden Kurulum İçin)
```bash
sqlplus username/password@database @01_DropAll.sql
```
**⚠️ DİKKAT:** Bu script tüm verileri kalıcı olarak siler!

### 2️⃣ Sequence'lar
```bash
sqlplus username/password@database @02_SEQUENCES.sql
```
Tüm tablolar için otomatik ID üretimi

### 3️⃣ Tablolar
```bash
sqlplus username/password@database @03_TABLES.sql
```
Ana database yapısı (tablolar, constraints, indexes)

### 4️⃣ View'lar (YENİ!)
```bash
sqlplus username/password@database @04_VIEWS.sql
```
Dinamik grup sistemleri için view'lar:
- `V_EMAIL_AKADEMIK` - Akademik personel
- `V_EMAIL_IDARI` - İdari personel
- `V_PERSONEL_AKTIF` - Tüm aktif personel
- `V_OGRENCI_LISANS` - Lisans öğrencileri
- `V_OGRENCI_LISANSUSTU` - Lisansüstü öğrenciler
- `V_EMAIL_GENEL` - Genel email listesi
- `V_EMAIL_LISTE` - Basit email listesi

### 5️⃣ Trigger'lar ve Function'lar
```bash
sqlplus username/password@database @05_TRIGGERS.sql
sqlplus username/password@database @06_FUNCTIONS.sql
```
İş mantığı ve audit mekanizmaları

### 6️⃣ Sample Data
```bash
sqlplus username/password@database @07_SampleData.sql
```
Test verisi (roller, kullanıcılar, gruplar, şablonlar, örnek duyurular)

---

## 🚀 Tek Seferde Kurulum

**Production'da kullanma! Sadece development için:**

```bash
sqlplus username/password@database << EOF
@01_DropAll.sql
@02_SEQUENCES.sql
@03_TABLES.sql
@04_VIEWS.sql
@05_TRIGGERS.sql
@06_FUNCTIONS.sql
@07_SampleData.sql
EOF
```

---

## ✅ Kurulum Doğrulama

Kurulum sonrası kontrol sorguları:

### Tablo Sayıları
```sql
SELECT table_name, num_rows
FROM user_tables
WHERE table_name IN (
    'ROLLER', 'KULLANICILAR', 'EPOSTA_GRUPLARI',
    'EPOSTA_GRUP_UYELERI', 'EPOSTA_SABLONLARI',
    'EPOSTA_DUYURULARI', 'EPOSTA_DUYURU_ALICILARI',
    'DOSYALAR', 'SISTEM_AYARLARI'
)
ORDER BY table_name;
```

### View'lar
```sql
SELECT view_name
FROM user_views
WHERE view_name LIKE 'V_%'
ORDER BY view_name;
```

### Sample Data Kontrol
```sql
-- Kullanıcılar (4 adet olmalı)
SELECT COUNT(*) AS kullanici_sayisi FROM KULLANICILAR;

-- Gruplar (15 adet olmalı: 4 NORMAL, 2 STATİK, 5 DİNAMİK, 4 DEBIS)
SELECT GRUP_TIPI, COUNT(*) AS grup_sayisi
FROM EPOSTA_GRUPLARI
GROUP BY GRUP_TIPI;

-- Duyurular (4 adet olmalı)
SELECT DURUM, COUNT(*) AS duyuru_sayisi
FROM EPOSTA_DUYURULARI
GROUP BY DURUM;

-- Dinamik grup test
SELECT * FROM V_EMAIL_AKADEMIK;
```

---

## 🔧 Sorun Giderme

### Hata: "view does not exist"
**Çözüm:** `04_VIEWS.sql` script'ini çalıştırmayı unutmuşsunuz
```bash
sqlplus username/password@database @04_VIEWS.sql
```

### Hata: "constraint violated"
**Çözüm:** Script sırası yanlış. Önce `01_DropAll.sql` ile temizleyin, sonra sırayla tekrar kurun.

### Hata: "sequence does not exist"
**Çözüm:** `02_SEQUENCES.sql` çalıştırılmamış.

### View'lar boş döndürüyor
**Çözüm:** Sample data yüklenmemiş. `07_SampleData.sql` çalıştırın.

---

## 📊 Dinamik Grup Test Senaryosu

1. **Dinamik grup oluşturma:**
```sql
INSERT INTO EPOSTA_GRUPLARI (GRUP_ADI, ACIKLAMA, GRUP_TIPI, VIEW_ADI, AKTIF)
VALUES ('Test Akademik', 'Test dinamik grup', 'DINAMIK', 'V_EMAIL_AKADEMIK', 'Y');
```

2. **Duyuru oluşturma ve gruba gönderme:**
```sql
-- API üzerinden test et
POST /api/announcements
{
  "Konu": "Test Duyuru",
  "konu": "Dinamik Grup Testi",
  "icerik": "<p>Test içerik</p>",
  "duyuruKategorisi": "PERSONEL"
}

-- Alıcı ekle
POST /api/announcements/{id}/recipients/group
{
  "grupId": {dinamik_grup_id}
}

-- Preview al
GET /api/announcements/{id}/preview
```

3. **Beklenen sonuç:**
- Preview'da V_EMAIL_AKADEMIK view'daki tüm kullanıcılar görünmeli
- MemberCount doğru olmalı
- Email listesi dolu olmalı

---

## 🎯 Önemli Notlar

1. **View İzinleri:** Production'da uygun kullanıcıya SELECT yetkisi verilmeli:
```sql
GRANT SELECT ON V_EMAIL_AKADEMIK TO eposta_app_user;
GRANT SELECT ON V_EMAIL_IDARI TO eposta_app_user;
-- ... diğer view'lar
```

2. **View Güvenliği:** `EmailService.cs` içinde whitelist var, yeni view eklerseniz güncelleyin:
```csharp
var allowedViews = new[]
{
    "V_EMAIL_AKADEMIK",
    "V_EMAIL_IDARI",
    // ... yeni view'lar buraya
};
```

3. **Filter Condition Güvenliği:** SQL injection önlemi için filter'lar sanitize ediliyor.

---

## 📞 Destek

Sorun yaşarsanız:
1. Log dosyalarını kontrol edin (`sqlplus` çıktısı)
2. View'ların doğru oluşturulduğunu kontrol edin
3. Application log'larını inceleyin (EmailService log'ları)

---

**Son Güncelleme:** 2025-01-01
**Versiyon:** 2.0 (Dinamik Grup Sistemi Eklendi)
