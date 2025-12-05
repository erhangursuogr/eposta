import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { UserDataService } from '../../services/userdata.service';

@Injectable({
  providedIn: 'root',
})
export class ErrorService {
  private _toastr = inject(ToastrService);
  private _router = inject(Router);
  private _userDataService = inject(UserDataService);

  /**
   * HTTP hatalarını merkezi olarak yönetir
   * @param err HttpErrorResponse objesi
   * @param customMessage Özel hata mesajı (opsiyonel)
   * @param showToast Toast gösterilsin mi? (default: true)
   * @returns Backend'den gelen hata mesajı
   */
  errorHandler(
    err: HttpErrorResponse,
    customMessage?: string,
    showToast: boolean = true
  ): string {
    let errorMessage = customMessage || 'Bir hata oluştu.';
    let title = 'Hata';

    switch (err.status) {
      case 0:
        errorMessage = 'Sunucuya bağlanılamıyor. Lütfen internet bağlantınızı kontrol edin.';
        title = 'Bağlantı Hatası';
        break;

      case 400:
        errorMessage = err.error?.message || 'Geçersiz istek. Lütfen girdiğiniz bilgileri kontrol edin.';
        title = 'Hatalı İşlem';
        break;

      case 401:
        errorMessage = err.error?.message || 'Oturumunuzun süresi dolmuş. Lütfen tekrar giriş yapın.';
        title = 'Yetki Hatası';

        // Login sayfasında değilse logout yap ve toast göster
        if (!this._router.url.includes('/login')) {
          if (showToast) {
            this._toastr.warning(errorMessage, title);
          }
          this._userDataService.logout();
        }
        return errorMessage;

      case 403:
        errorMessage = err.error?.message || 'Bu işlemi yapmaya yetkiniz bulunmamaktadır.';
        title = 'Erişim Engellendi';

        // 403: Authorization hatası (yetki yok), 401 değil!
        // Kullanıcı giriş yapmış ama yetkisi yok - logout YAPMA
        // Sadece hata mesajı göster
        break;

      case 404:
        errorMessage = err.error?.message || 'İstenen kayıt bulunamadı.';
        title = 'Kayıt Bulunamadı';
        break;

      case 409:
        errorMessage = err.error?.message || 'Bu kayıt zaten mevcut.';
        title = 'Çakışma';
        break;

      case 422:
        errorMessage = err.error?.message || 'Gönderilen veriler işlenemedi.';
        title = 'Doğrulama Hatası';
        break;

      case 500:
        errorMessage = err.error?.message || 'Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.';
        title = 'Sunucu Hatası';
        break;

      case 502:
      case 503:
      case 504:
        errorMessage = 'Sunucu şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.';
        title = 'Sunucu Kullanılamıyor';
        break;

      default:
        errorMessage = err.error?.message || err.error?.statusCodeMessage || 'Beklenmeyen bir hata oluştu.';
        title = 'Hata';
        break;
    }

    if (showToast) {
      this._toastr.error(errorMessage, title);
    }

    return errorMessage;
  }

  /**
   * Başarı mesajı göster
   */
  showSuccess(message: string, title: string = 'Başarılı') {
    this._toastr.success(message, title);
  }

  /**
   * Hata mesajı göster
   */
  showError(message: string, title: string = 'Hata') {
    this._toastr.error(message, title);
  }

  /**
   * Bilgi mesajı göster
   */
  showInfo(message: string, title: string = 'Bilgi') {
    this._toastr.info(message, title);
  }

  /**
   * Uyarı mesajı göster
   */
  showWarning(message: string, title: string = 'Uyarı') {
    this._toastr.warning(message, title);
  }
}
