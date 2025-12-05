import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../common/models/api-response.model';
import { buildHttpParams } from '../common/utils/http-params.util';
import {
  Announcement,
  AnnouncementListParams,
  CreateAnnouncementRequest,
  UpdateAnnouncementRequest,
  AnnouncementRecipient,
  AnnouncementFile,
  AnnouncementApprovalRequest,
  AnnouncementRejectionRequest,
  AnnouncementMovement
} from '../common/models/announcement.model';

@Injectable({
  providedIn: 'root'
})
export class AnnouncementService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/announcements`;

  /**
   * Get announcement categories
   */
  getCategories(): Observable<ApiResponse<{ key: string; displayName: string; hasSignature: boolean }[]>> {
    return this.http.get<ApiResponse<{ key: string; displayName: string; hasSignature: boolean }[]>>(`${this.apiUrl}/categories`);
  }

  /**
   * Get paginated list of announcements with optional filters
   */
  getAnnouncements(params?: AnnouncementListParams): Observable<ApiResponse<Announcement[]>> {
    const httpParams = params ? buildHttpParams(params) : new HttpParams();
    return this.http.get<ApiResponse<Announcement[]>>(this.apiUrl, { params: httpParams });
  }

  /**
   * Get single announcement by ID
   */
  getAnnouncementById(id: number): Observable<ApiResponse<Announcement>> {
    return this.http.get<ApiResponse<Announcement>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create new announcement
   */
  createAnnouncement(request: CreateAnnouncementRequest): Observable<ApiResponse<Announcement>> {
    return this.http.post<ApiResponse<Announcement>>(this.apiUrl, request);
  }

  /**
   * Update existing announcement
   */
  updateAnnouncement(request: UpdateAnnouncementRequest): Observable<ApiResponse<Announcement>> {
    return this.http.put<ApiResponse<Announcement>>(`${this.apiUrl}/${request.id}`, request);
  }

  /**
   * Delete announcement (soft delete)
   */
  deleteAnnouncement(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Duplicate announcement
   */
  duplicateAnnouncement(id: number): Observable<ApiResponse<Announcement>> {
    return this.http.post<ApiResponse<Announcement>>(`${this.apiUrl}/${id}/duplicate`, {});
  }

  /**
   * Get announcement recipients
   */
  getAnnouncementRecipients(duyuruId: number, page: number = 1, pageSize: number = 20): Observable<ApiResponse<AnnouncementRecipient[]>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<ApiResponse<AnnouncementRecipient[]>>(`${this.apiUrl}/${duyuruId}/recipients`, { params });
  }

  /**
   * Get announcement files
   */
  getAnnouncementFiles(duyuruId: number): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${environment.apiUrl}/api/files/announcement/${duyuruId}`);
  }

  /**
   * Get pending approvals (MANAGER/ADMIN only)
   */
  getPendingApprovals(page: number = 1, pageSize: number = 20): Observable<ApiResponse<Announcement[]>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<ApiResponse<Announcement[]>>(`${environment.apiUrl}/api/announcement-approval/pending`, { params });
  }

  /**
   * Get approved announcements (filtered by current user role)
   */
  getApprovedAnnouncements(page: number = 1, pageSize: number = 20): Observable<ApiResponse<Announcement[]>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<ApiResponse<Announcement[]>>(`${environment.apiUrl}/api/announcement-approval/approved`, { params });
  }

  /**
   * Get rejected announcements (filtered by current user)
   */
  getRejectedAnnouncements(page: number = 1, pageSize: number = 20): Observable<ApiResponse<Announcement[]>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<ApiResponse<Announcement[]>>(`${environment.apiUrl}/api/announcement-approval/rejected`, { params });
  }

  /**
   * Submit announcement for approval
   */
  submitForApproval(duyuruId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${environment.apiUrl}/api/announcement-approval/${duyuruId}/submit`, {});
  }

  /**
   * Approve announcement (MANAGER/ADMIN only)
   */
  approveAnnouncement(duyuruIdOrRequest: number | AnnouncementApprovalRequest, note?: string): Observable<ApiResponse<void>> {
    if (typeof duyuruIdOrRequest === 'number') {
      // Simple overload: approveAnnouncement(id, note)
      return this.http.post<ApiResponse<void>>(
        `${environment.apiUrl}/api/announcement-approval/${duyuruIdOrRequest}/approve`,
        { note: note || '' }
      );
    } else {
      // Original: approveAnnouncement(request)
      return this.http.post<ApiResponse<void>>(
        `${environment.apiUrl}/api/announcement-approval/${duyuruIdOrRequest.duyuruId}/approve`,
        duyuruIdOrRequest
      );
    }
  }

  /**
   * Reject announcement (MANAGER/ADMIN only)
   */
  rejectAnnouncement(request: AnnouncementRejectionRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${environment.apiUrl}/api/announcement-approval/${request.duyuruId}/reject`, request);
  }

  /**
   * Cancel announcement
   */
  cancelAnnouncement(duyuruId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${environment.apiUrl}/api/announcement-approval/${duyuruId}/cancel`, {});
  }

  /**
   * Reactivate cancelled announcement
   */
  reactivateAnnouncement(duyuruId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/${duyuruId}/reactivate`, {});
  }

  /**
   * Send announcement immediately (bypasses approval if ADMIN)
   */
  sendAnnouncement(duyuruId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/${duyuruId}/send`, {});
  }

  /**
   * Approve and send announcement immediately (ADMIN/MANAGER only)
   * Bypasses approval process and sends directly
   */
  approveAndSend(duyuruId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/${duyuruId}/approve-and-send`, {});
  }

  /**
   * Send test email before actual send
   */
  sendTestEmail(duyuruId: number, testEmail: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/${duyuruId}/send-test`, { testEmail });
  }

  /**
   * Get announcement preview HTML
   */
  getAnnouncementPreview(duyuruId: number): Observable<ApiResponse<string>> {
    return this.http.get<ApiResponse<string>>(`${this.apiUrl}/${duyuruId}/preview`);
  }

  /**
   * Get announcement movements (history)
   */
  getAnnouncementMovements(duyuruId: number): Observable<ApiResponse<AnnouncementMovement[]>> {
    return this.http.get<ApiResponse<AnnouncementMovement[]>>(`${this.apiUrl}/${duyuruId}/movements`);
  }

  // ===== İKİ AŞAMALI ONAY SİSTEMİ =====

  /**
   * Kontrolör onayı - ILK_ONAY_BEKLIYOR → SON_ONAY_BEKLIYOR
   * @param duyuruId Duyuru ID
   * @param managerId Seçilen Manager ID
   * @param note Onay notu (opsiyonel)
   */
  coordinatorApprove(duyuruId: number, managerId: number, note?: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(
      `${environment.apiUrl}/api/announcement-approval/${duyuruId}/coordinator/approve`,
      { managerId, note }
    );
  }

  /**
   * Kontrolör reddi - ILK_ONAY_BEKLIYOR → TASLAK
   * @param duyuruId Duyuru ID
   * @param rejectionNote Red nedeni
   */
  coordinatorReject(duyuruId: number, rejectionNote: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(
      `${environment.apiUrl}/api/announcement-approval/${duyuruId}/coordinator/reject`,
      { redNedeni: rejectionNote }
    );
  }

  /**
   * Manager onayı - SON_ONAY_BEKLIYOR → ONAYLANDI
   * @param duyuruId Duyuru ID
   * @param note Onay notu (opsiyonel)
   */
  managerApprove(duyuruId: number, note?: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(
      `${environment.apiUrl}/api/announcement-approval/${duyuruId}/manager/approve`,
      { note }
    );
  }

  /**
   * Manager reddi - SON_ONAY_BEKLIYOR → TASLAK
   * @param duyuruId Duyuru ID
   * @param rejectionNote Red nedeni
   */
  managerReject(duyuruId: number, rejectionNote: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(
      `${environment.apiUrl}/api/announcement-approval/${duyuruId}/manager/reject`,
      { redNedeni: rejectionNote }
    );
  }

  /**
   * Manager onayı ve direkt gönderim - SON_ONAY_BEKLIYOR → ONAYLANDI → GONDERILDI
   * @param duyuruId Duyuru ID
   * @param note Onay notu (opsiyonel)
   */
  managerApproveAndSend(duyuruId: number, note?: string): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(
      `${environment.apiUrl}/api/announcement-approval/${duyuruId}/manager/approve-and-send`,
      { note }
    );
  }

  // ===== END: İKİ AŞAMALI ONAY SİSTEMİ =====

  /**
   * Export announcements to Excel
   */
  exportToExcel(params?: AnnouncementListParams): Observable<Blob> {
    let httpParams = new HttpParams();

    if (params) {
      if (params.searchQuery) httpParams = httpParams.set('searchQuery', params.searchQuery);
      if (params.durum) httpParams = httpParams.set('durum', params.durum);
      if (params.baslangicTarihi) httpParams = httpParams.set('baslangicTarihi', params.baslangicTarihi);
      if (params.bitisTarihi) httpParams = httpParams.set('bitisTarihi', params.bitisTarihi);
      if (params.onlyMine !== undefined) httpParams = httpParams.set('onlyMine', params.onlyMine.toString());
    }

    return this.http.get(`${this.apiUrl}/export`, {
      params: httpParams,
      responseType: 'blob'
    });
  }
}
