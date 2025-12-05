import { Component, OnInit, OnDestroy, inject, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog } from '@angular/material/dialog';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

import { AnnouncementService } from '../../../services/announcement.service';
import { UserDataService } from '../../../services/userdata.service';
import { UserService, User } from '../../../services/user.service';
import { Announcement, AnnouncementStatus } from '../../../common/models/announcement.model';
import { AnnouncementPreviewDialog } from '../../announcements/announcement-preview-dialog/announcement-preview-dialog';
import { EmptyStateComponent } from '../../common/empty-state/empty-state.component';
import { LoadingComponent } from '../../common/loading/loading.component';

@Component({
  selector: 'app-approval-pending',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatSelectModule,
    MatCheckboxModule,
    EmptyStateComponent,
    LoadingComponent
],
  templateUrl: './approval-pending.html',
  styleUrl: './approval-pending.css'
})
export class ApprovalPending implements OnInit, OnDestroy {
  private announcementService = inject(AnnouncementService);
  private userDataService = inject(UserDataService);
  private userService = inject(UserService);
  private router = inject(Router);
  private toastr = inject(ToastrService);
  private dialog = inject(MatDialog);
  private destroyRef = inject(DestroyRef);

  // State
  loading = signal<boolean>(false);
  announcements = signal<Announcement[]>([]);
  managers = signal<User[]>([]);
  currentUser = computed(() => this.userDataService.user());

  // User role checks
  isCoordinator = computed(() => {
    const user = this.currentUser();
    return user?.role === 'COORDINATOR' || user?.role === 'ADMIN';
  });

  isManager = computed(() => {
    const user = this.currentUser();
    return user?.role === 'MANAGER' || user?.role === 'ADMIN';
  });

  // Onay/Red notları için
  approvalNotes: Map<number, string> = new Map();
  rejectionNotes: Map<number, string> = new Map();
  selectedActions: Map<number, 'approve' | 'reject' | null> = new Map();
  selectedManagers: Map<number, number> = new Map(); // Coordinator için manager seçimi
  approveAndSend: Map<number, boolean> = new Map(); // Manager için "Onayla ve Gönder" seçeneği

  displayedColumns = ['konu', 'olusturanKullanici', 'olusturmaTarihi', 'actions'];

  ngOnInit(): void {
    this.loadPendingAnnouncements();

    // Sadece Coordinator veya Admin ise manager listesini yükle
    // Manager kendi için manager seçmez
    if (this.isCoordinator()) {
      this.loadManagers();
    }
  }

  loadPendingAnnouncements(): void {
    this.loading.set(true);
    this.announcementService.getPendingApprovals().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.announcements.set(response.data);
        }
        this.loading.set(false);
      },
      error: () => {
        this.toastr.error('Onay bekleyen duyurular yüklenemedi');
        this.loading.set(false);
      }
    });
  }

  loadManagers(): void {
    this.userService.getManagers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.managers.set(response.data);
        }
      },
      error: (error) => {
        console.error('Error loading managers:', error);
        this.toastr.error('Yönetici listesi yüklenemedi');
      }
    });
  }

  getApprovalNote(announcementId: number): string {
    return this.approvalNotes.get(announcementId) || '';
  }

  setApprovalNote(announcementId: number, note: string): void {
    this.approvalNotes.set(announcementId, note);
  }

  getRejectionNote(announcementId: number): string {
    return this.rejectionNotes.get(announcementId) || '';
  }

  setRejectionNote(announcementId: number, note: string): void {
    this.rejectionNotes.set(announcementId, note);
  }

  getSelectedAction(announcementId: number): 'approve' | 'reject' | null {
    return this.selectedActions.get(announcementId) || null;
  }

  selectAction(announcementId: number, action: 'approve' | 'reject'): void {
    const currentAction = this.selectedActions.get(announcementId);
    // Toggle: Eğer aynı action tekrar tıklanırsa kapat
    if (currentAction === action) {
      this.selectedActions.set(announcementId, null);
    } else {
      this.selectedActions.set(announcementId, action);
    }
  }

  // Manager selection for coordinator
  getSelectedManager(announcementId: number): number | undefined {
    return this.selectedManagers.get(announcementId);
  }

  setSelectedManager(announcementId: number, managerId: number): void {
    this.selectedManagers.set(announcementId, managerId);
  }

  // Approve and send checkbox for manager
  getApproveAndSend(announcementId: number): boolean {
    return this.approveAndSend.get(announcementId) || false;
  }

  setApproveAndSend(announcementId: number, value: boolean): void {
    this.approveAndSend.set(announcementId, value);
  }

  // Check announcement status
  isFirstApproval(announcement: Announcement): boolean {
    return announcement.durum === 'ILK_ONAY_BEKLIYOR';
  }

  isFinalApproval(announcement: Announcement): boolean {
    return announcement.durum === 'SON_ONAY_BEKLIYOR';
  }

  // Check if announcement is email type (can be sent)
  isEmailType(announcement: Announcement): boolean {
    return announcement.icerikTipi === 'EMAIL';
  }

  // Check if manager can approve this announcement (assigned to them)
  canManagerApprove(announcement: Announcement): boolean {
    const user = this.currentUser();
    return announcement.onaylayanKullaniciId === user?.id;
  }

  // Get status badge info
  getStatusBadge(durum: string): { text: string; class: string } {
    switch (durum) {
      case 'ILK_ONAY_BEKLIYOR':
        return { text: 'İlk Onay Bekliyor', class: 'status-chip status-blue' };
      case 'SON_ONAY_BEKLIYOR':
        return { text: 'Son Onay Bekliyor', class: 'status-chip status-orange' };
      default:
        return { text: durum, class: 'status-chip status-gray' };
    }
  }

  approve(announcement: Announcement): void {
    // İlk onay - Coordinator
    if (this.isFirstApproval(announcement) && this.isCoordinator()) {
      this.coordinatorApprove(announcement);
    }
    // Son onay - Manager
    else if (this.isFinalApproval(announcement) && this.isManager()) {
      this.managerApprove(announcement);
    }
  }

  coordinatorApprove(announcement: Announcement): void {
    const note = this.getApprovalNote(announcement.id);
    const managerId = this.getSelectedManager(announcement.id);

    if (!managerId) {
      this.toastr.warning('Lütfen bir Yönetici seçin');
      return;
    }

    const selectedManager = this.managers().find(m => m.id === managerId);
    const managerName = selectedManager ? selectedManager.adSoyad || 'Seçilen Yönetici' : 'Seçilen Yönetici';

    Swal.fire({
      title: 'Kontrolör Onayı',
      html: `
        <div class="text-start p-3">
          <p><strong>Konu:</strong> ${announcement.konu}</p>
          <p><strong>Konu:</strong> ${announcement.konu || '-'}</p>
          <p><strong>Atanacak Yönetici:</strong> ${managerName}</p>
          ${note ? `<p><strong>Onay Notu:</strong> ${note}</p>` : ''}
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Onayla ve Yöneticiye Gönder',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#4CAF50'
    }).then((result) => {
      if (result.isConfirmed) {
        this.performCoordinatorApprove(announcement.id, managerId, note);
      }
    });
  }

  managerApprove(announcement: Announcement): void {
    const note = this.getApprovalNote(announcement.id);
    const approveAndSend = this.getApproveAndSend(announcement.id);
    const isEmail = this.isEmailType(announcement);

    let confirmText = 'Evet, Onayla';
    let extraInfo = '';

    if (approveAndSend && isEmail) {
      confirmText = 'Evet, Onayla ve Gönder';
      extraInfo = '<p class="text-warning"><strong>⚠️ Duyuru onaylandıktan hemen sonra gönderilecektir!</strong></p>';
    }

    Swal.fire({
      title: 'Yönetici Onayı',
      html: `
        <div class="text-start p-3">
          <p><strong>Konu:</strong> ${announcement.konu}</p>
          <p><strong>Konu:</strong> ${announcement.konu || '-'}</p>
          ${announcement.onayNotu ? `<p><strong>Kontrolör Notu:</strong> ${announcement.onayNotu}</p>` : ''}
          ${note ? `<p><strong>Yönetici Notu:</strong> ${note}</p>` : ''}
          ${extraInfo}
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: confirmText,
      cancelButtonText: 'İptal',
      confirmButtonColor: '#4CAF50'
    }).then((result) => {
      if (result.isConfirmed) {
        if (approveAndSend && isEmail) {
          this.performManagerApproveAndSend(announcement.id, note);
        } else {
          this.performManagerApprove(announcement.id, note);
        }
      }
    });
  }

  private performCoordinatorApprove(id: number, managerId: number, note?: string): void {
    this.loading.set(true);

    this.announcementService.coordinatorApprove(id, managerId, note).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Duyuru Kontrolör onayından geçti ve Yöneticiye atandı!');
          this.loadPendingAnnouncements();
          this.approvalNotes.delete(id);
          this.selectedManagers.delete(id);
          // Bildirim sayısını güncelle
          this.userDataService.refreshPendingApprovals();
        } else {
          this.toastr.error(response.message || 'Duyuru onaylanamadı');
          this.loading.set(false);
        }
      },
      error: (error) => {
        console.error('Coordinator approval error:', error);
        this.toastr.error(error.error?.message || 'Duyuru onaylanırken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  private performManagerApprove(id: number, note?: string): void {
    this.loading.set(true);

    this.announcementService.managerApprove(id, note).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Duyuru Yönetici tarafından onaylandı!');
          this.loadPendingAnnouncements();
          this.approvalNotes.delete(id);
          this.approveAndSend.delete(id);
          // Bildirim sayısını güncelle
          this.userDataService.refreshPendingApprovals();
        } else {
          this.toastr.error(response.message || 'Duyuru onaylanamadı');
          this.loading.set(false);
        }
      },
      error: (error) => {
        console.error('Manager approval error:', error);
        this.toastr.error(error.error?.message || 'Duyuru onaylanırken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  private performManagerApproveAndSend(id: number, note?: string): void {
    this.loading.set(true);

    this.announcementService.managerApproveAndSend(id, note).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Duyuru onaylandı ve gönderildi!');
          this.loadPendingAnnouncements();
          this.approvalNotes.delete(id);
          this.approveAndSend.delete(id);
          // Bildirim sayısını güncelle
          this.userDataService.refreshPendingApprovals();
        } else {
          this.toastr.error(response.message || 'Duyuru onaylanamadı veya gönderilemedi');
          this.loading.set(false);
        }
      },
      error: (error) => {
        console.error('Manager approve and send error:', error);
        this.toastr.error(error.error?.message || 'Duyuru onaylanırken/gönderilirken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  reject(announcement: Announcement): void {
    const note = this.getRejectionNote(announcement.id);

    if (!note || note.trim().length === 0) {
      this.toastr.warning('Red nedeni zorunludur');
      return;
    }

    // İlk onay - Coordinator
    if (this.isFirstApproval(announcement) && this.isCoordinator()) {
      this.coordinatorReject(announcement, note);
    }
    // Son onay - Manager
    else if (this.isFinalApproval(announcement) && this.isManager()) {
      this.managerReject(announcement, note);
    }
  }

  coordinatorReject(announcement: Announcement, note: string): void {
    Swal.fire({
      title: 'Kontrolör Reddi',
      html: `
        <div class="text-start p-3">
          <p><strong>Konu:</strong> ${announcement.konu}</p>
          <p><strong>Konu:</strong> ${announcement.konu || '-'}</p>
          <p><strong>Red Nedeni:</strong> ${note}</p>
          <p class="text-warning"><strong>⚠️ Duyuru TASLAK durumuna geri dönecektir</strong></p>
        </div>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Reddet',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#f44336'
    }).then((result) => {
      if (result.isConfirmed) {
        this.performCoordinatorReject(announcement.id, note);
      }
    });
  }

  managerReject(announcement: Announcement, note: string): void {
    Swal.fire({
      title: 'Yönetici Reddi',
      html: `
        <div class="text-start p-3">
          <p><strong>Konu:</strong> ${announcement.konu}</p>
          <p><strong>Konu:</strong> ${announcement.konu || '-'}</p>
          ${announcement.onayNotu ? `<p><strong>Kontrolör Notu:</strong> ${announcement.onayNotu}</p>` : ''}
          <p><strong>Red Nedeni:</strong> ${note}</p>
          <p class="text-warning"><strong>⚠️ Duyuru TASLAK durumuna geri dönecektir</strong></p>
        </div>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Reddet',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#f44336'
    }).then((result) => {
      if (result.isConfirmed) {
        this.performManagerReject(announcement.id, note);
      }
    });
  }

  private performCoordinatorReject(id: number, note: string): void {
    this.loading.set(true);

    this.announcementService.coordinatorReject(id, note).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Duyuru Kontrolör tarafından reddedildi');
          this.loadPendingAnnouncements();
          this.rejectionNotes.delete(id);
          // Bildirim sayısını güncelle
          this.userDataService.refreshPendingApprovals();
        } else {
          this.toastr.error(response.message || 'Duyuru reddedilemedi');
          this.loading.set(false);
        }
      },
      error: (error) => {
        console.error('Coordinator rejection error:', error);
        this.toastr.error(error.error?.message || 'Duyuru reddedilirken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  private performManagerReject(id: number, note: string): void {
    this.loading.set(true);

    this.announcementService.managerReject(id, note).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Duyuru manager tarafından reddedildi');
          this.loadPendingAnnouncements();
          this.rejectionNotes.delete(id);
          // Bildirim sayısını güncelle
          this.userDataService.refreshPendingApprovals();
        } else {
          this.toastr.error(response.message || 'Duyuru reddedilemedi');
          this.loading.set(false);
        }
      },
      error: (error) => {
        console.error('Manager rejection error:', error);
        this.toastr.error(error.error?.message || 'Duyuru reddedilirken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  viewDetails(announcement: Announcement): void {
    this.dialog.open(AnnouncementPreviewDialog, {
      width: '900px',
      maxHeight: '90vh',
      maxWidth: '95vw',
      panelClass: 'preview-dialog-container',
      data: { announcementId: announcement.id }
    });
  }

  formatDate(date: string | Date | null | undefined): string {
    if (!date) return '-';
    const d = new Date(date);
    return d.toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  // MEMORY LEAK FIX: Component destroy olduğunda Map'leri temizle
  ngOnDestroy(): void {
    this.approvalNotes.clear();
    this.rejectionNotes.clear();
    this.selectedActions.clear();
    this.selectedManagers.clear();
    this.approveAndSend.clear();
  }
}
