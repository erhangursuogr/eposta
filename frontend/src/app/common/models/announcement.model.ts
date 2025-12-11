// Announcement Models

export interface Announcement {
  id: number;
  konu: string;
  icerik: string;
  icerikTipi: string; // EMAIL veya SOSYAL_MEDYA
  duyuruKategorisi: string; // İmza kategorisi (EMAIL_IMZA tablosundan)
  gondericiKategori: string; // SMTP gönderici kategorisi (EMAIL_DUYURU, EMAIL_REKTOR, vb.)
  durum: AnnouncementStatus;
  olusturanKullaniciId: number;
  olusturanKullaniciAdi: string;
  olusturanAdSoyad?: string; // View'dan gelen field
  olusturmaTarihi: string;
  guncellemeTarihi?: string;
  gonderimTarihi?: string;
  gercekGonderimTarihi?: string; // View'dan gelen field
  zamanlanmisTarih?: string;

  // İki aşamalı onay bilgileri
  ilkOnaylayanKullaniciId?: number;  // Koordinatör
  ilkOnaylayanKullaniciAdi?: string;
  ilkOnaylayanAdSoyad?: string; // View'dan gelen field
  sonOnaylayanKullaniciId?: number;  // Manager
  sonOnaylayanKullaniciAdi?: string;
  sonOnaylayanAdSoyad?: string; // View'dan gelen field

  // Backward compatibility (deprecated)
  onaylayanKullaniciId?: number;
  onaylayanKullaniciAdi?: string;
  onayTarihi?: string;
  onayNotu?: string;

  // İşlem bilgileri (Hareket tablosundan)
  islemTarihi?: string;  // Gerçek onay/red tarihi
  islemNotu?: string;    // Onay/Red notu
  islemYapan?: string;   // İşlem yapan kişi adı

  // Red bilgileri (deprecated - tek red notu)
  redNedeni?: string;
  redTarihi?: string;

  // Çift aşamalı red bilgileri
  koordinatorRedNotu?: string;
  koordinatorRedTarihi?: string;
  managerRedNotu?: string;
  managerRedTarihi?: string;

  toplamAliciSayisi: number;
  basariliGonderimSayisi?: number;
  basarisizGonderimSayisi?: number;
  basariYuzdesi?: number; // View'dan gelen field
  dosyaVarMi: boolean;
  dosyaSayisi: number;
  tekrarSikligi?: RepeatFrequency;
  tekrarBitisTarihi?: string;
  aktif: boolean;
  zamanlanmisMi?: boolean;
  zamanlamaSayisi?: number;
  aciklama?: string;

  // Düzenleme için alıcı listeleri
  grupIdList?: number[];
  aliciEmailList?: string[];
}

export enum AnnouncementStatus {
  TASLAK = 'TASLAK',
  ILK_ONAY_BEKLIYOR = 'ILK_ONAY_BEKLIYOR',
  SON_ONAY_BEKLIYOR = 'SON_ONAY_BEKLIYOR',
  ONAY_BEKLIYOR = 'ONAY_BEKLIYOR', // Backward compatibility
  REDDEDILDI = 'REDDEDILDI',
  ONAYLANDI = 'ONAYLANDI',
  GONDERILIYOR = 'GONDERILIYOR',
  GONDERILDI = 'GONDERILDI',
  IPTAL = 'IPTAL'
}

export enum RepeatFrequency {
  NONE = 'NONE',
  DAILY = 'DAILY',
  WEEKLY = 'WEEKLY',
  MONTHLY = 'MONTHLY'
}

export interface CreateAnnouncementRequest {
  konu: string;
  icerik: string;
  aciklama?: string;
  zamanlanmisTarih?: string;
  tekrarSikligi?: RepeatFrequency;
  tekrarBitisTarihi?: string;
  grupIdList: number[];
  aliciEmailList: string[];
  dosyaIdList?: number[];
}

export interface UpdateAnnouncementRequest {
  id: number;
  konu: string;
  icerik: string;
  aciklama?: string;
  zamanlanmisTarih?: string;
  tekrarSikligi?: RepeatFrequency;
  tekrarBitisTarihi?: string;
  grupIdList: number[];
  aliciEmailList: string[];
  dosyaIdList?: number[];
}

export interface AnnouncementListParams {
  page?: number;
  pageSize?: number;
  searchQuery?: string;
  searchTerm?: string; // BACKEND FILTRELEME (Gemini Audit Fix): Arama terimi backend'e gönderiliyor
  durum?: AnnouncementStatus | '';
  baslangicTarihi?: string;
  bitisTarihi?: string;
  startDate?: Date; // BACKEND FILTRELEME: Tarih aralığı backend'e gönderiliyor
  endDate?: Date; // BACKEND FILTRELEME: Tarih aralığı backend'e gönderiliyor
  olusturanKullaniciId?: number;
  onlyMine?: boolean;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface AnnouncementRecipient {
  id: number;
  duyuruId: number;
  aliciEmail: string;
  aliciAdi: string;
  gonderimDurumu: DeliveryStatus;
  gonderimTarihi?: string;
  hataMesaji?: string;
  okunduMu: boolean;
  okunmaTarihi?: string;
}

export enum DeliveryStatus {
  BEKLEMEDE = 'BEKLEMEDE',
  GONDERILDI = 'GONDERILDI',
  BASARISIZ = 'BASARISIZ'
}

export interface AnnouncementFile {
  id: number;
  dosyaAdi: string;
  dosyaYolu: string;
  dosyaBoyutu: number;
  mimeType: string;
  yuklemeTarihi: string;
  yukleyenKullaniciId: number;
  yukleyenKullaniciAdi: string;
}

export interface AnnouncementApprovalRequest {
  duyuruId: number;
  onayNotu?: string;
}

export interface AnnouncementRejectionRequest {
  duyuruId: number;
  redNedeni: string;
}

export interface AnnouncementMovement {
  id: number;
  duyuruId: number;
  oncekiDurum?: string;
  yeniDurum: string;
  islemTipi: string;
  kullaniciId?: number;
  kullaniciAdi?: string;
  aciklama?: string;
  secilenOnaylayiciId?: number;
  secilenOnaylayiciAdi?: string;
  islemTarihi: string;
}

// Helper functions
export function getStatusColor(status: AnnouncementStatus): string {
  switch (status) {
    case AnnouncementStatus.TASLAK:
      return 'gray';
    case AnnouncementStatus.ILK_ONAY_BEKLIYOR:
      return 'orange';
    case AnnouncementStatus.SON_ONAY_BEKLIYOR:
      return 'orange';
    case AnnouncementStatus.ONAY_BEKLIYOR:
      return 'orange';
    case AnnouncementStatus.REDDEDILDI:
      return 'red';
    case AnnouncementStatus.ONAYLANDI:
      return 'blue';
    case AnnouncementStatus.GONDERILDI:
      return 'green';
    case AnnouncementStatus.IPTAL:
      return 'dark-gray';
    default:
      return 'gray';
  }
}

export function getStatusLabel(status: AnnouncementStatus): string {
  switch (status) {
    case AnnouncementStatus.TASLAK:
      return 'Taslak';
    case AnnouncementStatus.ILK_ONAY_BEKLIYOR:
      return 'İlk Onay Bekliyor';
    case AnnouncementStatus.SON_ONAY_BEKLIYOR:
      return 'Son Onay Bekliyor';
    case AnnouncementStatus.ONAY_BEKLIYOR:
      return 'Onay Bekliyor';
    case AnnouncementStatus.REDDEDILDI:
      return 'Reddedildi';
    case AnnouncementStatus.ONAYLANDI:
      return 'Onaylandı';
    case AnnouncementStatus.GONDERILDI:
      return 'Gönderildi';
    case AnnouncementStatus.IPTAL:
      return 'İptal';
    default:
      return status;
  }
}

export function getStatusIcon(status: AnnouncementStatus): string {
  switch (status) {
    case AnnouncementStatus.TASLAK:
      return 'edit_note';
    case AnnouncementStatus.ILK_ONAY_BEKLIYOR:
      return 'pending';
    case AnnouncementStatus.SON_ONAY_BEKLIYOR:
      return 'pending_actions';
    case AnnouncementStatus.ONAY_BEKLIYOR:
      return 'pending';
    case AnnouncementStatus.REDDEDILDI:
      return 'cancel';
    case AnnouncementStatus.ONAYLANDI:
      return 'check_circle';
    case AnnouncementStatus.GONDERILDI:
      return 'send';
    case AnnouncementStatus.IPTAL:
      return 'block';
    default:
      return 'info';
  }
}

export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
}
