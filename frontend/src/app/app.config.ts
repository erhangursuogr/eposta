import { ApplicationConfig, importProvidersFrom, provideBrowserGlobalErrorListeners, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AuthInterceptor } from './common/interceptors/auth.interceptor';
import { provideAnimations } from '@angular/platform-browser/animations';
import { ToastrModule } from 'ngx-toastr';
import { NgxSpinnerModule } from 'ngx-spinner';
import { routes } from './app.routes';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { UserDataService } from './services/userdata.service';
import { inject } from '@angular/core';
// ✅ Angular Material tarih biçimi ve locale ayarları için eklenen importlar:
import { provideMomentDateAdapter } from '@angular/material-moment-adapter';
import { MAT_DATE_LOCALE, MAT_DATE_FORMATS } from '@angular/material/core';

// ✅ Özel tarih formatı tanımı (DD/MM/YYYY)
export const MY_DATE_FORMATS = {
  parse: {
    dateInput: 'DD/MM/YYYY',
  },
  display: {
    dateInput: 'DD/MM/YYYY',
    monthYearLabel: 'MMM YYYY',
    dateA11yLabel: 'DD/MM/YYYY',
    monthYearA11yLabel: 'MMMM YYYY',
  },
};

// App başlatmadan önce user bilgisini yükle (cookie-based auth için gerekli)
// SSO callback route'unda skip et (henüz token yok)
function initializeApp() {
  const userDataService = inject(UserDataService);
  return () => new Promise<void>((resolve) => {
    // SSO callback sayfasındaysak user bilgisi çekmeyi skip et
    if (window.location.pathname.includes('/auth/callback')) {
      resolve();
      return;
    }

    userDataService.setUser(() => {
      resolve();
    });
  });
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimations(),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true,
    },
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      multi: true
    },
    importProvidersFrom(
      ToastrModule.forRoot({
        timeOut: 3000,
        positionClass: 'toast-top-right',
        preventDuplicates: true,
        progressBar: true
      }),
      NgxSpinnerModule.forRoot({ type: 'ball-scale-multiple' })
    ),

    // ✅ Tarih formatı sağlayıcıları (eklenen kısım)
    provideMomentDateAdapter(),
    { provide: MAT_DATE_LOCALE, useValue: 'tr-TR' },
    { provide: MAT_DATE_FORMATS, useValue: MY_DATE_FORMATS }
  ]
};


//Material-Table Kullanıcak İse Paginator Türkçeleştirme Düzenlemesi
export function CustomPaginator(): MatPaginatorIntl {
  const paginatorIntl = new MatPaginatorIntl();
  paginatorIntl.itemsPerPageLabel = 'Sayfa başına kayıt sayısı:';
  paginatorIntl.nextPageLabel = 'Sonraki Sayfa';
  paginatorIntl.previousPageLabel = 'Önceki Sayfa';
  paginatorIntl.firstPageLabel = 'İlk Sayfa';
  paginatorIntl.lastPageLabel = 'Son Sayfa';
  paginatorIntl.getRangeLabel = (
    page: number,
    pageSize: number,
    length: number
  ) => {
    if (length === 0 || pageSize === 0) {
      return '0 / ' + length;
    }
    length = Math.max(length, 0);
    const startIndex = page * pageSize;
    const endIndex =
      startIndex < length
        ? Math.min(startIndex + pageSize, length)
        : startIndex + pageSize;
    return startIndex + 1 + ' - ' + endIndex + ' / ' + length;
  };
  return paginatorIntl;
}
