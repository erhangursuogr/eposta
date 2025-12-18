import { Component, inject, signal, computed, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SharedModule } from '../../common/shared/shared.module';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatBadgeModule } from '@angular/material/badge';
import { StatCardComponent } from '../../common/components/stat-card/stat-card';
import { EmptyStateComponent } from '../common/empty-state/empty-state.component';
import { DashboardService } from '../../services/dashboard.service';
import { UserDataService } from '../../services/userdata.service';
import { RecentActivity, GroupStats, SystemHealth, TopUser } from '../../common/models/dashboard.model';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    SharedModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule,
    MatBadgeModule,
    StatCardComponent,
    EmptyStateComponent
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private userDataService = inject(UserDataService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  // Signals
  user = computed(() => this.userDataService.user());
  stats = computed(() => this.dashboardService.stats());
  recentActivities = signal<RecentActivity[]>([]);
  groupStats = signal<GroupStats[]>([]);
  systemHealth = signal<SystemHealth | null>(null);
  topUsers = signal<TopUser[]>([]);
  loading = signal<boolean>(true);

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.loading.set(true);

    // Load stats - Memory leak fix: takeUntilDestroyed added
    this.dashboardService.getDashboardStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (err) => console.error('Error loading stats:', err),
        complete: () => this.loading.set(false)
      });

    // Load recent activities - Memory leak fix: takeUntilDestroyed added
    this.dashboardService.getRecentActivities(10)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.recentActivities.set(response.data);
          }
        },
        error: (err) => console.error('Error loading activities:', err)
      });

    // Load group stats - Memory leak fix: takeUntilDestroyed added
    this.dashboardService.getGroupStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.groupStats.set(response.data.slice(0, 5)); // Top 5
          }
        },
        error: (err) => console.error('Error loading group stats:', err)
      });

    // Load top users - Memory leak fix: takeUntilDestroyed added
    this.dashboardService.getTopUsers(5)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.topUsers.set(response.data);
          }
        },
        error: (err) => console.error('Error loading top users:', err)
      });

    // Load system health (ADMIN only) - Memory leak fix: takeUntilDestroyed added
    if (this.user().role === 'ADMIN') {
      this.dashboardService.getSystemHealth()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (response) => {
            if (response.success && response.data) {
              this.systemHealth.set(response.data);
            }
          },
          error: (err) => console.error('Error loading system health:', err)
        });
    }
  }

  // Navigation helpers
  goToNewAnnouncement(): void {
    this.router.navigate(['/duyuru-olustur']);
  }

  goToUserManagement(): void {
    this.router.navigate(['/kullanici-yonetimi/yeni']);
  }

  goToPendingApprovals(): void {
    this.router.navigate(['/onay-bekleyenler']);
  }

  goToGroups(): void {
    this.router.navigate(['/grup-yonetimi']);
  }

  goToAnnouncements(): void {
    this.router.navigate(['/duyurular']);
  }

  goToSendAnnouncements(): void {
    this.router.navigate(['/gonderim-gecmisi']);
  }

  goToUsers(): void {
    this.router.navigate(['/kullanici-yonetimi']);
  }

  goToActivityLogs(): void {
    this.router.navigate(['/sistem-loglari']);
  }

  // handleCardClick() {
  // const role = this.user().role;
  // if (role === 'ADMIN' || role === 'COORDINATOR' || role === 'MANAGER') {
  //   this.goToPendingApprovals();
  // } else {
  //   this.goToAnnouncements();
  // }
// }

  // Utility methods
  getActivityIcon(type: string): string {
    switch (type) {
      case 'DUYURU': return 'campaign';
      case 'LOGIN': return 'login';
      default: return 'info';
    }
  }

  getActivityColor(type: string): string {
    switch (type) {
      case 'DUYURU': return 'primary';
      case 'LOGIN': return 'accent';
      default: return 'default';
    }
  }

  getGroupTypeLabel(type: string): string {
    switch (type) {
      case 'MANUEL':
        return 'Manuel';
      case 'DOSYA':
        return 'Dosya';
      case 'DINAMIK': return 'Dinamik';
      case 'DEBIS': return 'Debis';
      default: return type;
    }
  }

  getGroupTypeColor(type: string): string {
    switch (type) {
      case 'MANUEL':
        return 'default';
      case 'DOSYA':
        return 'primary';
      case 'DINAMIK': return 'accent';
      case 'DEBIS': return 'warn';
      default: return 'default';
    }
  }

  getHealthStatusColor(status: string): string {
    switch (status) {
      case 'HEALTHY': return 'success';
      case 'WARNING': return 'warn';
      case 'ERROR': return 'danger';
      default: return 'default';
    }
  }

  getHealthStatusIcon(status: string): string {
    switch (status) {
      case 'HEALTHY': return 'check_circle';
      case 'WARNING': return 'warning';
      case 'ERROR': return 'error';
      default: return 'help';
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return 'Az önce';
    if (minutes < 60) return `${minutes} dakika önce`;
    if (hours < 24) return `${hours} saat önce`;
    if (days < 7) return `${days} gün önce`;

    return date.toLocaleDateString('tr-TR');
  }

  getUserInitials(name: string): string {
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }
}
