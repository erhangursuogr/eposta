import { MediaMatcher } from '@angular/cdk/layout';

import {
  ChangeDetectorRef,
  Component,
  EventEmitter,
  inject,
  Output,
  computed
} from '@angular/core';
import { SharedModule } from '../../../common/shared/shared.module';
import { MatListModule } from '@angular/material/list';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { UserDataService } from '../../../services/userdata.service';

export interface MenuItem {
  id: string;
  routerLink: string;
  icon: string;
  title: string;
  permissions: string[];
  visible?: boolean;
}

export interface MenuSection {
  id: string;
  title: string;
  icon: string;
  children: MenuItem[];
  permissions: string[];
  expanded?: boolean;
  visible?: boolean;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    SharedModule,
    MatListModule,
    MatExpansionModule,
    MatIconModule,
    MatButtonModule
],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
})
export class SidebarComponent {
  @Output() closeSidebar = new EventEmitter<void>();

  private _userDataService = inject(UserDataService);

  // User property for better performance in template
  get currentUser() {
    return this._userDataService.user();
  }

  get userInitials(): string {
    const user = this.currentUser;
    const firstInitial = user?.name?.charAt(0) ?? '';
    const lastInitial = user?.surname?.charAt(0) ?? '';
    return firstInitial + lastInitial;
  }

  get userFullName(): string {
    const user = this.currentUser;
    return `${user?.name ?? ''} ${user?.surname ?? ''}`.trim();
  }

  mobileQuery: MediaQueryList;
  private _mobileQueryListener: () => void;

  menuSections: MenuSection[] = [];
  standaloneMenuItems: MenuItem[] = [];
  mainRoute = '/';

  logoutSound = new Audio('assets/logout.mp3');

   playLogoutSound() {
    this.logoutSound.currentTime = 0; // başa sar
    this.logoutSound.play().catch(err => console.warn(err));
  }

  constructor(changeDetectorRef: ChangeDetectorRef, media: MediaMatcher) {
    this.mobileQuery = media.matchMedia('(max-width: 1000px)');
    this._mobileQueryListener = () => changeDetectorRef.detectChanges();
    this.mobileQuery.addListener(this._mobileQueryListener);
  }

  ngOnDestroy(): void {
    this.mobileQuery.removeListener(this._mobileQueryListener);
  }

  logout() {
    this._userDataService.logout();
    this.playLogoutSound();
  }

  hideSideBar() {
    if(this.mobileQuery.matches){
      this.closeSidebar.emit();
    }
  }

  private menuConfiguration: MenuSection[] = [
    {
      id: 'announcement',
      title: 'Duyuru İşlemleri',
      icon: 'campaign',
      permissions: ['ADMIN', 'MANAGER', 'COORDINATOR', 'EDITOR', 'VIEWER'],
      expanded: false,
      children: [
        {
          id: 'announcement-list',
          routerLink: '/duyurular',
          icon: 'list',
          title: 'Duyuru Listesi',
          permissions: ['ADMIN', 'MANAGER', 'COORDINATOR', 'EDITOR', 'VIEWER']
        },
        {
          id: 'announcement-create',
          routerLink: '/duyuru-olustur',
          icon: 'add_circle',
          title: 'Yeni Duyuru',
          permissions: ['ADMIN', 'MANAGER', 'EDITOR']
        },
        {
          id: 'announcement-drafts',
          routerLink: '/sablonlar',
          icon: 'drafts',
          title: 'Şablonlar',
          permissions: ['ADMIN', 'MANAGER', 'COORDINATOR', 'EDITOR']
        },
        // TODO: Zamanlama özelliği ileride kullanılacak
        {
          id: 'announcement-scheduled',
          routerLink: '/zamanli-duyurular',
          icon: 'schedule',
          title: 'Zamanlanmış Duyurular',
          permissions: ['ADMIN', 'MANAGER', 'COORDINATOR', 'EDITOR']
        }
      ]
    },
    {
      id: 'approval',
      title: 'Onay İşlemleri',
      icon: 'task_alt',
      permissions: ['ADMIN', 'MANAGER', 'COORDINATOR'],
      expanded: false,
      children: [
        {
          id: 'approval-pending',
          routerLink: '/onay-bekleyenler',
          icon: 'pending_actions',
          title: 'Onay Bekleyenler',
          permissions: ['ADMIN', 'MANAGER', 'COORDINATOR']
        },
        {
          id: 'approval-approved',
          routerLink: '/onaylananlar',
          icon: 'check_circle',
          title: 'Onaylananlar',
          permissions: ['ADMIN', 'MANAGER', 'COORDINATOR']
        },
        {
          id: 'approval-rejected',
          routerLink: '/reddedilenler',
          icon: 'cancel',
          title: 'Reddedilenler',
          permissions: ['ADMIN', 'MANAGER', 'COORDINATOR']
        }
      ]
    },
    {
      id: 'mailing',
      title: 'Duyuru Yönetimi',
      icon: 'mail',
      permissions: ['ADMIN', 'MANAGER', 'COORDINATOR', 'EDITOR', 'VIEWER'],
      expanded: false,
      children: [
        {
          id: 'mail-history',
          routerLink: '/gonderim-gecmisi',
          icon: 'history',
          title: 'Gönderim Geçmişi',
          permissions: ['ADMIN', 'MANAGER', 'COORDINATOR', 'EDITOR', 'VIEWER']
        },
        {
          id: 'mail-scheduled',
          routerLink: '/planli-mailler',
          icon: 'schedule_send',
          title: 'Planlı Duyurular',
          permissions: ['ADMIN', 'MANAGER', 'COORDINATOR', 'EDITOR']
        }
      ]
    },
    {
      id: 'groups',
      title: 'Grup Yönetimi',
      icon: 'groups',
      permissions: ['ADMIN'],
      expanded: false,
      children: [
        {
          id: 'group-management',
          routerLink: '/grup-yonetimi',
          icon: 'group_work',
          title: 'Grup Yönetimi',
          permissions: ['ADMIN']
        }
      ]
    },
    {
      id: 'users',
      title: 'Kullanıcı Yönetimi',
      icon: 'people',
      permissions: ['ADMIN'],
      expanded: false,
      children: [
        {
          id: 'user-management',
          routerLink: '/kullanici-yonetimi',
          icon: 'person',
          title: 'Kullanıcı Yönetimi',
          permissions: ['ADMIN']
        }
      ]
    },
    {
      id: 'system',
      title: 'Sistem Yönetimi',
      icon: 'settings',
      permissions: ['ADMIN'],
      expanded: false,
      children: [
        {
          id: 'system-settings',
          routerLink: '/sistem-ayarlari',
          icon: 'tune',
          title: 'Genel Sistem Ayarları',
          permissions: ['ADMIN']
        },
        {
          id: 'email-smtp-settings',
          routerLink: '/email-smtp-ayarlari',
          icon: 'mail_outline',
          title: 'Duyuru SMTP Ayarları',
          permissions: ['ADMIN']
        },
        {
          id: 'email-categories',
          routerLink: '/email-kategorileri',
          icon: 'category',
          title: 'Duyuru Kategorileri ve İmzalar',
          permissions: ['ADMIN']
        },
        {
          id: 'system-logs',
          routerLink: '/sistem-loglari',
          icon: 'description',
          title: 'Sistem Logları',
          permissions: ['ADMIN']
        }
      ]
    }
  ];

  ngOnInit(): void {
    const user = this.currentUser;
    if (!user?.role) return;
    this.initializeMenus();
  }

  private initializeMenus(): void {
    const user = this.currentUser;
    if (!user?.role) return;

    this.menuSections = this.menuConfiguration
      .map(section => ({
        ...section,
        visible: this.hasPermission(section.permissions, user.role),
        children: section.children.filter(item => this.hasPermission(item.permissions, user.role))
      }))
      .filter(section => section.visible && section.children.length > 0);
  }

  private hasPermission(permissions: string[], userRole: string): boolean {
    if (!permissions || permissions.length === 0) return true;
    return permissions.some(permission => userRole.includes(permission));
  }

  onPanelOpened(sectionId: string): void {
    this.menuSections.forEach(section => {
      if (section.id !== sectionId) {
        section.expanded = false;
      } else {
        section.expanded = true;
      }
    });
  }

  onPanelClosed(sectionId: string): void {
    const section = this.menuSections.find(s => s.id === sectionId);
    if (section) {
      section.expanded = false;
    }
  }

  yardim(){
    // Yardım modal veya sayfası açılabilir
  }

  getRoleName(): string {
    const role = this.currentUser?.role;
    if (!role) return '';

    switch (role) {
      case 'ADMIN': return 'Admin';
      case 'MANAGER': return 'Yönetici';
      case 'COORDINATOR': return 'Kontrolör';
      case 'EDITOR': return 'Editör';
      case 'VIEWER': return 'Görüntüleyici';
      default: return role;
    }
  }

  // TrackBy functions for better performance
  trackBySectionId(index: number, section: MenuSection): string {
    return section.id;
  }

  trackByItemId(index: number, item: MenuItem): string {
    return item.id;
  }
}
