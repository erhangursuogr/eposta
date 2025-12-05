export interface LoginLog {
  id: number;
  kullaniciId?: number;
  kullaniciAdi?: string;
  email?: string;
  ipAdres?: string;
  userAgent?: string;
  girisTuru: string;
  basarili: boolean;
  hataMesaji?: string;
  girisTarihi: string;
}

export interface SystemLog {
  id: number;
  kullaniciId?: number;
  kullaniciAdi?: string;
  logSeviye: string;
  kategori: string;
  islem: string;
  detay?: string;
  ipAdres?: string;
  userAgent?: string;
  logTarihi: string;
}

export interface EmailLog {
  id: number;
  duyuruId: number;
  duyuruKonu?: string;
  aliciEmail: string;
  aliciAdSoyad?: string;
  aliciKategorisi: string;
  gonderimBasarili: boolean;
  hataMesaji?: string;
  gonderimTarihi: string;
}

export interface PagedLogResponse<T> {
  items: T[];
  toplamKayit: number;
  sayfa: number;
  sayfaBoyutu: number;
  toplamSayfa: number;
}

export interface LogFilterRequest {
  baslangicTarihi?: string;
  bitisTarihi?: string;
  arama?: string;
  sayfa: number;
  sayfaBoyutu: number;
}

export interface LoginLogFilterRequest extends LogFilterRequest {
  sadeceBasarisiz?: boolean;
  girisTuru?: string;
}

export interface SystemLogFilterRequest extends LogFilterRequest {
  sadeceHata?: boolean;
  logSeviye?: string;
  kategori?: string;
}

export interface EmailLogFilterRequest extends LogFilterRequest {
  duyuruId?: number;
  sadeceBasarisiz?: boolean;
}
