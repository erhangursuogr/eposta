import { Component, signal, computed, inject, effect } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SystemLogsService } from '../../../../services/system-logs.service';
import { LoginLog, SystemLog, EmailLog } from '../../../../common/models/system-logs.model';

@Component({
  selector: 'app-system-logs-list',
  standalone: true,
  imports: [
    FormsModule,
    MatTabsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
],
  templateUrl: './system-logs-list.html',
  styleUrl: './system-logs-list.css'
})
export class SystemLogsList {
  private logsService = inject(SystemLogsService);

  // Current tab
  activeTab = signal<'login' | 'system' | 'email'>('login');

  // Login logs state
  loginLogs = signal<LoginLog[]>([]);
  loginLoading = signal(false);
  loginTotal = signal(0);
  loginPage = signal(1);
  loginPageSize = signal(50);

  // Login filters - separate writable signals
  loginStartDate = signal<Date | null>(null);
  loginEndDate = signal<Date | null>(null);
  loginSearch = signal('');
  loginType = signal('');
  loginFailedOnly = signal(false);

  // System logs state
  systemLogs = signal<SystemLog[]>([]);
  systemLoading = signal(false);
  systemTotal = signal(0);
  systemPage = signal(1);
  systemPageSize = signal(50);

  // System filters - separate writable signals
  systemStartDate = signal<Date | null>(null);
  systemEndDate = signal<Date | null>(null);
  systemSearch = signal('');
  systemLogLevel = signal('');
  systemCategory = signal('');
  systemErrorsOnly = signal(false);

  // Email logs state
  emailLogs = signal<EmailLog[]>([]);
  emailLoading = signal(false);
  emailTotal = signal(0);
  emailPage = signal(1);
  emailPageSize = signal(50);

  // Email filters - separate writable signals
  emailStartDate = signal<Date | null>(null);
  emailEndDate = signal<Date | null>(null);
  emailSearch = signal('');
  emailFailedOnly = signal(false);

  // Filter options
  girisTuruOptions = ['LDAP', 'LOCAL', 'API'];
  logSeviyeOptions = ['INFO', 'WARNING', 'ERROR'];
  kategoriOptions = ['EMAIL', 'USER', 'SYSTEM', 'GROUP', 'FILE'];

  // Table columns
  loginColumns = ['id', 'kullaniciAdi', 'email', 'ipAdres', 'girisTuru', 'basarili', 'girisTarihi', 'hataMesaji'];
  systemColumns = ['id', 'kullaniciAdi', 'logSeviye', 'kategori', 'islem', 'detay', 'ipAdres', 'logTarihi'];
  emailColumns = ['id', 'duyuruKonu', 'aliciEmail', 'aliciAdSoyad', 'aliciKategorisi', 'gonderimBasarili', 'gonderimTarihi', 'hataMesaji'];

  constructor() {
    // Load initial data
    this.loadLoginLogs();
  }

  // Tab change
  onTabChange(index: number): void {
    if (index === 0) {
      this.activeTab.set('login');
      if (this.loginLogs().length === 0) this.loadLoginLogs();
    } else if (index === 1) {
      this.activeTab.set('system');
      if (this.systemLogs().length === 0) this.loadSystemLogs();
    } else {
      this.activeTab.set('email');
      if (this.emailLogs().length === 0) this.loadEmailLogs();
    }
  }

  // Load login logs
  loadLoginLogs(): void {
    this.loginLoading.set(true);

    this.logsService.getLoginLogs({
      sayfa: this.loginPage(),
      sayfaBoyutu: this.loginPageSize(),
      baslangicTarihi: this.loginStartDate()?.toISOString(),
      bitisTarihi: this.loginEndDate()?.toISOString(),
      arama: this.loginSearch() || undefined,
      girisTuru: this.loginType() || undefined,
      sadeceBasarisiz: this.loginFailedOnly() ? true : undefined
    }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.loginLogs.set(response.data.items);
          this.loginTotal.set(response.data.toplamKayit);
        }
        this.loginLoading.set(false);
      },
      error: () => this.loginLoading.set(false)
    });
  }

  // Load system logs
  loadSystemLogs(): void {
    this.systemLoading.set(true);

    this.logsService.getSystemLogs({
      sayfa: this.systemPage(),
      sayfaBoyutu: this.systemPageSize(),
      baslangicTarihi: this.systemStartDate()?.toISOString(),
      bitisTarihi: this.systemEndDate()?.toISOString(),
      arama: this.systemSearch() || undefined,
      logSeviye: this.systemLogLevel() || undefined,
      kategori: this.systemCategory() || undefined,
      sadeceHata: this.systemErrorsOnly() ? true : undefined,
    }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          let logs = response.data.items;

          // Client-side filtreleme: Sadece hatalı loglar
          // if (this.systemErrorsOnly()) {
          //   logs = logs.filter(log => {
          //     const level = log.logSeviye?.toUpperCase();
          //     return level === 'ERROR' || level === 'WARNING' || level === 'WARN';
          //   });
          // }

          this.systemLogs.set(logs);
          this.systemTotal.set(this.systemErrorsOnly() ? logs.length : response.data.toplamKayit);
        }
        this.systemLoading.set(false);
      },
      error: () => this.systemLoading.set(false)
    });
  }

  // Load email logs
  loadEmailLogs(): void {
    this.emailLoading.set(true);

    this.logsService.getEmailLogs({
      sayfa: this.emailPage(),
      sayfaBoyutu: this.emailPageSize(),
      baslangicTarihi: this.emailStartDate()?.toISOString(),
      bitisTarihi: this.emailEndDate()?.toISOString(),
      arama: this.emailSearch() || undefined,
      sadeceBasarisiz: this.emailFailedOnly() ? true : undefined
    }).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.emailLogs.set(response.data.items);
          this.emailTotal.set(response.data.toplamKayit);
        }
        this.emailLoading.set(false);
      },
      error: () => this.emailLoading.set(false)
    });
  }

  // Pagination handlers
  onLoginPageChange(event: PageEvent): void {
    this.loginPage.set(event.pageIndex + 1);
    this.loginPageSize.set(event.pageSize);
    this.loadLoginLogs();
  }

  onSystemPageChange(event: PageEvent): void {
    this.systemPage.set(event.pageIndex + 1);
    this.systemPageSize.set(event.pageSize);
    this.loadSystemLogs();
  }

  onEmailPageChange(event: PageEvent): void {
    this.emailPage.set(event.pageIndex + 1);
    this.emailPageSize.set(event.pageSize);
    this.loadEmailLogs();
  }

  // Filter actions
  applyLoginFilter(): void {
    this.loginPage.set(1);
    this.loadLoginLogs();
  }

  clearLoginFilter(): void {
    this.loginStartDate.set(null);
    this.loginEndDate.set(null);
    this.loginSearch.set('');
    this.loginType.set('');
    this.loginFailedOnly.set(false);
    this.loginPage.set(1);
    this.loadLoginLogs();
  }

  applySystemFilter(): void {
    this.systemPage.set(1);
    this.loadSystemLogs();
  }

  clearSystemFilter(): void {
    this.systemStartDate.set(null);
    this.systemEndDate.set(null);
    this.systemSearch.set('');
    this.systemLogLevel.set('');
    this.systemCategory.set('');
    this.systemErrorsOnly.set(false);
    this.systemPage.set(1);
    this.loadSystemLogs();
  }

  applyEmailFilter(): void {
    this.emailPage.set(1);
    this.loadEmailLogs();
  }

  clearEmailFilter(): void {
    this.emailStartDate.set(null);
    this.emailEndDate.set(null);
    this.emailSearch.set('');
    this.emailFailedOnly.set(false);
    this.emailPage.set(1);
    this.loadEmailLogs();
  }

  // Helper methods
  formatDate(date: string): string {
    return new Date(date).toLocaleString('tr-TR');
  }

  getLogSeviyeClass(seviye: string): string {
    switch (seviye?.toUpperCase()) {
      case 'TRACE': return 'log-trace';
      case 'DEBUG': return 'log-debug';
      case 'INFO': return 'log-info';
      case 'WARN':
      case 'WARNING': return 'log-warning';
      case 'ERROR': return 'log-error';
      case 'FATAL':
      case 'CRITICAL': return 'log-critical';
      default: return '';
    }
  }

  getStatusClass(success: boolean): string {
    return success ? 'status-success' : 'status-error';
  }

  getStatusText(success: boolean): string {
    return success ? 'Başarılı' : 'Başarısız';
  }

  getStatusIcon(success: boolean): string {
    return success ? 'check_circle' : 'cancel';
  }
}
