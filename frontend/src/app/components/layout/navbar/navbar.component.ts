import { Component, EventEmitter, Output, inject, computed, signal, DestroyRef } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter, Observable, of, switchMap, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SharedModule } from '../../../common/shared/shared.module';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { UserDataService } from '../../../services/userdata.service';
import { DashboardService } from '../../../services/dashboard.service';
import { AuthService } from '../../../services/auth.service';

import { environment } from '../../../../environments/environment';
import Swal from 'sweetalert2';

interface Breadcrumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    SharedModule,
    RouterModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
    MatMenuModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatChipsModule
],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  @Output() toggleSidebar = new EventEmitter<void>();

  private _userDataService = inject(UserDataService);
  private _dashboardService = inject(DashboardService);
  private _authService = inject(AuthService);
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  user = computed(() => this._userDataService.user());

  // PERFORMANS: Computed signals (getter yerine) - Her CD cycle'da hesaplanmaz
  currentUser = this._userDataService.user;

  userInitials = computed(() => {
    const user = this.currentUser();
    const firstName = user?.name?.charAt(0) || '';
    const lastName = user?.surname?.charAt(0) || '';
    return (firstName + lastName).toUpperCase();
  });

  userFullName = computed(() => {
    const user = this.currentUser();
    return `${user?.name || ''} ${user?.surname || ''}`.trim();
  });

  userRoleName = computed(() => {
    const role = this.currentUser()?.role;
    switch (role) {
      case 'ADMIN': return 'Yönetici';
      case 'MANAGER': return 'Onaylayıcı';
      case 'COORDINATOR': return 'Kontrolör';
      case 'EDITOR': return 'Editör';
      case 'VIEWER': return 'Görüntüleyici';
      default: return 'Kullanıcı';
    }
  });

  userRoleBadgeColor = computed(() => {
    const role = this.currentUser()?.role;
    switch (role) {
      case 'ADMIN': return 'admin-badge';
      case 'MANAGER': return 'manager-badge';
      case 'COORDINATOR': return 'coordinator-badge';
      case 'EDITOR': return 'editor-badge';
      case 'VIEWER': return 'viewer-badge';
      default: return 'viewer-badge';
    }
  });

  userRoleIcon = computed(() => {
    const role = this.currentUser()?.role;
    if (role === 'ADMIN') return 'shield';
    if (role === 'MANAGER') return 'verified_user';
    if (role === 'COORDINATOR') return 'supervisor_account';
    return 'person';
  });

  // Development mode check for admin dashboards
  isDevelopment = !environment.production;

  pageTitle = signal<string>('');
  pageDescription = signal<string>('');
  breadcrumbs = signal<Breadcrumb[]>([]);

  // Pending approval count - UserDataService'den signal kullan (reactive)
  pendingApprovalCount = computed(() => this._userDataService.pendingApprovalsCount());

  // Search
  searchQuery = signal<string>('');
  showSearch = signal<boolean>(false);

  ngOnInit(): void {
    this.updatePageInfo();
    this.loadDashboardStats();

    // İlk yüklemede bildirim sayısını çek
    this._userDataService.refreshPendingApprovals();

    // Memory leak fix: takeUntilDestroyed added
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      tap(() => {
        this.updatePageInfo();
        this.updateBreadcrumbs();
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();
  }

  private updatePageInfo(): void {
    // Memory leak fix: takeUntilDestroyed added
    this.getRouteData(this.activatedRoute.root)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ title, description }) => {
        this.pageTitle.set(title || 'DEÜ Duyuru Yönetim Sistemi');
        this.pageDescription.set(description || '');
      });
  }

  private updateBreadcrumbs(): void {
    const breadcrumbs: Breadcrumb[] = [];
    let currentRoute = this.activatedRoute.root;

    breadcrumbs.push({ label: 'Ana Sayfa', url: '/' });

    while (currentRoute.firstChild) {
      currentRoute = currentRoute.firstChild;
      const routeData = currentRoute.snapshot.data;
      const routeTitle = routeData['title'];

      if (routeTitle && routeTitle !== 'Ana Sayfa') {
        const url = currentRoute.snapshot.url.map(segment => segment.path).join('/');
        breadcrumbs.push({ label: routeTitle, url: `/${url}` });
      }
    }

    this.breadcrumbs.set(breadcrumbs);
  }

  private getRouteData(route: ActivatedRoute): Observable<{ title: string; description: string }> {
    while (route.firstChild) {
      route = route.firstChild;
    }
    return route.data.pipe(
      switchMap(data => {
        const routeTitle = data['title'];
        const routeDescription = data['description'];
        return of({
          title: routeTitle,
          description: routeDescription
        });
      })
    );
  }

  private loadDashboardStats(): void {
    this._dashboardService.getDashboardStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (err) => console.error('Error loading dashboard stats in navbar:', err)
      });
  }

  // TrackBy function for breadcrumbs
  trackByBreadcrumb(index: number, item: Breadcrumb): string {
    return item.url;
  }

  toggleSearchBar(): void {
    this.showSearch.update(v => !v);
    if (!this.showSearch()) {
      this.searchQuery.set('');
    }
  }

  onSearch(): void {
    const query = this.searchQuery();
    if (query.trim()) {
      // TODO: Global arama sayfasına yönlendir
      // this.router.navigate(['/arama'], { queryParams: { q: query } });
    }
  }

  goToProfile(): void {
    // Loading göster
    Swal.fire({
      title: 'Yükleniyor...',
      allowOutsideClick: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });

    // API'den güncel kullanıcı bilgilerini al
    this._authService.getCurrentUser().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const userInfo = response.data;
          const userName = userInfo.kullaniciAdi || userInfo.email?.split('@')[0] || '';
          const fullName = userInfo.adSoyad || `${userInfo.name || ''} ${userInfo.surname || ''}`.trim();
          const roleName = this.getRoleDisplayName(userInfo.rol);

          Swal.fire({
            title: '<strong>Kullanıcı Profili</strong>',
            html: `
              <div style="text-align: left; padding: 20px;">
                <div style="margin-bottom: 20px; text-align: center;">
                  <div style="width: 80px; height: 80px; border-radius: 50%; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                              display: inline-flex; align-items: center; justify-content: center; color: white; font-size: 32px; font-weight: bold; margin-bottom: 10px;">
                    ${this.userInitials()}
                  </div>
                  <h3 style="margin: 10px 0 5px 0; color: #333;">${fullName}</h3>
                  <span style="display: inline-block; padding: 4px 12px; background: #e3f2fd; color: #1976d2;
                               border-radius: 12px; font-size: 12px; font-weight: 600;">${roleName}</span>
                </div>

                <div style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 10px;">
                  <div style="display: flex; align-items: center;">
                    <span style="width: 24px; height: 24px; color: #666; margin-right: 10px;">📧</span>
                    <div>
                      <div style="font-size: 12px; color: #666;">Email</div>
                      <div style="font-weight: 500; color: #333;">${userInfo.email}</div>
                    </div>
                  </div>
                </div>

                <div style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 10px;">
                  <div style="display: flex; align-items: center;">
                    <span style="width: 24px; height: 24px; color: #666; margin-right: 10px;">👤</span>
                    <div>
                      <div style="font-size: 12px; color: #666;">Kullanıcı Adı</div>
                      <div style="font-weight: 500; color: #333;">${userName}</div>
                    </div>
                  </div>
                </div>

                ${userInfo.gorevYeriAdi ? `
                  <div style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 10px;">
                    <div style="display: flex; align-items: center;">
                      <span style="width: 24px; height: 24px; color: #666; margin-right: 10px;">🏛️</span>
                      <div>
                        <div style="font-size: 12px; color: #666;">Görev Yeri</div>
                        <div style="font-weight: 500; color: #333;">${userInfo.gorevYeriAdi}</div>
                      </div>
                    </div>
                  </div>
                ` : ''}

                ${userInfo.departman ? `
                  <div style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 10px;">
                    <div style="display: flex; align-items: center;">
                      <span style="width: 24px; height: 24px; color: #666; margin-right: 10px;">🏢</span>
                      <div>
                        <div style="font-size: 12px; color: #666;">Departman</div>
                        <div style="font-weight: 500; color: #333;">${userInfo.departman}</div>
                      </div>
                    </div>
                  </div>
                ` : ''}

                ${userInfo.unvan ? `
                  <div style="background: #f5f5f5; padding: 15px; border-radius: 8px;">
                    <div style="display: flex; align-items: center;">
                      <span style="width: 24px; height: 24px; color: #666; margin-right: 10px;">🎓</span>
                      <div>
                        <div style="font-size: 12px; color: #666;">Ünvan</div>
                        <div style="font-weight: 500; color: #333;">${userInfo.unvan}</div>
                      </div>
                    </div>
                  </div>
                ` : ''}
              </div>
            `,
            icon: undefined,
            showConfirmButton: true,
            confirmButtonText: 'Kapat',
            confirmButtonColor: '#1976d2',
            width: '500px'
          });
        }
      },
      error: (err) => {
        console.error('Error fetching user profile:', err);
        Swal.fire({
          icon: 'error',
          title: 'Hata',
          text: 'Kullanıcı bilgileri alınamadı',
          confirmButtonText: 'Tamam',
          confirmButtonColor: '#1976d2'
        });
      }
    });
  }

  private getRoleDisplayName(role: string): string {
    switch (role) {
      case 'ADMIN': return 'Yönetici';
      case 'MANAGER': return 'Onaylayıcı';
      case 'COORDINATOR': return 'Kontrolör';
      case 'EDITOR': return 'Editör';
      case 'VIEWER': return 'Görüntüleyici';
      default: return 'Kullanıcı';
    }
  }

  goToSettings(): void {
    this.router.navigate(['/sistem-ayarlari']);
  }

  goToPendingApprovals(): void {
    this.router.navigate(['/onay-bekleyenler']);
  }

  openHangfireDashboard(): void {
    // Backend Hangfire dashboard'u yeni sekmede aç
    window.open(environment.hangfireUrl, '_blank');
  }

  openSeqDashboard(): void {
    // Seq dashboard'u yeni sekmede aç
    window.open(environment.seqUrl, '_blank');
  }

  logout(): void {
    this._userDataService.logout();
  }
}
