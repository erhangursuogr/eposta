import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../common/models/api-response.model';
import { Template, CreateTemplateRequest, UpdateTemplateRequest, TemplatePreview } from '../common/models/template.model';

@Injectable({
  providedIn: 'root'
})
export class TemplateService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/templates`;

  /**
   * Tüm şablonları getir
   */
  getTemplates(): Observable<ApiResponse<Template[]>> {
    return this.http.get<ApiResponse<Template[]>>(this.apiUrl);
  }

  /**
   * Aktif şablonları getir (duyuru oluşturmada kullanılacak)
   */
  getActiveTemplates(): Observable<ApiResponse<Template[]>> {
    return this.http.get<ApiResponse<Template[]>>(`${this.apiUrl}/active`);
  }

  /**
   * ID'ye göre şablon detayı getir
   */
  getTemplateById(id: number): Observable<ApiResponse<Template>> {
    return this.http.get<ApiResponse<Template>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Yeni şablon oluştur
   */
  createTemplate(data: CreateTemplateRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(this.apiUrl, data);
  }

  /**
   * Şablon güncelle
   */
  updateTemplate(id: number, data: UpdateTemplateRequest): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/${id}`, data);
  }

  /**
   * Şablon sil
   */
  deleteTemplate(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Şablonu aktif et
   */
  activateTemplate(id: number): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Şablonu pasif et
   */
  deactivateTemplate(id: number): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Şablonu çoğalt
   */
  duplicateTemplate(id: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/${id}/duplicate`, {});
  }

  /**
   * Şablon önizlemesi
   */
  previewTemplate(id: number, testData?: any): Observable<ApiResponse<TemplatePreview>> {
    return this.http.post<ApiResponse<TemplatePreview>>(`${this.apiUrl}/${id}/preview`, testData);
  }
}
