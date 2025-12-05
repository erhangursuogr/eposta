import { Component, OnInit, signal, computed, inject, DestroyRef } from '@angular/core';

import { FormsModule, ReactiveFormsModule, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { finalize } from 'rxjs/operators';
import { EmptyStateComponent } from '../../common/empty-state/empty-state.component';
import { LoadingComponent } from '../../common/loading/loading.component';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog } from '@angular/material/dialog';
import { ToastrService } from 'ngx-toastr';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import Swal from 'sweetalert2';
import ExcelJS from 'exceljs';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

import { AnnouncementService } from '../../../services/announcement.service';
import { UserDataService } from '../../../services/userdata.service';
import { AnnouncementPreviewDialog } from '../announcement-preview-dialog/announcement-preview-dialog';
import {
  Announcement,
  AnnouncementStatus,
  AnnouncementListParams,
  getStatusColor,
  getStatusLabel,
  getStatusIcon
} from '../../../common/models/announcement.model';

@Component({
  selector: 'app-announcement-list',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatChipsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
    MatButtonToggleModule,
    MatBadgeModule,
    MatDividerModule,
    EmptyStateComponent,
    LoadingComponent
],
  templateUrl: './announcement-list.html',
  styleUrl: './announcement-list.css'
})
export class AnnouncementList implements OnInit {
  private announcementService = inject(AnnouncementService);
  private userDataService = inject(UserDataService);
  private toastr = inject(ToastrService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private breakpointObserver = inject(BreakpointObserver);
  private dialog = inject(MatDialog);

  // User info
  user = computed(() => this.userDataService.user());

  // View state
  viewMode = signal<'table' | 'card'>('table');
  loading = signal<boolean>(false);

  // Data
  announcements = signal<Announcement[]>([]);

  // BACKEND FILTRELEME (Gemini Audit Fix): Client-side filtreleme kaldırıldı
  // Tüm filtreler backend'e taşındı - artık 2. sayfadaki kayıtları da bulabiliriz
  filteredAnnouncements = computed(() => this.announcements());

  // Pagination
  totalCount = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = signal<number>(20);
  pageSizeOptions = [10, 20, 50, 100];

  // Filters
  searchControl = new FormControl('');
  searchTerm = signal<string>(''); // Signal for reactive filtering
  statusFilter = signal<AnnouncementStatus | ''>('');
  startDateFilter = signal<Date | null>(null);
  endDateFilter = signal<Date | null>(null);
  onlyMineFilter = signal<boolean>(false);

  // Table columns
  displayedColumns = computed(() => {
    const baseColumns = ['konu', 'durum', 'olusturanKullaniciAdi', 'olusturmaTarihi', 'aliciSayisi'];
    const role = this.user().role;

    if (role === 'ADMIN' || role === 'MANAGER') {
      return [...baseColumns, 'actions'];
    }
    return [...baseColumns, 'actions'];
  });

  // Status options for filter
  statusOptions = [
    { value: '', label: 'Tüm Durumlar' },
    { value: AnnouncementStatus.TASLAK, label: 'Taslak' },
    { value: AnnouncementStatus.ILK_ONAY_BEKLIYOR, label: 'İlk Onay Bekliyor' },
    { value: AnnouncementStatus.SON_ONAY_BEKLIYOR, label: 'Son Onay Bekliyor' },
    { value: AnnouncementStatus.ONAYLANDI, label: 'Onaylandı' },
    { value: AnnouncementStatus.GONDERILDI, label: 'Gönderildi' },
    { value: AnnouncementStatus.IPTAL, label: 'İptal' }
  ];


  // Expose helper functions to template
  getStatusColor = getStatusColor;
  getStatusLabel = getStatusLabel;
  getStatusIcon = getStatusIcon;

  getStatusTooltip(announcement: Announcement): string {
    if (announcement.durum === AnnouncementStatus.ONAYLANDI && announcement.onayNotu) {
      return `Onay Notu: ${announcement.onayNotu}`;
    }
    if (announcement.durum === AnnouncementStatus.REDDEDILDI && announcement.redNedeni) {
      return `Red Nedeni: ${announcement.redNedeni}`;
    }
    return '';
  }

  ngOnInit(): void {
    this.setupSearchDebounce();
    this.setupResponsiveView();
    this.loadAnnouncements();
  }

  private setupResponsiveView(): void {
    this.breakpointObserver
      .observe([Breakpoints.HandsetPortrait, Breakpoints.TabletPortrait])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.matches) {
          // Mobil veya tablet: Kart görünümü
          this.viewMode.set('card');
        } else {
          // PC: Tablo görünümü
          this.viewMode.set('table');
        }
      });
  }

  private setupSearchDebounce(): void {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((value) => {
        this.searchTerm.set(value || '');
        this.currentPage.set(1); // Reset to first page
        this.loadAnnouncements(); // BACKEND FILTRELEME: Backend'den yeniden yükle
      });
  }

  loadAnnouncements(): void {
    this.loading.set(true);

    const params: AnnouncementListParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      onlyMine: this.onlyMineFilter(),
      sortBy: 'olusturmaTarihi',
      sortOrder: 'desc',
      // BACKEND FILTRELEME (Gemini Audit Fix): Filtreler backend'e gönderiliyor
      searchTerm: this.searchTerm() || undefined,
      durum: this.statusFilter() || undefined,
      startDate: this.startDateFilter() || undefined,
      endDate: this.endDateFilter() || undefined
    };

    this.announcementService.getAnnouncements(params)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.announcements.set(response.data);
            this.totalCount.set(response.totalCount || 0);
          } else {
            this.toastr.error(response.message || 'Duyurular yüklenemedi');
          }
        },
        error: (error) => {
          const errorMessage = error?.error?.message || 'Duyurular yüklenirken hata oluştu';
          this.toastr.error(errorMessage);
        }
      });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadAnnouncements();
  }

  onStatusFilterChange(status: AnnouncementStatus | ''): void {
    this.statusFilter.set(status);
    this.currentPage.set(1); // Reset to first page
    this.loadAnnouncements(); // BACKEND FILTRELEME: Backend'den yeniden yükle
  }

  onDateRangeChange(): void {
    this.currentPage.set(1); // Reset to first page
    this.loadAnnouncements(); // BACKEND FILTRELEME: Backend'den yeniden yükle
  }

  toggleOnlyMine(): void {
    this.onlyMineFilter.set(!this.onlyMineFilter());
    this.currentPage.set(1);
    this.loadAnnouncements();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.statusFilter.set('');
    this.startDateFilter.set(null);
    this.endDateFilter.set(null);
    this.onlyMineFilter.set(false);
    this.currentPage.set(1);
    this.loadAnnouncements();
  }

  toggleViewMode(): void {
    this.viewMode.set(this.viewMode() === 'table' ? 'card' : 'table');
  }

  // Action methods
  canEdit(announcement: Announcement): boolean {
    const role = this.user().role;
    const isOwner = announcement.olusturanKullaniciId === this.user().id;

    // TASLAK veya REDDEDILDI durumundaki duyurular düzenlenebilir
    return (
      (role === 'ADMIN' || role === 'MANAGER' || (role === 'EDITOR' && isOwner)) &&
      (announcement.durum === AnnouncementStatus.TASLAK ||
       announcement.durum === AnnouncementStatus.REDDEDILDI)
    );
  }

  canDelete(announcement: Announcement): boolean {
    // Sadece TASLAK durumundaki duyurular silinebilir
    if (announcement.durum !== AnnouncementStatus.TASLAK) {
      return false;
    }

    // Daha önce gönderilmiş veya onaylanmış duyurular silinemez
    if (announcement.gonderimTarihi || announcement.onayTarihi) {
      return false;
    }

    const role = this.user().role;
    const isOwner = announcement.olusturanKullaniciId === this.user().id;

    return (
      (role === 'ADMIN') ||
      (role === 'MANAGER') ||
      (role === 'EDITOR' && isOwner)
    );
  }

  canApprove(announcement: Announcement): boolean {
    const role = this.user().role;

    // Koordinatör: ILK_ONAY_BEKLIYOR durumunu onaylayabilir
    if (role === 'COORDINATOR' && announcement.durum === AnnouncementStatus.ILK_ONAY_BEKLIYOR) {
      return true;
    }

    // Manager: SON_ONAY_BEKLIYOR durumunu onaylayabilir
    if (role === 'MANAGER' && announcement.durum === AnnouncementStatus.SON_ONAY_BEKLIYOR) {
      return true;
    }

    return false;
  }

  isViewer(): boolean {
    return this.user().role === 'VIEWER';
  }

  goToApprovals(): void {
    this.router.navigate(['/onay-bekleyenler']);
  }

  canSubmitForApproval(announcement: Announcement): boolean {
    const role = this.user().role;
    const isOwner = announcement.olusturanKullaniciId === this.user().id;

    return (
      (role === 'ADMIN' || role === 'MANAGER' || role === 'EDITOR') &&
      isOwner &&
      (announcement.durum === AnnouncementStatus.TASLAK || announcement.durum === AnnouncementStatus.REDDEDILDI)
    );
  }

  canReject(announcement: Announcement): boolean {
    const role = this.user().role;
    return (role === 'ADMIN' || role === 'MANAGER') && announcement.durum === AnnouncementStatus.ONAY_BEKLIYOR;
  }

  canSend(announcement: Announcement): boolean {
    const role = this.user().role;
    return role === 'ADMIN' && announcement.durum === AnnouncementStatus.ONAYLANDI;
  }

  canCancel(announcement: Announcement): boolean {
    const role = this.user().role;
    return (
      (role === 'ADMIN' || role === 'MANAGER') &&
      (announcement.durum === AnnouncementStatus.ONAY_BEKLIYOR || announcement.durum === AnnouncementStatus.ONAYLANDI)
    );
  }

  viewDetails(id: number): void {
    // TODO: Duyuru detay sayfası oluşturulacak
    // Geçici olarak düzenleme sayfasına yönlendir
    this.router.navigate(['/duyuru-duzenle', id]);
  }

  createNew(): void {
    this.router.navigate(['/duyuru-olustur']);
  }

  openPreview(id: number): void {
    this.dialog.open(AnnouncementPreviewDialog, {
      data: { announcementId: id },
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      panelClass: 'preview-dialog-container'
    });
  }

  editAnnouncement(id: number): void {
    this.router.navigate(['/duyuru-duzenle', id]);
  }

  scheduleAnnouncement(id: number): void {
    this.router.navigate(['/zamanli-duyurular'], { queryParams: { duyuruId: id } });
  }

  duplicateAnnouncement(id: number): void {
    Swal.fire({
      title: 'Duyuru Kopyala',
      text: 'Bu duyurunun bir kopyası oluşturulacak. Onaylıyor musunuz?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Kopyala',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#004B87'
    }).then((result) => {
      if (result.isConfirmed) {
        this.announcementService.duplicateAnnouncement(id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this.toastr.success('Duyuru başarıyla kopyalandı');
                this.loadAnnouncements();
              } else {
                this.toastr.error(response.message || 'Duyuru kopyalanamadı');
              }
            },
            error: (error) => {
              const errorMessage = error?.error?.message || 'Duyuru kopyalanırken hata oluştu';
              this.toastr.error(errorMessage);
            }
          });
      }
    });
  }

  // Koordinatör ve Manager için buton metni
  getDuplicateButtonText(): string {
    return 'Kopyala';
  }

  getDuplicateButtonIcon(): string {
    return 'content_copy';
  }

  deleteAnnouncement(id: number): void {
    Swal.fire({
      title: 'Duyuru Sil',
      text: 'Bu duyuruyu silmek istediğinizden emin misiniz?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Sil',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed) {
        this.announcementService.deleteAnnouncement(id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this.toastr.success('Duyuru başarıyla silindi');
                this.loadAnnouncements();
              } else {
                this.toastr.error(response.message || 'Duyuru silinemedi');
              }
            },
            error: (error) => {
              const errorMessage = error?.error?.message || 'Duyuru silinirken hata oluştu';
              this.toastr.error(errorMessage);
            }
          });
      }
    });
  }

  submitForApproval(id: number): void {
    Swal.fire({
      title: 'Onaya Gönder',
      text: 'Bu duyuruyu onaya göndermek istediğinizden emin misiniz?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Gönder',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#004B87'
    }).then((result) => {
      if (result.isConfirmed) {
        this.announcementService.submitForApproval(id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this.toastr.success('Duyuru onaya gönderildi');
                this.loadAnnouncements();
              } else {
                this.toastr.error(response.message || 'Duyuru onaya gönderilemedi');
              }
            },
            error: (error) => {
              const errorMessage = error?.error?.message || 'Duyuru onaya gönderilirken hata oluştu';
              this.toastr.error(errorMessage);
            }
          });
      }
    });
  }

  approveAnnouncement(id: number): void {
    Swal.fire({
      title: 'Duyuru Onayla',
      text: 'Bu duyuruyu onaylamak istediğinizden emin misiniz?',
      input: 'textarea',
      inputLabel: 'Onay Notu (İsteğe bağlı)',
      inputPlaceholder: 'Onay notunuzu buraya yazın...',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Onayla',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#28a745'
    }).then((result) => {
      if (result.isConfirmed) {
        this.announcementService.approveAnnouncement({
          duyuruId: id,
          onayNotu: result.value || undefined
        })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this.toastr.success('Duyuru onaylandı');
                this.loadAnnouncements();
              } else {
                this.toastr.error(response.message || 'Duyuru onaylanamadı');
              }
            },
            error: (error) => {
              const errorMessage = error?.error?.message || 'Duyuru onaylanırken hata oluştu';
              this.toastr.error(errorMessage);
            }
          });
      }
    });
  }

  rejectAnnouncement(id: number): void {
    Swal.fire({
      title: 'Duyuru Reddet',
      text: 'Red nedenini belirtiniz:',
      input: 'textarea',
      inputLabel: 'Red Nedeni',
      inputPlaceholder: 'Red nedenini buraya yazın...',
      inputValidator: (value) => {
        if (!value) {
          return 'Red nedeni zorunludur!';
        }
        return null;
      },
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Reddet',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.announcementService.rejectAnnouncement({
          duyuruId: id,
          redNedeni: result.value
        })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this.toastr.success('Duyuru reddedildi');
                this.loadAnnouncements();
              } else {
                this.toastr.error(response.message || 'Duyuru reddedilemedi');
              }
            },
            error: (error) => {
              const errorMessage = error?.error?.message || 'Duyuru reddedilirken hata oluştu';
              this.toastr.error(errorMessage);
            }
          });
      }
    });
  }

  sendAnnouncement(id: number): void {
    Swal.fire({
      title: 'Duyuru Gönder',
      text: 'Bu duyuru şimdi gönderilecek. Onaylıyor musunuz?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Gönder',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#004B87'
    }).then((result) => {
      if (result.isConfirmed) {
        this.announcementService.sendAnnouncement(id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this.toastr.success('Duyuru gönderildi');
                this.loadAnnouncements();
              } else {
                this.toastr.error(response.message || 'Duyuru gönderilemedi');
              }
            },
            error: (error) => {
              const errorMessage = error?.error?.message || 'Duyuru gönderilirken hata oluştu';
              this.toastr.error(errorMessage);
            }
          });
      }
    });
  }

  cancelAnnouncement(id: number): void {
    Swal.fire({
      title: 'Duyuru İptal',
      text: 'Bu duyuruyu iptal etmek istediğinizden emin misiniz?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, İptal Et',
      cancelButtonText: 'Vazgeç',
      confirmButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed) {
        this.announcementService.cancelAnnouncement(id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this.toastr.success('Duyuru iptal edildi');
                this.loadAnnouncements();
              } else {
                this.toastr.error(response.message || 'Duyuru iptal edilemedi');
              }
            },
            error: (error) => {
              const errorMessage = error?.error?.message || 'Duyuru iptal edilirken hata oluştu';
              this.toastr.error(errorMessage);
            }
          });
      }
    });
  }

  async exportToExcel(): Promise<void> {
    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet('Duyurular');

    // Header row
    worksheet.columns = [
      { header: 'Konu', key: 'konu', width: 40 },
      { header: 'Durum', key: 'durum', width: 20 },
      { header: 'Oluşturan', key: 'olusturan', width: 25 },
      { header: 'Oluşturma Tarihi', key: 'olusturmaTarihi', width: 18 },
      { header: 'Gönderim Tarihi', key: 'gonderimTarihi', width: 18 },
      { header: 'Alıcı Sayısı', key: 'aliciSayisi', width: 12 }
    ];

    // Style header row
    worksheet.getRow(1).font = { bold: true };
    worksheet.getRow(1).fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: 'FF4472C4' }
    };
    worksheet.getRow(1).font = { bold: true, color: { argb: 'FFFFFFFF' } };

    // Add data rows
    this.filteredAnnouncements().forEach(a => {
      worksheet.addRow({
        konu: a.konu || '',
        durum: getStatusLabel(a.durum),
        olusturan: a.olusturanKullaniciAdi || '',
        olusturmaTarihi: this.formatDate(a.olusturmaTarihi),
        gonderimTarihi: a.gonderimTarihi ? this.formatDate(a.gonderimTarihi) : '-',
        aliciSayisi: a.toplamAliciSayisi || 0
      });
    });

    // Generate and download file
    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `duyurular_${new Date().toISOString().split('T')[0]}.xlsx`;
    link.click();
    window.URL.revokeObjectURL(url);
    this.toastr.success('Excel dosyası indirildi');
  }

  exportToPDF(): void {
    const doc = new jsPDF('l', 'mm', 'a4');

    doc.setFontSize(16);
    doc.text('Duyuru Listesi', 14, 15);

    doc.setFontSize(10);
    doc.text('Tarih: ' + new Date().toLocaleDateString('tr-TR'), 14, 22);
    doc.text('Toplam: ' + this.filteredAnnouncements().length + ' duyuru', 14, 28);

    const tableData = this.filteredAnnouncements().map(a => [
      this.toTurkishChars(a.konu || ''),
      this.toTurkishChars(getStatusLabel(a.durum)),
      this.toTurkishChars(a.olusturanKullaniciAdi || ''),
      this.formatDate(a.olusturmaTarihi),
      a.toplamAliciSayisi || 0
    ]);

    autoTable(doc, {
      head: [['Konu', 'Durum', 'Olusturan', 'Olusturma Tarihi', 'Alici']],
      body: tableData,
      startY: 35,
      theme: 'grid',
      styles: {
        fontSize: 8,
        cellPadding: 2
      },
      headStyles: {
        fillColor: [0, 75, 135],
        textColor: 255,
        fontStyle: 'bold'
      },
      alternateRowStyles: {
        fillColor: [245, 245, 245]
      }
    });

    const fileName = 'duyurular_' + new Date().toISOString().split('T')[0] + '.pdf';
    doc.save(fileName);
    this.toastr.success('PDF dosyası indirildi');
  }

  private toTurkishChars(text: string): string {
    if (!text) return '';
    return text
      .replace(/ç/g, 'c').replace(/Ç/g, 'C')
      .replace(/ğ/g, 'g').replace(/Ğ/g, 'G')
      .replace(/ı/g, 'i').replace(/İ/g, 'I')
      .replace(/ö/g, 'o').replace(/Ö/g, 'O')
      .replace(/ş/g, 's').replace(/Ş/g, 'S')
      .replace(/ü/g, 'u').replace(/Ü/g, 'U');
  }


  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('tr-TR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  truncateText(text: string, maxLength: number = 100): string {
    if (!text) return '';
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
  }

  viewHistory(announcementId: number): void {
    this.announcementService.getAnnouncementMovements(announcementId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.showHistoryModal(response.data);
        } else {
          this.toastr.error('Duyuru geçmişi yüklenemedi');
        }
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Duyuru geçmişi yüklenirken hata oluştu';
        this.toastr.error(errorMessage);
      }
    });
  }

  private showHistoryModal(movements: any[]): void {
    const getStatusBadge = (durum: string): {icon: string, color: string, text: string} => {
      switch (durum) {
        case 'TASLAK': return {icon: '📝', color: '#9e9e9e', text: 'Taslak'};
        case 'ILK_ONAY_BEKLIYOR': return {icon: '⏳', color: '#2196f3', text: 'İlk Onay Bekliyor'};
        case 'SON_ONAY_BEKLIYOR': return {icon: '⏱️', color: '#ff9800', text: 'Son Onay Bekliyor'};
        case 'ONAYLANDI': return {icon: '✅', color: '#4caf50', text: 'Onaylandı'};
        case 'REDDEDILDI': return {icon: '❌', color: '#f44336', text: 'Reddedildi'};
        case 'GONDERILDI': return {icon: '📧', color: '#00bcd4', text: 'Gönderildi'};
        default: return {icon: 'ℹ️', color: '#757575', text: durum};
      }
    };

    const getActionIcon = (islemTipi: string): string => {
      switch (islemTipi) {
        case 'OLUSTURMA': return '➕';
        case 'GUNCELLEME': return '✏️';
        case 'ONAYA_GONDERME': return '📤';
        case 'ONAYLAMA': return '✓';
        case 'REDDETME': return '✗';
        case 'GONDERIM': return '📨';
        default: return '•';
      }
    };

    const timelineHtml = movements.map(m => {
      const statusBadge = getStatusBadge(m.yeniDurum);
      const actionIcon = getActionIcon(m.islemTipi);
      const oldStatus = m.oncekiDurum ? getStatusBadge(m.oncekiDurum) : null;

      return `
        <div style="padding: 16px; border-left: 3px solid ${statusBadge.color}; margin-bottom: 16px; background: #f5f5f5; border-radius: 4px;">
          <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
            <span style="font-size: 20px;">${actionIcon}</span>
            <strong style="color: #333;">${m.islemTipi.replace(/_/g, ' ')}</strong>
          </div>

          ${oldStatus ? `
            <div style="margin: 8px 0; font-size: 13px;">
              <span style="background: ${oldStatus.color}; color: white; padding: 2px 8px; border-radius: 3px;">${oldStatus.icon} ${oldStatus.text}</span>
              <span style="margin: 0 8px;">→</span>
              <span style="background: ${statusBadge.color}; color: white; padding: 2px 8px; border-radius: 3px;">${statusBadge.icon} ${statusBadge.text}</span>
            </div>
          ` : `
            <div style="margin: 8px 0; font-size: 13px;">
              <span style="background: ${statusBadge.color}; color: white; padding: 2px 8px; border-radius: 3px;">${statusBadge.icon} ${statusBadge.text}</span>
            </div>
          `}

          <div style="color: #666; font-size: 12px; margin-top: 8px;">
            <div><strong>👤 İşlemi Yapan:</strong> ${m.kullaniciAdi || 'Sistem'}</div>
            ${m.secilenOnaylayiciAdi ? `<div><strong>👔 Atanan:</strong> ${m.secilenOnaylayiciAdi}</div>` : ''}
            ${m.aciklama ? `<div style="margin-top: 4px;"><strong>📋 Açıklama:</strong> ${m.aciklama}</div>` : ''}
            <div style="margin-top: 4px;"><strong>🕐 Tarih:</strong> ${new Date(m.islemTarihi).toLocaleString('tr-TR')}</div>
          </div>
        </div>
      `;
    }).join('');

    Swal.fire({
      title: '📜 Duyuru Geçmişi',
      html: `
        <div style="max-height: 500px; overflow-y: auto; text-align: left; padding: 8px;">
          ${movements.length > 0 ? timelineHtml : '<p style="text-align: center; color: #999;">Henüz işlem geçmişi bulunmamaktadır.</p>'}
        </div>
      `,
      width: '700px',
      showCloseButton: true,
      showConfirmButton: false,
      customClass: {
        container: 'history-modal'
      }
    });
  }

}
