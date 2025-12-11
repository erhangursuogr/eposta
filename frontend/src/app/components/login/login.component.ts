import { Component, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SharedModule } from '../../common/shared/shared.module';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { NgOptimizedImage } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { UserDataService } from '../../services/userdata.service';
import { ErrorService } from '../../common/services/error.service';
import { SessionTimeoutService } from '../../services/session-timeout.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    SharedModule,
    NgOptimizedImage,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  private _authService = inject(AuthService);
  private _sessionTimeout = inject(SessionTimeoutService);
  private _userDataService = inject(UserDataService);
  private _errorService = inject(ErrorService);
  private _router = inject(Router);
  private destroyRef = inject(DestroyRef);

  loginform: FormGroup = new FormGroup({
    email: new FormControl('', [Validators.required]),
    password: new FormControl('', [
      Validators.required,
      Validators.minLength(8), // GÜVENLIK: Minimum 8 karakter
      Validators.maxLength(100)
    ]),
  });

  reqSended = false;
  hide = signal(true);
  errorMessage = signal('');

  clickEvent(event: MouseEvent) {
    this.hide.set(!this.hide());
    event.stopPropagation();
  }

  ngOnInit() {
    // Login sayfası ilk yüklendiğinde flag set et (sayfa kapatılıp açılınca localStorage temizlenmiş olabilir)
    // Bu sayede login sayfasındayken gelen 401'ler için "oturum doldu" mesajı gösterilmez
    if (!localStorage.getItem('manual_logout')) {
      localStorage.setItem('manual_logout', 'true');
    }

    if (this._userDataService.isAuthenticated()) {
      this._router.navigate([this._userDataService.redirectUrl()]);
    }
  }

  login() {
    if (this.loginform.valid) {
      this.reqSended = true;
      this.errorMessage.set('');

      let email = this.loginform.value.email.trim().toLowerCase();

      // @deu.edu.tr uzantısı yoksa ekle
      if (!email.includes('@')) {
        email = email + '@deu.edu.tr';
      } else if (!email.endsWith('@deu.edu.tr')) {
        // Başka bir domain yazılmışsa reddet
        this.errorMessage.set('Sadece @deu.edu.tr uzantılı e-posta adresleri kabul edilir');
        this.reqSended = false;
        return;
      }

      const loginRequest = {
        email: email,
        password: this.loginform.value.password,
      };

      // Memory leak fix: takeUntilDestroyed added
      this._authService.login(loginRequest)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (response) => {
            if (response.success) {
              // SECURITY: Token artık HttpOnly cookie'de (backend tarafından set edildi)
              // localStorage'a yazılmıyor (XSS koruması)

              this._errorService.showSuccess('Giriş başarılı.', 'Başarılı');

              // Session timeout monitoring başlat
              if (response.data?.expiresAt) {
                this._sessionTimeout.setSessionExpiration(new Date(response.data.expiresAt));
                this._sessionTimeout.startMonitoring();
              }

              // User bilgisini backend'den çek ve SONRA navigate et
              this._userDataService.setUser(() => {
                // Callback: setUser tamamlandıktan sonra çalışır
                // Manuel logout flag'ini temizle (başarılı login sonrası)
                localStorage.removeItem('manual_logout');
                this.loginform.reset();
                this._router.navigate([this._userDataService.redirectUrl()]);
                this.reqSended = false;
              });
            } else {
              this.reqSended = false;
            }
          },
          error: (err) => {
            // Backend'den gelen hata mesajını al (camelCase artık backend'den geliyor)
            const errorMsg = err.error?.message || err.error?.Message || 'Giriş başarısız. Lütfen bilgilerinizi kontrol edin.';

            // Form altında göster
            this.errorMessage.set(errorMsg);

            // Toastr mesajı göster - 403 için error, diğerleri için warning
            if (err.status === 403) {
              this._errorService.showError(errorMsg, 'Erişim Engellendi');
            } else if (err.status === 401) {
              this._errorService.showWarning(errorMsg, 'Yetki Hatası');
            } else if (err.status === 0) {
              this._errorService.showError('Sunucuya bağlanılamıyor. Lütfen internet bağlantınızı kontrol edin.', 'Bağlantı Hatası');
            } else {
              this._errorService.showWarning(errorMsg, 'Giriş Hatası');
            }
            this.reqSended = false;
          }
        });
    }
  }
}
