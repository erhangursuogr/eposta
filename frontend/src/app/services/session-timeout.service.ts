import { Injectable, inject, signal, Injector } from '@angular/core';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { UserDataService } from './userdata.service';

@Injectable({
  providedIn: 'root'
})
export class SessionTimeoutService {
  private _router = inject(Router);
  private _injector = inject(Injector); // Lazy injection için
  
  private checkInterval: any;
  private warningShown = signal<boolean>(false);
  private readonly WARNING_MINUTES = 5; // 5 dakika kala uyar
  private readonly CHECK_INTERVAL_MS = 60000; // 1 dakikada bir kontrol et

  startMonitoring() {
    this.stopMonitoring(); // Önceki interval'i temizle
    
    this.checkInterval = setInterval(() => {
      this.checkSessionTimeout();
    }, this.CHECK_INTERVAL_MS);
  }

  stopMonitoring() {
    if (this.checkInterval) {
      clearInterval(this.checkInterval);
      this.checkInterval = null;
    }
    this.warningShown.set(false);
  }

  private checkSessionTimeout() {
    const expiresAtStr = localStorage.getItem('sessionExpiresAt');
    if (!expiresAtStr) {
      return; // Session bilgisi yok
    }

    const expiresAt = new Date(expiresAtStr);
    const now = new Date();
    const diffMs = expiresAt.getTime() - now.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);

    // Token expire olduysa monitoring'i durdur
    if (diffMinutes <= 0) {
      this.stopMonitoring();
      return;
    }

    // 5 dakika veya daha az kaldıysa ve henüz uyarı gösterilmediyse
    if (diffMinutes <= this.WARNING_MINUTES && !this.warningShown()) {
      this.showTimeoutWarning(diffMinutes);
    }
  }

  private async showTimeoutWarning(minutesLeft: number) {
    this.warningShown.set(true);

    const result = await Swal.fire({
      title: 'Oturum Süreniz Sona Eriyor',
      html: `
        <p>Oturumunuz <strong>${minutesLeft} dakika</strong> içinde sona erecek.</p>
        <p>Çalışmaya devam etmek için "Devam Et" butonuna tıklayın.</p>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Devam Et',
      cancelButtonText: 'Çıkış Yap',
      confirmButtonColor: '#3085d6',
      cancelButtonColor: '#d33',
      allowOutsideClick: false,
      timer: 240000, // 4 dakika sonra otomatik kapan
      timerProgressBar: true
    });

    if (result.isConfirmed) {
      // Kullanıcı devam etmek istiyor - Sayfayı yenile (yeni token alacak)
      window.location.reload();
    } else {
      // Kullanıcı çıkış yapmak istiyor - Lazy injection ile circular dependency önlenir
      const userDataService = this._injector.get(UserDataService);
      await userDataService.logout();
    }

    this.warningShown.set(false);
  }

  // Login sonrası session expiration'ı kaydet
  setSessionExpiration(expiresAt: Date) {
    localStorage.setItem('sessionExpiresAt', expiresAt.toISOString());
    this.warningShown.set(false);
  }

  // Logout'ta temizle
  clearSessionExpiration() {
    localStorage.removeItem('sessionExpiresAt');
    this.stopMonitoring();
  }
}
