export interface TemplateCategory {
  id: number;
  kategoriAdi: string;
  aciklama?: string;
  renk: string;
  ikon: string;
  siraNo: number;
  aktif: string;
  olusturmaTarihi: string;
  guncellemeTarihi?: string;
}

export interface CreateTemplateCategoryRequest {
  kategoriAdi: string;
  aciklama?: string;
  renk?: string;
  ikon?: string;
  siraNo: number;
}

export interface UpdateTemplateCategoryRequest {
  kategoriAdi: string;
  aciklama?: string;
  renk?: string;
  ikon?: string;
  siraNo: number;
}

export interface ReorderCategoriesRequest {
  categoryIds: number[];
}
