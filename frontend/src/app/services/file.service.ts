import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../common/models/api-response.model';

export interface UploadedFile {
  id: number;
  dosyaAdi: string;
  dosyaYolu: string;
  dosyaBoyutu: number;
  mimeType: string;
  yuklemeTarihi: string;
  yukleyenKullaniciId: number;
  yukleyenKullaniciAdi: string;
}

export interface FileUploadResult {
  fileId: number;
  fileName: string;
  fileSize: number;
  fileType: string;
  uploadDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class FileService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/files`;

  /**
   * Upload single file
   */
  uploadFile(file: File, announcementId?: number, sessionId?: string): Observable<ApiResponse<FileUploadResult>> {
    const formData = new FormData();
    formData.append('file', file);
    if (announcementId) {
      formData.append('announcementId', announcementId.toString());
    }
    if (sessionId && !announcementId) {
      formData.append('sessionId', sessionId);
    }

    return this.http.post<ApiResponse<FileUploadResult>>(`${this.apiUrl}/upload`, formData);
  }

  /**
   * Upload multiple files with progress
   */
  uploadFileWithProgress(file: File): Observable<HttpEvent<any>> {
    const formData = new FormData();
    formData.append('file', file);

    const req = new HttpRequest('POST', `${this.apiUrl}/upload`, formData, {
      reportProgress: true
    });

    return this.http.request(req);
  }

  /**
   * Download file
   */
  downloadFile(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/download`, { responseType: 'blob' });
  }

  /**
   * Delete file
   */
  deleteFile(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get file info
   */
  getFileInfo(id: number): Observable<ApiResponse<UploadedFile>> {
    return this.http.get<ApiResponse<UploadedFile>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get session files
   */
  getSessionFiles(sessionId: string): Observable<ApiResponse<UploadedFile[]>> {
    return this.http.get<ApiResponse<UploadedFile[]>>(`${this.apiUrl}/session/${sessionId}`);
  }

  /**
   * Link session files to announcement
   */
  linkSessionFiles(sessionId: string, announcementId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/session/${sessionId}/link/${announcementId}`, {});
  }
}
