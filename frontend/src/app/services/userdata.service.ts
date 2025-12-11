import { inject, Injectable, signal, Injector } from '@angular/core';
import { Router } from '@angular/router';
import { UserDataModel } from '../common/models/user-data.model';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from './auth.service';
import { AnnouncementService } from './announcement.service';
import { SessionTimeoutService } from './session-timeout.service';

@Injectable({
  providedIn: 'root',
})
export class UserDataService {
  private _authService = inject(AuthService);
  private _sessionTimeout = inject(SessionTimeoutService);
  private _injector = inject(Injector);
  private _router = inject(Router);
  private _toastr = inject(ToastrService);

  user = signal<UserDataModel>({} as UserDataModel);
  pendingApprovalsCount = signal<number>(0);

  constructor() {}

setUser(onComplete?: () => void) {
    // SECURITY: Token artık HttpOnly cookie'de, backend'den user bilgisi çek
    this._authService.getCurrentUser().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const userInfo = response.data;

          // Backend'den gelen adSoyad'ı parse et (camelCase: adSoyad)
          const fullName = userInfo.adSoyad || userInfo.name || '';
          const nameParts = fullName.trim().split(' ');
          const firstName = nameParts.length > 0 ? nameParts[0] : '';
          const lastName = nameParts.length > 1 ? nameParts.slice(1).join(' ') : '';

          this.user.set({
            id: userInfo.id || 0,
            email: userInfo.email || '',
            role: userInfo.rol || 'VIEWER', // Backend: "rol" (camelCase)
            name: firstName,
            surname: lastName,
            token: 'COOKIE_BASED', // Placeholder - token artık cookie'de
            gorevYeri: userInfo.gorevYeri || 0
          });

          // Callback çağır (user set edildikten sonra)
          if (onComplete) {
            onComplete();
          }
        } else {
          this.user.set({} as UserDataModel);
          if (onComplete) {
            onComplete();
          }
        }
      },
      error: (err) => {
        //console.error('User info fetch failed:', err);

        // 401 (Unauthorized) - cookie yok veya geçersiz, sessizce handle et
        if (err.status === 401) {
          this.user.set({} as UserDataModel);
        } else {
          // Diğer hatalar - kullanıcıya bildir
          this._toastr.error('Kullanıcı bilgisi alınamadı. Lütfen tekrar giriş yapınız.');
          this.user.set({} as UserDataModel);
        }

        if (onComplete) {
          onComplete();
        }
      }
    });
  }


  async logout() {
    // Dual-mode logout (AUTH_MODE'a göre)
    try {
      const mode = await this._authService.getAuthMode();

      // SSO mode: id_token'ı localStorage.clear() öncesi al
      const idToken = mode === '1' ? localStorage.getItem('id_token') : null;

      // Backend logout
      this._authService.logout().subscribe({
        next: (response) => {
          if (response.success) {
            this._toastr.success('Çıkış yapıldı');
          }
        },
        error: (err) => {
          console.warn('Backend logout failed:', err);
        }
      });

      // Clear state
      localStorage.clear();
      this.user.set({} as UserDataModel);
      localStorage.setItem('manual_logout', 'true');

      // Session timeout monitoring'i durdur
      this._sessionTimeout.clearSessionExpiration();

      // Redirect based on mode
      if (mode === '1') {
        // SSO mode: Keycloak logout with id_token_hint
        const keycloakLogoutUrl = await import('../../environments/environment').then(m => m.environment.keycloakLogoutUrl);

        if (idToken) {
          const logoutUrlWithToken = `${keycloakLogoutUrl}&id_token_hint=${encodeURIComponent(idToken)}`;
          window.location.href = logoutUrlWithToken;
        } else {
          window.location.href = keycloakLogoutUrl;
        }
      } else {
        // LDAP mode: Login page
        this._router.navigate(['/login']);
      }
    } catch (error) {
      console.error('Logout error:', error);
      // Fallback: LDAP logout
      localStorage.clear();
      this.user.set({} as UserDataModel);
      localStorage.setItem('manual_logout', 'true');
      this._router.navigate(['/login']);
    }
  }

  /**
   * Oturum süresi dolduğunda client-side temizleme (backend'e istek göndermeden)
   */
  clearUserData() {
    // Manuel logout flag'ini koru (401 interceptor'da mesaj gösterip göstermeme kontrolü için)
    const manualLogoutFlag = localStorage.getItem('manual_logout');
    localStorage.clear();
    if (manualLogoutFlag) {
      localStorage.setItem('manual_logout', manualLogoutFlag);
    }
    this.user.set({} as UserDataModel);

    // Session timeout monitoring'i durdur
    this._sessionTimeout.clearSessionExpiration();
  }

  redirectUrl() {
    return '/';
  }

  isAuthenticated(): boolean {
    // SECURITY: Token artık cookie'de, user.id varlığı ile kontrol et
    const userId = this.user().id;
    return !!(userId && userId > 0);
  }

  /**
   * Onay bekleyen duyuru sayısını günceller
   * Onay/red işlemlerinden sonra çağrılır
   */
  refreshPendingApprovals(): void {
    const userRole = this.user().role;

    // Sadece COORDINATOR ve MANAGER için bildirim var
    if (userRole === 'COORDINATOR' || userRole === 'MANAGER') {
      // Lazy injection ile circular dependency önlenir
      const announcementService = this._injector.get(AnnouncementService);

      announcementService.getPendingApprovals(1, 100).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.pendingApprovalsCount.set(response.data.length);
          }
        },
        error: () => {
          // Hata olursa sessizce geç (bildirim kritik değil)
          console.warn('Failed to refresh pending approvals count');
        }
      });
    } else {
      this.pendingApprovalsCount.set(0);
    }
  }
}
