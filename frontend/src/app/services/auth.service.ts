import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, LoginData, LoginRequest } from '../common/models/api-response.model';
import { SessionTimeoutService } from './session-timeout.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private _http = inject(HttpClient);
  private _sessionTimeout = inject(SessionTimeoutService);
  private baseUrl = environment.apiUrl;

  // Authentication state
  isAuthenticated = signal<boolean>(false);
  currentUser = signal<any>(null);

  // LDAP Login
  login(request: LoginRequest): Observable<ApiResponse<LoginData>> {
    return this._http.post<ApiResponse<LoginData>>(`${this.baseUrl}/api/auth/login`, request);
  }

  // LDAP/SSO Logout
  logout(): Observable<ApiResponse> {
    return this._http.post<ApiResponse>(`${this.baseUrl}/api/auth/logout`, {});
  }

  getCurrentUser(): Observable<ApiResponse<any>> {
    return this._http.get<ApiResponse<any>>(`${this.baseUrl}/api/auth/me`);
  }

  checkSession(): boolean {
    const user = localStorage.getItem('user');
    if (user) {
      try {
        this.currentUser.set(JSON.parse(user));
        this.isAuthenticated.set(true);
        return true;
      } catch {
        localStorage.removeItem('user');
      }
    }
    return false;
  }

  // ========== SSO KEYCLOAK (SIMPLE) ==========

  /**
   * AUTH_MODE değerini backend'den çeker (0=LDAP, 1=SSO)
   */
  async getAuthMode(): Promise<'0' | '1'> {
    try {
      const res = await firstValueFrom(
        this._http.get<{ mode: string }>(`${this.baseUrl}/api/auth/mode`)
      );
      return res.mode as '0' | '1';
    } catch {
      return '0'; // Fallback: LDAP
    }
  }

  /**
   * SSO Keycloak callback işleyici
   * Returns: { success: boolean, errorType?: string, errorMessage?: string }
   */
  async handleSsoCallback(code: string): Promise<{ success: boolean; errorType?: string; errorMessage?: string }> {
    try {
      const response = await firstValueFrom(
        this._http.post<ApiResponse<LoginData>>(
          `${this.baseUrl}/api/auth/sso/callback`,
          { code }
        )
      );

      if (response.success && response.data) {
        this.currentUser.set(response.data.user);
        this.isAuthenticated.set(true);
        localStorage.setItem('user', JSON.stringify(response.data.user));

        // SSO id_token'ı localStorage'a kaydet (logout için)
        if (response.data.idToken) {
          localStorage.setItem('id_token', response.data.idToken);
        }

        // Session timeout monitoring başlat
        if (response.data.expiresAt) {
          this._sessionTimeout.setSessionExpiration(new Date(response.data.expiresAt));
          this._sessionTimeout.startMonitoring();
        }

        // SSO hata flag'ini temizle
        localStorage.removeItem('sso_error');

        return { success: true };
      }

      // Backend başarısız yanıt döndürdü (success: false)
      return this.handleSsoError(response.statusCode, response.message);
    } catch (error: any) {
      console.error('SSO callback error:', error);

      // HTTP status code'u kontrol et
      const statusCode = error?.status || error?.error?.statusCode;
      const errorMessage = error?.error?.message || 'SSO işlemi sırasında bir hata oluştu';

      return this.handleSsoError(statusCode, errorMessage);
    }
  }

  /**
   * SSO error'ları kategorize eder ve localStorage'a kaydeder (DRY helper)
   */
  private handleSsoError(statusCode: number | undefined, defaultMessage: string): { success: false; errorType: string; errorMessage: string } {
    let errorType = 'SSO_ERROR';
    let errorMessage = defaultMessage || 'SSO giriş başarısız oldu';

    // Status code'a göre error type ve mesaj belirle
    if (statusCode === 404) {
      errorType = 'USER_NOT_FOUND';
      errorMessage = 'Bu e-posta adresi ile kayıtlı bir kullanıcı bulunamadı. Lütfen sistem yöneticisi ile iletişime geçin.';
    } else if (statusCode === 403) {
      errorType = 'USER_INACTIVE';
      errorMessage = 'Kullanıcı hesabınız aktif değil. Lütfen sistem yöneticisi ile iletişime geçin.';
    }

    // SSO hata durumunu kaydet (infinite loop önleme)
    localStorage.setItem('sso_error', JSON.stringify({ type: errorType, message: errorMessage }));

    return { success: false, errorType, errorMessage };
  }


  /**
   * Dual-mode logout (AUTH_MODE'a göre)
   */
  async logoutWithMode(): Promise<void> {
    const mode = await this.getAuthMode();

    // Backend logout
    try {
      await firstValueFrom(this.logout());
    } catch (e) {
      console.warn('Backend logout failed:', e);
    }

    // Clear state
    this.isAuthenticated.set(false);
    this.currentUser.set(null);
    localStorage.clear();

    // Redirect
    if (mode === '1') {
      // SSO mode: Keycloak logout
      window.location.href = environment.keycloakLogoutUrl;
    } else {
      // LDAP mode: Login page
      window.location.href = '/login';
    }
  }
}
