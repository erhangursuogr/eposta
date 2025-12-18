import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { UserDataService } from '../../services/userdata.service';

/**
 * SSO Keycloak Callback Component (SIMPLE)
 * Keycloak'tan gelen authorization code'u işler
 */
@Component({
  selector: 'app-sso-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="callback-container">
      <div class="callback-card">
        @if (isProcessing()) {
          <div class="spinner"></div>
          <h2>Kimlik Doğrulanıyor...</h2>
          <p>Lütfen bekleyiniz...</p>
        } @else if (errorType()) {
          <div class="error-icon">
            @if (errorType() === 'USER_NOT_FOUND') {
              🔒
            } @else if (errorType() === 'USER_INACTIVE') {
              ⛔
            } @else {
              ⚠️
            }
          </div>
          <h2>
            @if (errorType() === 'USER_NOT_FOUND') {
              Kullanıcı Bulunamadı
            } @else if (errorType() === 'USER_INACTIVE') {
              Hesap Aktif Değil
            } @else {
              Giriş Hatası
            }
          </h2>
          <p class="error-message">{{ errorMessage() }}</p>

          @if (errorType() === 'USER_NOT_FOUND' || errorType() === 'USER_INACTIVE') {
            <div class="contact-info">
              <p>Yardım için:</p>
              <a href="https://destek.deu.edu.tr/front/ticket.form.php" target="_blank" class="contact-link">
                📧 destek.deu.edu.tr
              </a>
            </div>
          }

          <div class="action-buttons">
            @if (errorType() === 'USER_NOT_FOUND' || errorType() === 'USER_INACTIVE') {
              <button class="btn-keycloak-logout" (click)="logoutFromKeycloak()">
                Farklı Hesapla Giriş Yap
              </button>
            } @else {
              <button class="btn-retry" (click)="retryLogin()">
                Tekrar Dene
              </button>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .callback-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }
    .callback-card {
      background: white;
      border-radius: 16px;
      padding: 48px;
      text-align: center;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
      max-width: 450px;
      min-width: 350px;
    }
    .spinner {
      width: 60px;
      height: 60px;
      margin: 0 auto 24px;
      border: 4px solid #f3f3f3;
      border-top: 4px solid #667eea;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
    h2 {
      font-size: 24px;
      font-weight: 600;
      color: #333;
      margin: 0 0 12px 0;
    }
    p {
      font-size: 14px;
      color: #666;
      margin: 0;
    }
    .error-icon {
      font-size: 64px;
      margin-bottom: 16px;
    }
    .error-message {
      color: #555;
      line-height: 1.5;
      margin-bottom: 24px;
    }
    .contact-info {
      background: #f8f9fa;
      border-radius: 8px;
      padding: 16px;
      margin: 20px 0;
    }
    .contact-info p {
      margin-bottom: 8px;
      color: #666;
      font-size: 13px;
    }
    .contact-link {
      color: #667eea;
      text-decoration: none;
      font-weight: 500;
    }
    .contact-link:hover {
      text-decoration: underline;
    }
    .action-buttons {
      margin-top: 24px;
    }
    .btn-retry, .btn-keycloak-logout {
      padding: 12px 32px;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;
    }
    .btn-retry {
      background: #667eea;
      color: white;
    }
    .btn-retry:hover {
      background: #5a6fd6;
    }
    .btn-keycloak-logout {
      background: #6c757d;
      color: white;
    }
    .btn-keycloak-logout:hover {
      background: #5a6268;
    }
  `]
})
export class SsoCallbackComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private userDataService = inject(UserDataService);

  isProcessing = signal(true);
  errorType = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  async ngOnInit() {
    const code = this.route.snapshot.queryParamMap.get('code');
    const keycloakError = this.route.snapshot.queryParamMap.get('error');

    // Önceki SSO hatası varsa kontrol et (infinite loop önleme)
    const storedError = localStorage.getItem('sso_error');
    if (storedError && !code) {
      try {
        const errorData = JSON.parse(storedError);
        this.isProcessing.set(false);
        this.errorType.set(errorData.type || 'SSO_ERROR');
        this.errorMessage.set(errorData.message || 'SSO giriş başarısız oldu');
        return;
      } catch {
        localStorage.removeItem('sso_error');
      }
    }

    if (keycloakError) {
      this.isProcessing.set(false);
      this.errorType.set('SSO_ERROR');
      this.errorMessage.set(`Kimlik doğrulama hatası: ${keycloakError}`);
      return;
    }

    if (!code) {
      this.isProcessing.set(false);
      this.errorType.set('SSO_ERROR');
      this.errorMessage.set('Authorization code bulunamadı');
      return;
    }

    try {
      const result = await this.authService.handleSsoCallback(code);

      if (result.success) {
        // UserDataService'e user bilgisini yükle
        this.userDataService.setUser(() => {
          setTimeout(() => this.router.navigate(['/duyurular']), 500);
        });
      } else {
        this.isProcessing.set(false);
        this.errorType.set(result.errorType || 'SSO_ERROR');
        this.errorMessage.set(result.errorMessage || 'SSO giriş başarısız oldu');
      }
    } catch (err) {
      console.error('SSO callback error:', err);
      this.isProcessing.set(false);
      this.errorType.set('SSO_ERROR');
      this.errorMessage.set('Bir hata oluştu: ' + (err as any)?.message);
    }
  }

  /**
   * Keycloak oturumunu kapatıp farklı hesapla giriş yapmayı sağlar
   * NOT: Kullanıcı kayıtlı değilken id_token olmadığı için logout URL çalışmaz.
   * Bu nedenle auth URL'e prompt=login ekleyerek yeni giriş ekranına yönlendiriyoruz.
   */
  async logoutFromKeycloak() {
    // id_token'ı ÖNCE al (localStorage temizlenmeden)
    const idToken = localStorage.getItem('id_token');

    // Sadece SSO ile ilgili storage'ı temizle (selective cleanup, manual_logout flag'ini korur)
    this.clearSsoStorage();

    const environment = await import('../../../environments/environment').then(m => m.environment);

    if (idToken) {
      // id_token varsa normal logout yap
      const logoutUrl = `${environment.keycloakLogoutUrl}&id_token_hint=${encodeURIComponent(idToken)}`;
      window.location.href = logoutUrl;
    } else {
      // id_token yoksa (kullanıcı kayıtlı değilse), auth URL'e prompt=login ekleyerek
      // Keycloak'un oturumu sıfırlayıp yeni giriş ekranı göstermesini sağla
      const authUrl = environment.keycloakAuthUrl;

      // prompt=login parametresi ekle (zaten varsa değiştirme)
      const authUrlWithPrompt = authUrl.includes('prompt=')
        ? authUrl
        : `${authUrl}&prompt=login`;

      window.location.href = authUrlWithPrompt;
    }
  }

  /**
   * SSO giriş işlemini tekrar dener
   */
  retryLogin() {
    // Sadece SSO ile ilgili storage'ı temizle
    this.clearSsoStorage();

    // Login sayfasına yönlendir
    this.router.navigate(['/login']);
  }

  /**
   * SSO ile ilgili localStorage item'larını temizler (selective cleanup)
   */
  private clearSsoStorage() {
    localStorage.removeItem('sso_error');
    localStorage.removeItem('user');
    localStorage.removeItem('id_token');
    localStorage.removeItem('sessionExpiresAt');
    // NOT: manual_logout gibi diğer flag'ler korunur
  }
}

