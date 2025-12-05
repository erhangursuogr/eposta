import { inject, Injectable, signal, Injector } from '@angular/core';
import { Router } from '@angular/router';
import { UserDataModel } from '../common/models/user-data.model';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from './auth.service';
import { AnnouncementService } from './announcement.service';

@Injectable({
  providedIn: 'root',
})
export class UserDataService {
  private _authService = inject(AuthService);
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


  logout() {
    // SECURITY: Backend'e logout isteği gönder (cookie silinecek)
    this._authService.logout().subscribe({
      next: (response) => {
        if (response.success) {
          localStorage.clear();
          this._toastr.success('Çıkış yapıldı');
          this.user.set({} as UserDataModel);
          // Manuel logout flag set et (localStorage temizlendikten SONRA, navigate'den ÖNCE)
          localStorage.setItem('manual_logout', 'true');
          this._router.navigate(['/login']);
        }
      },
      error: (err) => {
        console.error('Logout failed:', err);
        // Hata olsa bile client-side temizle
        localStorage.clear();
        this.user.set({} as UserDataModel);
        // Manuel logout flag set et (localStorage temizlendikten SONRA, navigate'den ÖNCE)
        localStorage.setItem('manual_logout', 'true');
        this._router.navigate(['/login']);
        this._toastr.warning('Çıkış yapıldı (kısmi hata)');
      }
    });
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
