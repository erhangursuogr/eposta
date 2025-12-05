import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../common/models/api-response.model';
import {
  TemplateCategory,
  CreateTemplateCategoryRequest,
  UpdateTemplateCategoryRequest,
  ReorderCategoriesRequest
} from '../common/models/template-category.model';

@Injectable({
  providedIn: 'root'
})
export class TemplateCategoryService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/template-categories`;

  /**
   * Tüm kategorileri getir
   */
  getAllCategories(): Observable<ApiResponse<TemplateCategory[]>> {
    return this.http.get<ApiResponse<TemplateCategory[]>>(this.apiUrl);
  }

  /**
   * Aktif kategorileri getir
   */
  getActiveCategories(): Observable<ApiResponse<TemplateCategory[]>> {
    return this.http.get<ApiResponse<TemplateCategory[]>>(`${this.apiUrl}/active`);
  }

  /**
   * ID'ye göre kategori detayı getir
   */
  getCategoryById(id: number): Observable<ApiResponse<TemplateCategory>> {
    return this.http.get<ApiResponse<TemplateCategory>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Yeni kategori oluştur
   */
  createCategory(data: CreateTemplateCategoryRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(this.apiUrl, data);
  }

  /**
   * Kategori güncelle
   */
  updateCategory(id: number, data: UpdateTemplateCategoryRequest): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/${id}`, data);
  }

  /**
   * Kategori sil
   */
  deleteCategory(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Kategoriyi aktif et
   */
  activateCategory(id: number): Observable<ApiResponse<void>> {
    return this.http.patch<ApiResponse<void>>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Kategoriyi pasif et
   */
  deactivateCategory(id: number): Observable<ApiResponse<void>> {
    return this.http.patch<ApiResponse<void>>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Kategori sıralamasını güncelle
   */
  reorderCategories(data: ReorderCategoriesRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/reorder`, data);
  }
}
