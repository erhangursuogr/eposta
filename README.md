# 📧 DEÜ E-Posta Yönetim Sistemi

Dokuz Eylül Üniversitesi için toplu e-posta duyuru yönetim ve gönderim sistemi.

## 🎯 Genel Bakış

Üniversite çapında toplu e-posta duyurularının oluşturulması, onaylanması ve zamanlanmış gönderimini yöneten kurumsal web uygulaması.

**İş Akışı**: Duyuru Oluşturma → Alıcı Yönetimi → Onay Süreci → Zamanlama → Gönderim → Raporlama

## ✨ Ana Özellikler

### 🔐 Kimlik Doğrulama
- DEÜ LDAP entegrasyonu
- JWT Bearer Token
- Rol bazlı yetkilendirme (ADMIN, COORDINATOR, MANAGER, EDITOR, VIEWER)
- Token blacklist (logout)

### 📝 Duyuru Yönetimi
- Taslak sistemi ve şablon desteği
- Rich text editor (XSS korumalı)
- Banner/logo ekleme
- Duyuru çoğaltma
- İki aşamalı onay süreci (Kontrolör → Manager)

### 👥 Alıcı Yönetimi
- Manuel alıcı ekleme
- **Grup türleri**: Normal, Statik (Excel/CSV/TXT import), Dinamik (Oracle View), DEBIS
- TO/CC/BCC kategorileri
- BCC-only güvenli gruplar

### 📅 Zamanlama (Hangfire)
- Tek seferlik ve tekrarlayan zamanlamalar
- Hangfire dashboard
- SQLite persistent storage

### 📊 Raporlama & İzleme
- Dashboard istatistikleri
- Detaylı gönderim logları
- Audit trail (LOG_SISTEM)
- Health checks (Database, Disk, Memory, Hangfire)
- Hangfire job failure notifications (ADMIN email)
- Serilog (Console + File + Seq)

### 📁 Dosya Yönetimi
- Ek dosya yükleme (PDF, Office, resim)
- Sistematik klasör yapısı (uploads/announcements/YYYY/MM/)
- Streaming upload (8KB buffer, SHA256 hash)
- Güvenlik: Path traversal koruması, tip/boyut kontrolü, duplicate check

### 🔒 Güvenlik
- Rate limiting
- SQL Injection/XSS/Path Traversal koruması
- CORS whitelisting
- BCrypt password hashing
- HTTPS redirect
- Secure headers

## 🛠 Teknoloji Stack

**Backend**: ASP.NET Core 9.0, Oracle 19c/11g, EF Core, Hangfire, Serilog
**Frontend**: Angular 20.3 (Standalone + Signals), TypeScript 5.7
**Auth**: JWT + LDAP
**Jobs**: Hangfire (SQLite)

## 📦 Sistem Gereksinimleri

**Development**:
- .NET SDK 9.0+
- Node.js 20.x+
- Oracle Client 19.3+

**Production**:
- .NET 9.0 Runtime
- Oracle 19c
- 2GB+ RAM (4GB önerilir)
- 10GB+ disk

## 🚀 Hızlı Başlangıç

### 1. Database Kurulumu
```bash
cd backend/Scripts
sqlplus XXEPOSTA/password@oracle_host:1521/service_name

@02_Sequences.sql
@03_TABLES.sql
@04_Indexes.sql
@05_Triggers.sql
@06_VIEWS.sql
@07_SampleData.sql  # Opsiyonel
```

### 2. Backend
```bash
cd backend
cp .env.example .env
nano .env  # Ayarları düzenle

dotnet restore
dotnet build -c Release
dotnet run --environment Production
```

### 3. Frontend
```bash
cd frontend
npm install
npm start  # Development
npm run build  # Production
```

## ⚙️ Yapılandırma

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Oracle connection string"
  },
  "Jwt": {
    "Key": "32+ karakter secret key",
    "ExpirationInMinutes": 1440
  },
  "EmailSettings": {
    "SmtpServer": "giden.posta.deu.edu.tr",
    "SmtpPort": 25
  }
}
```

Detaylar için `backend/.env.example` dosyasına bakınız.

## 📖 Kullanım

### Kullanıcı Rolleri

| Rol | Yetkiler |
|-----|----------|
| **ADMIN** | Tüm sistem ayarları, kullanıcı yönetimi |
| **COORDINATOR** | İlk onay, şablon yönetimi |
| **MANAGER** | Son onay, grup yönetimi, duyuru oluşturma |
| **EDITOR** | Duyuru oluşturma/düzenleme (kendi) |
| **VIEWER** | Sadece görüntüleme |

### İş Akışı
1. DEÜ LDAP ile giriş
2. Duyuru oluştur (şablon/manuel)
3. Alıcı ekle (grup/manuel)
4. Önizleme
5. Onaya gönder → Kontrolör onayı → Manager onayı
6. Zamanlama (opsiyonel)
7. Gönderim
8. Takip ve raporlama

## 🔌 Önemli Endpoint'ler

- **Swagger**: `http://localhost:5118/openapi/v1.json`
- **Hangfire Dashboard**: `http://localhost:5118/hangfire` (ADMIN)
- **Health Checks**:
  - `GET /api/health/liveness` - Simple alive check (anonymous)
  - `GET /api/health/readiness` - DB connection check (anonymous)
  - `GET /api/health` - Full system health (anonymous)
  - `GET /api/health/detailed` - Extended diagnostics (anonymous)

## 🚢 Docker Deployment

```bash
cd backend

docker build -t deu-eposta-backend:latest .

docker run -d \
  -p 5118:5118 \
  -v /path/to/uploads:/app/uploads \
  -v /path/to/logs:/app/logs \
  --env-file .env \
  --name deu-eposta-backend \
  deu-eposta-backend:latest
```

## ✅ Production Checklist

- [ ] `appsettings.Production.json` ve `.env` güncellenmiş
- [ ] JWT Secret Key değiştirilmiş (64+ karakter)
- [ ] SMTP ayarları test edilmiş
- [ ] CORS allowed origins production domain'i içeriyor
- [ ] HTTPS sertifikası yapılandırılmış
- [ ] Hangfire dashboard authentication aktif
- [ ] Upload/log klasörleri yazılabilir
- [ ] Health checks çalışıyor (`/api/health`)
- [ ] Hangfire job failure notifications test edilmiş
- [ ] Oracle 11g connection string yapılandırılmış
- [ ] Backup stratejisi belirlenmiş
- [ ] Rate limiting ayarları gözden geçirilmiş

## 📝 Lisans

Bu yazılım Dokuz Eylül Üniversitesi tarafından kurum içi kullanım için geliştirilmiştir.

## 📧 İletişim

**Proje Sahibi**: DEÜ Bilgi İşlem Daire Başkanlığı
**Teknik Destek**: destek@deu.edu.tr

---

**Versiyon**: 1.0 | **Durum**: Production Ready ✅
