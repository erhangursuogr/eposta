import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../common/models/api-response.model';

export interface User {
  id: number;
  ad?: string;
  soyad?: string;
  adSoyad?: string;
  email: string;
  rolKodu?: string;
  rolAdi?: string;
  aktif?: string;
  departman?: string;
  unvan?: string;
}

// DTOs for user management
export interface UserListView {
  id: number;
  kullaniciAdi: string;
  adSoyad: string;
  email: string;
  departman?: string;
  unvan?: string;
  rol: string;
  rolKodu: string;
  rolAdi: string;
  aktif: string;
  olusturmaTarihi: string;
  sonGirisTarihi?: string;
}

export interface UserDetailView {
  id: number;
  kullaniciAdi: string;
  adSoyad: string;
  email: string;
  departman?: string;
  unvan?: string;
  rolId: number;
  rol: string;
  rolKodu: string;
  rolAdi: string;
  aktif: string;
  olusturmaTarihi: string;
  sonGirisTarihi?: string;
  guncellemeTarihi?: string;
}

export interface CreateUserRequest {
  email: string;
  rolId: number;
  aktif: string;
}

export interface UpdateUserRequest {
  adSoyad: string;
  email: string;
  departman?: string | null;
  unvan?: string | null;
  rolId: number;
  aktif: string;
}

export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  adminCount: number;
  managerCount: number;
  moderatorCount: number;
  editorCount: number;
  viewerCount: number;
}

export interface ApproverView {
  id: number;
  adSoyad: string;
  email: string;
  departman?: string;
  rolAdi: string;
}

export interface Role {
  id: number;
  rolAdi: string;
  rolKodu: string;
  aciklama?: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/User`;

  // Signals for reactive state
  users = signal<UserListView[]>([]);
  statistics = signal<UserStatistics | null>(null);
  loading = signal<boolean>(false);

  /**
   * Get managers for approval assignment (MANAGER + ADMIN roles)
   */
  getManagers(): Observable<ApiResponse<User[]>> {
    return this.http.get<ApiResponse<User[]>>(`${this.apiUrl}/approvers`);
  }

  /**
   * Get users with filters (legacy - backward compatibility)
   */
  getUsers(search?: string, role?: string, activeOnly: string = 'Y', page: number = 1, pageSize: number = 20): Observable<ApiResponse<User[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString())
      .set('activeOnly', activeOnly);

    if (search) params = params.set('search', search);
    if (role) params = params.set('role', role);

    return this.http.get<ApiResponse<User[]>>(this.apiUrl, { params });
  }

  /**
   * Get users (new - for user management)
   * GET /api/User?search=...&role=...&activeOnly=...&page=...&pageSize=...
   */
  getUserList(
    search?: string,
    role?: string,
    activeOnly?: string,
    page: number = 1,
    pageSize: number = 50
  ): Observable<ApiResponse<UserListView[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) params = params.set('search', search);
    if (role) params = params.set('role', role);
    if (activeOnly) params = params.set('activeOnly', activeOnly);

    return this.http.get<ApiResponse<UserListView[]>>(this.apiUrl, { params });
  }

  /**
   * Get user by ID
   * GET /api/User/{id}
   */
  getUserById(id: number): Observable<ApiResponse<UserDetailView>> {
    return this.http.get<ApiResponse<UserDetailView>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create new user (ADMIN only)
   * POST /api/User
   */
  createUser(request: CreateUserRequest): Observable<ApiResponse<UserDetailView>> {
    return this.http.post<ApiResponse<UserDetailView>>(this.apiUrl, request);
  }

  /**
   * Update user (ADMIN only)
   * PUT /api/User/{id}
   */
  updateUser(id: number, request: UpdateUserRequest): Observable<ApiResponse<UserDetailView>> {
    return this.http.put<ApiResponse<UserDetailView>>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete user (ADMIN only) - soft delete
   * DELETE /api/User/{id}
   */
  deleteUser(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get user statistics
   * GET /api/User/statistics
   */
  getStatistics(): Observable<ApiResponse<UserStatistics>> {
    return this.http.get<ApiResponse<UserStatistics>>(`${this.apiUrl}/statistics`);
  }

  /**
   * Get approvers (MANAGER + COORDINATOR roles)
   * GET /api/User/approvers
   */
  getApprovers(): Observable<ApiResponse<ApproverView[]>> {
    return this.http.get<ApiResponse<ApproverView[]>>(`${this.apiUrl}/approvers`);
  }

  /**
   * Static roles (fallback)
   */
  getStaticRoles(): Role[] {
    return [
      { id: 1, rolAdi: 'Admin', rolKodu: 'ADMIN', aciklama: 'Sistem yöneticisi' },
      { id: 2, rolAdi: 'Yönetici', rolKodu: 'MANAGER', aciklama: 'Son onaylayıcı' },
      { id: 3, rolAdi: 'Kontrolör', rolKodu: 'COORDINATOR', aciklama: 'İlk onaylayıcı' },
      { id: 4, rolAdi: 'Editör', rolKodu: 'EDITOR', aciklama: 'Duyuru oluşturabilir' },
      { id: 5, rolAdi: 'Görüntüleyici', rolKodu: 'VIEWER', aciklama: 'Sadece okuma' }
    ];
  }
}
