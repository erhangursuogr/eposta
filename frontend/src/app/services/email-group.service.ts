import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../common/models/api-response.model';
import type {
  EmailGroup,
  EmailGroupDetail,
  EmailGroupMember,
  CreateEmailGroupRequest,
  UpdateEmailGroupRequest,
  AddGroupMemberRequest,
  ImportMembersResult,
  DynamicGroupPreview,
  PreviewDynamicGroupRequest
} from '../common/models/email-group.model';

// Re-export types for convenience
export type { EmailGroup, EmailGroupDetail, EmailGroupMember, CreateEmailGroupRequest, UpdateEmailGroupRequest, AddGroupMemberRequest, ImportMembersResult, DynamicGroupPreview, PreviewDynamicGroupRequest };

@Injectable({
  providedIn: 'root'
})
export class EmailGroupService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/email-groups`;

  // Signals for reactive state
  groups = signal<EmailGroup[]>([]);
  loading = signal<boolean>(false);

  /**
   * Get all active groups (for dropdown/selection)
   * Backend automatically filters based on user role:
   * - ADMIN: sees all groups (active + inactive)
   * - Other roles: sees only active groups
   */
  getActiveGroups(): Observable<ApiResponse<EmailGroup[]>> {
    const params = new HttpParams()
      .set('page', '1')
      .set('pageSize', '1000');
    return this.http.get<ApiResponse<EmailGroup[]>>(this.apiUrl, { params });
  }

  /**
   * Get paginated groups with search
   */
  getGroups(page: number = 1, pageSize: number = 20, searchTerm?: string): Observable<ApiResponse<EmailGroup[]>> {
    this.loading.set(true);
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<ApiResponse<EmailGroup[]>>(this.apiUrl, { params }).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.groups.set(response.data);
        }
        this.loading.set(false);
      })
    );
  }

  /**
   * Get group detail by ID
   */
  getGroupById(id: number): Observable<ApiResponse<EmailGroupDetail>> {
    return this.http.get<ApiResponse<EmailGroupDetail>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create new group
   */
  createGroup(request: CreateEmailGroupRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(this.apiUrl, request);
  }

  /**
   * Update group
   */
  updateGroup(id: number, request: UpdateEmailGroupRequest): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete group
   */
  deleteGroup(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get group members
   */
  getGroupMembers(groupId: number): Observable<ApiResponse<EmailGroupMember[]>> {
    return this.http.get<ApiResponse<EmailGroupMember[]>>(`${this.apiUrl}/${groupId}/members`);
  }

  /**
   * Add member to group (NORMAL only)
   */
  addMember(groupId: number, request: AddGroupMemberRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/${groupId}/members`, request);
  }

  /**
   * Remove member from group
   */
  removeMember(groupId: number, memberId: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${groupId}/members/${memberId}`);
  }

  /**
   * Import members from file (STATIK only)
   */
  importMembers(groupId: number, file: File): Observable<ApiResponse<ImportMembersResult>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<ImportMembersResult>>(
      `${this.apiUrl}/${groupId}/import-members`,
      formData
    );
  }

  /**
   * Get group email list
   */
  getGroupEmails(groupId: number): Observable<ApiResponse<string[]>> {
    return this.http.get<ApiResponse<string[]>>(`${this.apiUrl}/${groupId}/emails`);
  }

  /**
   * Get grup tipi display text
   */
  getGrupTipiText(grupTipi: string): string {
    switch (grupTipi) {
      case 'MANUEL':
        return 'Manuel Liste';
      case 'DOSYA':
        return 'Dosyadan Yüklenen';
      case 'DINAMIK':
        return 'Dinamik View';
      case 'DEBIS':
        return 'Debis Listeci';
      default:
        return grupTipi;
    }
  }

  /**
   * Get grup tipi badge class
   */
  getGrupTipiBadge(grupTipi: string): string {
    switch (grupTipi) {
      case 'MANUEL':
        return 'status-chip status-primary';
      case 'DOSYA':
        return 'status-chip status-warning';
      case 'DINAMIK':
        return 'status-chip status-active';
      case 'DEBIS':
        return 'status-chip status-info';
      default:
        return 'status-chip status-gray';
    }
  }

  /**
   * Preview dynamic group - test view and filter
   */
  previewDynamicGroup(request: PreviewDynamicGroupRequest): Observable<ApiResponse<DynamicGroupPreview>> {
    return this.http.post<ApiResponse<DynamicGroupPreview>>(`${this.apiUrl}/preview-dynamic`, request);
  }
}
