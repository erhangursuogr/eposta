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
        } @else if (error()) {
          <div class="error-icon">⚠️</div>
          <h2>Hata</h2>
          <p>{{ error() }}</p>
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
      max-width: 400px;
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
      margin: 0 0 8px 0;
    }
    p {
      font-size: 14px;
      color: #666;
      margin: 0;
    }
    .error-icon {
      font-size: 48px;
      margin-bottom: 16px;
    }
  `]
})
export class SsoCallbackComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private userDataService = inject(UserDataService);

  isProcessing = signal(true);
  error = signal<string | null>(null);

  async ngOnInit() {
    const code = this.route.snapshot.queryParamMap.get('code');
    const keycloakError = this.route.snapshot.queryParamMap.get('error');

    if (keycloakError) {
      this.isProcessing.set(false);
      this.error.set(`SSO hatası: ${keycloakError}`);
      setTimeout(() => this.router.navigate(['/login']), 3000);
      return;
    }

    if (!code) {
      this.isProcessing.set(false);
      this.error.set('Authorization code bulunamadı');
      setTimeout(() => this.router.navigate(['/login']), 3000);
      return;
    }

    try {
      const success = await this.authService.handleSsoCallback(code);

      if (success) {
        // UserDataService'e user bilgisini yükle
        this.userDataService.setUser(() => {
          setTimeout(() => this.router.navigate(['/duyurular']), 500);
        });
      } else {
        this.isProcessing.set(false);
        this.error.set('SSO giriş başarısız oldu. Lütfen tekrar deneyin.');
        setTimeout(() => this.router.navigate(['/login']), 3000);
      }
    } catch (err) {
      console.error('SSO callback error:', err);
      this.isProcessing.set(false);
      this.error.set('Bir hata oluştu: ' + (err as any)?.message);
      setTimeout(() => this.router.navigate(['/login']), 3000);
    }
  }
}
