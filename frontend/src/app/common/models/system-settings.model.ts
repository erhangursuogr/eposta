export interface SystemSetting {
  id: number;
  category: string;
  key: string;
  value: string;
  description: string;
  gizli: string; // 'Y' or 'N'
  aktif: string; // 'Y' or 'N'
  gorevYeri?: number | null;
}

export interface SystemSettingCategory {
  category: string;
  displayName: string;
  icon: string;
  color: string;
  settings: SystemSetting[];
  isExpanded?: boolean;
}

export interface UpdateSystemSettingRequest {
  value: string;
  description?: string;
  isActive?: boolean;
  gorevYeri?: number | null;
}

export interface UpdateBulkSettingsRequest {
  settings: {
    key: string;
    value: string;
  }[];
}

export const CATEGORY_CONFIG: Record<string, { displayName: string; icon: string; color: string; order: number }> = {
  'AUTH': { displayName: 'Kimlik Doğrulama Modu', icon: 'vpn_key', color: 'warn', order: 0 },
  'EMAIL_KATEGORI': { displayName: 'Duyuru Kategorileri ve İmzalar', icon: 'category', color: 'primary', order: 1 },
  'EMAIL_SISTEM': { displayName: 'Sistem Email Ayarları', icon: 'badge', color: 'accent', order: 2 },
  'EMAIL_DUYURU': { displayName: 'Duyuru Email Ayarları', icon: 'school', color: 'primary', order: 4 },
  'EMAIL_REKTOR': { displayName: 'Rektör Email Ayarları', icon: 'school', color: 'warn', order: 3 },
  'EMAIL_BID': { displayName: 'BİD Email Ayarları', icon: 'computer', color: 'accent', order: 5 },
  'EMAIL_ORTAK': { displayName: 'Ortak SMTP Ayarları', icon: 'email', color: 'primary', order: 6 },
  'DOSYA': { displayName: 'Dosya Ayarları', icon: 'folder', color: 'primary', order: 7 },
  'DEBIS': { displayName: 'DEBIS Servis Ayarları', icon: 'cloud', color: 'accent', order: 8 },
  'CACHE': { displayName: 'Cache Ayarları', icon: 'speed', color: 'primary', order: 9 },
  'SECURITY_DOMAIN': { displayName: 'Email Domain Güvenliği', icon: 'security', color: 'warn', order: 10 }
};
