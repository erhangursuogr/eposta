export enum GrupTipi {
  NORMAL = 'NORMAL',
  STATIK = 'STATIK',
  DINAMIK = 'DINAMIK',
  DEBIS = 'DEBIS'
}

export interface EmailGroup {
  id: number;
  grupAdi: string;
  aciklama?: string;
  grupTipi: GrupTipi;
  grupTipiText: string;
  bccOnly: boolean;
  uyeSayisi: number;
  aktif: string;
  isActive: boolean;
  olusturmaTarihi: Date;
}

export interface EmailGroupDetail {
  id: number;
  grupAdi: string;
  aciklama?: string;
  grupTipi: GrupTipi;
  grupTipiText: string;
  viewAdi?: string;
  filterKosulu?: string;
  listeciEmail?: string;
  bccOnly: boolean;
  canUseCC: boolean;
  aktif: string;
  olusturmaTarihi: Date;
  guncellemeTarihi?: Date;
  uyeSayisi: number;
  uyeler?: EmailGroupMember[];
}

export interface EmailGroupMember {
  id: number;
  email: string;
  adSoyad?: string;
  departman?: string;
  aktif: string;
  isActive: boolean;
  eklenmeTarihi: Date;
}

export interface CreateEmailGroupRequest {
  grupAdi: string;
  aciklama?: string;
  grupTipi: GrupTipi;
  viewAdi?: string;
  filterKosulu?: string;
  listeciEmail?: string;
  statikUyeler?: StatikUye[];
}

export interface UpdateEmailGroupRequest {
  grupAdi?: string;
  aciklama?: string;
  filterKosulu?: string;
  listeciEmail?: string;
  statikUyeler?: StatikUye[];
  aktif?: string;
}

export interface StatikUye {
  email: string;
  adSoyad?: string;
  departman?: string;
}

export interface AddGroupMemberRequest {
  email: string;
  adSoyad: string;
}

export interface ImportMembersResult {
  totalRows: number;
  successCount: number;
  failedCount: number;
  duplicateCount: number;
  failedEmails: string[];
  duplicateEmails: string[];
  message: string;
}

export interface DynamicGroupPreview {
  viewAdi: string;
  filterKosulu?: string;
  toplamUye: number;
  onizlemeUyeler: EmailGroupMember[];
  isValid: boolean;
  errorMessage?: string;
}

export interface PreviewDynamicGroupRequest {
  viewAdi: string;
  filterKosulu?: string;
}
