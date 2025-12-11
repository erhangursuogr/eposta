export interface ApiResponse<T = any> {
  success: boolean;
  statusCode: number;
  message: string;
  data?: T;
  errorDetail?: string;
  // Pagination properties (optional)
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginData {
  token: string;
  idToken?: string; // SSO Keycloak id_token (logout için)
  user: UserInfo;
  expiresAt: string; // ISO 8601 format - Session timeout warning için
}

export interface UserInfo {
  id: number;
  kullaniciAdi: string;
  adSoyad: string;
  email: string;
  departman?: string;
  unvan?: string;
  gorevYeri?: number; // Görev yeri kodu (0=Rektörlük, 500=Mühendislik vb.)
  gorevYeriAdi?: string; // Görev yeri adı
  rol: string;
}
