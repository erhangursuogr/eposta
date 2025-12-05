export interface Template {
  id: number;
  sablonAdi: string;
  konuSablonu?: string;
  icerikSablonu: string;
  kategoriId?: number;
  varsayilan: string; // 'Y' | 'N'
  aktif: string; // 'Y' | 'N'
  olusturmaTarihi: string;
  guncellemeTarihi?: string;
  kullanimSayisi?: number;
  kategori?: {
    id: number;
    kategoriAdi: string;
    renk: string;
    ikon: string;
  };
}

export interface CreateTemplateRequest {
  ad: string;
  aciklama?: string;
  konu: string;
  icerik: string;
  kategori?: string;
  kategoriId?: number;
}

export interface UpdateTemplateRequest {
  ad: string;
  aciklama?: string;
  konu: string;
  icerik: string;
  kategori?: string;
  kategoriId?: number;
}

export interface TemplatePreview {
  templateId: number;
  templateName: string;
  renderedSubject: string;
  renderedContent: string;
}
