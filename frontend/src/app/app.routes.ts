import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { LayoutComponent } from './components/layout/layout.component';
import { HomeComponent } from './components/home/home.component';
import { NotfoundComponent } from './components/notfound/notfound.component';
import { AccessDeniedComponent } from './components/access-denied/access-denied.component';
import { authGuard } from './common/guards/auth.guard';
import { roleGuard } from './common/guards/role.guard';
import { loginGuard } from './common/guards/login.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [loginGuard],
    title: 'Giriş - DEÜ Duyuru Yönetim Sistemi'
  },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        component: HomeComponent,
        title: 'Ana Sayfa',
        data: { title: 'Ana Sayfa', description: 'DEÜ Duyuru Yönetim Sistemi' }
      },
      {
        path: 'duyurular',
        loadComponent: () => import('./components/announcements/announcement-list/announcement-list').then(m => m.AnnouncementList),
        title: 'Duyuru Listesi - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Duyuru Listesi', description: 'Tüm duyuruları görüntüleyin ve yönetin' }
      },
      {
        path: 'duyuru-olustur',
        loadComponent: () => import('./components/announcements/announcement-form/announcement-form').then(m => m.AnnouncementForm),
        title: 'Yeni Duyuru Oluştur - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Yeni Duyuru', description: 'Alıcılara gönderilecek duyuru içeriğini hazırlayın' }
      },
      {
        path: 'duyuru-duzenle/:id',
        loadComponent: () => import('./components/announcements/announcement-form/announcement-form').then(m => m.AnnouncementForm),
        title: 'Duyuru Düzenle - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Duyuru Düzenle', description: 'Mevcut duyuruyu düzenleyin' }
      },
      {
        path: 'zamanli-duyurular',
        loadComponent: () => import('./components/announcements/scheduled-announcements/scheduled-announcements').then(m => m.ScheduledAnnouncementsComponent),
        title: 'Zamanlanmış Duyurular - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Zamanlanmış Duyurular', description: 'Zamanlanmış duyuruları görüntüleyin ve yönetin' }
      },
      {
        path: 'planli-mailler',
        loadComponent: () => import('./components/announcements/scheduled-list/scheduled-list').then(m => m.ScheduledListComponent),
        title: 'Planlı Mailler - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Planlı Mailler', description: 'Zamanlanmış tüm duyuruları görüntüleyin' }
      },
      {
        path: 'onay-bekleyenler',
        loadComponent: () => import('./components/approvals/approval-pending/approval-pending').then(m => m.ApprovalPending),
        canActivate: [roleGuard],
        title: 'Onay Bekleyen Duyurular - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Onay Bekleyenler', description: 'Onay bekleyen duyuruları görüntüleyin ve onaylayın', roles: ['ADMIN', 'COORDINATOR', 'MANAGER'] }
      },
      {
        path: 'onaylananlar',
        loadComponent: () => import('./components/approvals/approved-announcements/approved-announcements').then(m => m.ApprovedAnnouncements),
        canActivate: [roleGuard],
        title: 'Onaylanan Duyurular - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Onaylananlar', description: 'Onayladığınız duyuruları görüntüleyin', roles: ['ADMIN', 'COORDINATOR', 'MANAGER'] }
      },
      {
        path: 'reddedilenler',
        loadComponent: () => import('./components/approvals/rejected-announcements/rejected-announcements').then(m => m.RejectedAnnouncements),
        canActivate: [roleGuard],
        title: 'Reddedilen Duyurular - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Reddedilenler', description: 'Reddettiğiniz duyuruları görüntüleyin', roles: ['ADMIN', 'COORDINATOR', 'MANAGER'] }
      },
      {
        path: 'gonderim-gecmisi',
        loadComponent: () => import('./components/announcements/announcement-sent-history/announcement-sent-history').then(m => m.AnnouncementSentHistoryComponent),
        title: 'Gönderim Geçmişi - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Gönderim Geçmişi', description: 'Gönderilen duyuruların detaylı listesi' }
      },
      {
        path: 'sablonlar',
        loadComponent: () => import('./components/templates/template-list/template-list').then(m => m.TemplateListComponent),
        title: 'Şablon Yönetimi - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Şablonlar', description: 'Email şablonlarını yönetin ve kullanın' } // Tüm roller erişebilir
      },
      {
        path: 'templates/new',
        loadComponent: () => import('./components/templates/template-form/template-form').then(m => m.TemplateFormComponent),
        canActivate: [roleGuard],
        title: 'Yeni Şablon - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Yeni Şablon', roles: ['ADMIN', 'MANAGER', 'COORDINATOR'] }
      },
      {
        path: 'templates/:id/edit',
        loadComponent: () => import('./components/templates/template-form/template-form').then(m => m.TemplateFormComponent),
        canActivate: [roleGuard],
        title: 'Şablon Düzenle - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Şablon Düzenle', roles: ['ADMIN', 'MANAGER', 'COORDINATOR'] }
      },
      {
        path: 'template-categories',
        loadComponent: () => import('./components/template-categories/template-category-list/template-category-list').then(m => m.TemplateCategoryList),
        canActivate: [roleGuard],
        title: 'Şablon Kategorileri - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Şablon Kategorileri', description: 'Şablon kategorilerini yönetin', roles: ['ADMIN', 'COORDINATOR'] }
      },
      {
        path: 'template-categories/new',
        loadComponent: () => import('./components/template-categories/template-category-form/template-category-form').then(m => m.TemplateCategoryForm),
        canActivate: [roleGuard],
        title: 'Yeni Kategori - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Yeni Kategori', roles: ['ADMIN', 'COORDINATOR'] }
      },
      {
        path: 'template-categories/edit/:id',
        loadComponent: () => import('./components/template-categories/template-category-form/template-category-form').then(m => m.TemplateCategoryForm),
        canActivate: [roleGuard],
        title: 'Kategori Düzenle - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Kategori Düzenle', roles: ['ADMIN', 'COORDINATOR'] }
      },
      {
        path: 'grup-yonetimi',
        loadComponent: () => import('./components/system-settings/email-groups/email-group-list/email-group-list').then(m => m.EmailGroupList),
        canActivate: [roleGuard],
        title: 'Grup Yönetimi - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Grup Yönetimi', description: 'Email gruplarını oluşturun ve yönetin', roles: ['ADMIN'] }
      },
      {
        path: 'kullanici-yonetimi',
        loadComponent: () => import('./components/user-management/user-list/user-list').then(m => m.UserList),
        canActivate: [roleGuard],
        title: 'Kullanıcı Yönetimi - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Kullanıcı Yönetimi', description: 'Kullanıcıları yönetin', roles: ['ADMIN'] }
      },
      {
        path: 'kullanici-yonetimi/yeni',
        loadComponent: () => import('./components/user-management/user-form/user-form').then(m => m.UserForm),
        canActivate: [roleGuard],
        title: 'Yeni Kullanıcı - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Yeni Kullanıcı', roles: ['ADMIN'] }
      },
      {
        path: 'sistem-ayarlari',
        loadComponent: () => import('./components/system-settings/system-settings-list/system-settings-list').then(m => m.SystemSettingsList),
        canActivate: [roleGuard],
        title: 'Genel Sistem Ayarları - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Genel Sistem Ayarları', description: 'Teknik sistem ayarları', roles: ['ADMIN'] }
      },
      {
        path: 'email-smtp-ayarlari',
        loadComponent: () => import('./components/system-settings/email-smtp-settings/email-smtp-settings-list/email-smtp-settings-list').then(m => m.EmailSmtpSettingsList),
        canActivate: [roleGuard],
        title: 'Duyuru SMTP Ayarları - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Duyuru SMTP Ayarları', description: 'SMTP grupları yönetimi', roles: ['ADMIN'] }
      },
      {
        path: 'email-kategorileri',
        loadComponent: () => import('./components/system-settings/email-categories/email-category-list/email-category-list').then(m => m.EmailCategoryList),
        canActivate: [roleGuard],
        title: 'Duyuru Kategorileri ve İmzalar - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Duyuru Kategorileri ve İmzalar', description: 'Kategori ve imza yönetimi', roles: ['ADMIN'] }
      },
      {
        path: 'sistem-loglari',
        loadComponent: () => import('./components/system-settings/system-logs/system-logs-list/system-logs-list').then(m => m.SystemLogsList),
        canActivate: [roleGuard],
        title: 'Sistem Logları - DEÜ Duyuru Yönetim Sistemi',
        data: { title: 'Sistem Logları', description: 'Login, sistem ve email logları', roles: ['ADMIN'] }
      },
      {
        path: 'access-denied',
        component: AccessDeniedComponent,
        title: 'Yetkisiz Erişim - DEÜ Duyuru Yönetim Sistemi'
      },
      {
        path: 'not-found',
        component: NotfoundComponent,
        title: 'Sayfa Bulunamadı - DEÜ Duyuru Yönetim Sistemi'
      }
    ]
  },
  {
    path: '**',
    component: NotfoundComponent,
    title: 'Sayfa Bulunamadı - DEÜ Duyuru Yönetim Sistemi'
  }
];
