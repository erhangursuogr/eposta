import { Injectable,inject } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import moment from 'moment';
import { UserDataModel } from '../models/user-data.model';
import { UserDataService } from '../../services/userdata.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private _userDataService = inject(UserDataService);
  private _router = inject(Router);
  private _toastr = inject(ToastrService);
  private _lastRateLimitToast = 0; // Rate limit toastını throttle etmek için
  constructor() {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    // SECURITY: Token artık HttpOnly cookie'de tutulduğu için
    // localStorage'dan okuma kaldırıldı. Cookie otomatik gider.

    // withCredentials: true ekle (cookie'lerin gönderilmesi için)
    request = request.clone({
      withCredentials: true
    });

    // NOT: Token validation artık backend'de yapılıyor (JWT middleware)
    // Frontend'de token decode etmeye gerek yok (zaten erişemiyoruz)

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        // 401 Unauthorized - Oturum süresi dolmuş veya geçersiz token
        if (error.status === 401) {
          // Login ve callback sayfalarında 401'i ignore et
          const currentUrl = this._router.url;
          const isLoginOrCallback = currentUrl.includes('/login') || currentUrl.includes('/auth/callback');

          if (!isLoginOrCallback) {
            this._userDataService.clearUserData();

            // Manuel logout sonrası mesaj gösterme (localStorage flag kontrolü)
            const isManualLogout = localStorage.getItem('manual_logout') === 'true';
            if (!isManualLogout) {
              this._toastr.warning('Oturum süreniz doldu. Lütfen tekrar giriş yapın.', 'Oturum Süresi Doldu', {
                timeOut: 5000,
                closeButton: true
              });
            }

            this._router.navigate(['/login']);
          }
        }

        // 429 Too Many Requests - Rate limiting
        if (error.status === 429) {
          const retryAfter = error.headers.get('Retry-After') || '60';
          const retrySeconds = parseInt(retryAfter, 10);

          // Throttle: Son 5 saniyede rate limit toastı gösterilmediyse göster
          const now = Date.now();
          if (now - this._lastRateLimitToast > 5000) {
            this._lastRateLimitToast = now;
            this._toastr.error(
              `Çok fazla istek gönderdiniz. Lütfen ${retrySeconds} saniye bekleyip tekrar deneyin.`,
              'İstek Limiti Aşıldı',
              {
                timeOut: 8000,
                closeButton: true,
                progressBar: true
              }
            );
          }
        }

        return throwError(() => error);
      })
    );
  }
}
