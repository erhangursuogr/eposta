import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../common/models/api-response.model';
import { SystemSetting, UpdateSystemSettingRequest, UpdateBulkSettingsRequest } from '../common/models/system-settings.model';

@Injectable({
  providedIn: 'root'
})
export class SystemSettingsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/admin/system-settings`;

  /**
   * Get all system settings
   * @param category Optional category filter
   * @param includeSecret Include secret settings (default: false)
   * @param includeInactive Include inactive settings (default: false)
   */
  getAllSettings(category?: string, includeSecret: boolean = false, includeInactive: boolean = false): Observable<ApiResponse<SystemSetting[]>> {
    let params = new HttpParams();
    if (category) params = params.set('category', category);
    params = params.set('includeSecret', includeSecret.toString());
    params = params.set('includeInactive', includeInactive.toString());

    return this.http.get<ApiResponse<SystemSetting[]>>(`${this.apiUrl}/all`, { params });
  }

  /**
   * Get email settings (category-based)
   */
  getEmailSettings(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/email-settings`);
  }

  /**
   * Update email settings
   */
  updateEmailSettings(request: UpdateBulkSettingsRequest): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/email-settings`, request);
  }

  /**
   * Get managers list (for approval process settings)
   */
  getManagers(): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${this.apiUrl}/managers`);
  }

  /**
   * Create new system setting
   */
  createSetting(request: CreateSystemSettingRequest): Observable<ApiResponse<SystemSetting>> {
    return this.http.post<ApiResponse<SystemSetting>>(this.apiUrl, request);
  }

  /**
   * Update system setting
   */
  updateSingleSetting(id: number, request: UpdateSystemSettingRequest): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete system setting
   */
  deleteSetting(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Bulk update system settings (for SMTP group management)
   */
  bulkUpdateSettings(requests: BulkUpdateSettingRequest[]): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/bulk`, requests);
  }

  /**
   * Test SMTP connection for a category
   */
  testSmtpConnection(category: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/test-smtp-connection`, JSON.stringify(category), {
      headers: { 'Content-Type': 'application/json' }
    });
  }
}

export interface CreateSystemSettingRequest {
  category: string;
  key: string;
  value: string;
  description: string;
  isSecret?: boolean;
  isActive?: boolean;
  gorevYeri?: number | null;
}

export interface BulkUpdateSettingRequest {
  id: number;
  value: string;
  description?: string;
  isActive?: boolean;
  gorevYeri?: number | null;
}
