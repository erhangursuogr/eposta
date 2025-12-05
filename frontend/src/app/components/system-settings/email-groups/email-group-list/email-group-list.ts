import { Component, OnInit, inject, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

import { EmailGroupService } from '../../../../services/email-group.service';
import { EmailGroup, GrupTipi } from '../../../../common/models/email-group.model';
import { EmailGroupCreateDialog } from '../email-group-create-dialog/email-group-create-dialog';
import { EmptyStateComponent } from '../../../common/empty-state/empty-state.component';
import { LoadingComponent } from '../../../common/loading/loading.component';

@Component({
  selector: 'app-email-group-list',
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
    MatSelectModule,
    MatProgressSpinnerModule,
    EmptyStateComponent,
    LoadingComponent
],
  templateUrl: './email-group-list.html',
  styleUrl: './email-group-list.css'
})
export class EmailGroupList implements OnInit {
  private emailGroupService = inject(EmailGroupService);
  private dialog = inject(MatDialog);
  private toastr = inject(ToastrService);

  // State
  groups = signal<EmailGroup[]>([]);
  loading = signal<boolean>(false);
  searchTerm = signal<string>('');
  filterType = signal<string>('ALL');
  filterStatus = signal<string>('ALL');

  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalCount = 0;

  // Filter options
  grupTipleri = [
    { value: 'ALL', label: 'Tüm Tipler' },
    { value: 'NORMAL', label: 'Standart Grup' },
    { value: 'STATIK', label: 'Dosyadan Yüklenen' },
    { value: 'DINAMIK', label: 'Dinamik View' },
    { value: 'DEBIS', label: 'Debis Listeci' }
  ];

  statusOptions = [
    { value: 'ALL', label: 'Tümü' },
    { value: 'ACTIVE', label: 'Aktif' },
    { value: 'INACTIVE', label: 'Pasif' }
  ];

  // Table columns
  displayedColumns = ['grupAdi', 'grupTipi', 'uyeSayisi', 'aktif', 'bccOnly', 'olusturmaTarihi', 'actions'];

  ngOnInit(): void {
    this.loadGroups();
  }

  loadGroups(): void {
    this.loading.set(true);
    this.emailGroupService.getGroups(this.currentPage, this.pageSize, this.searchTerm()).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.groups.set(response.data);
          this.totalCount = response.totalCount || 0;
        }
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading groups:', error);
        this.toastr.error('Gruplar yüklenirken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadGroups();
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadGroups();
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.filterType.set('ALL');
    this.filterStatus.set('ALL');
    this.currentPage = 1;
    this.loadGroups();
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(EmailGroupCreateDialog, {
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadGroups(); // Refresh list
      }
    });
  }

  openDetailDialog(group: EmailGroup): void {
    import('../email-group-detail-dialog/email-group-detail-dialog').then(m => {
      this.dialog.open(m.EmailGroupDetailDialog, {
        width: '700px',
        maxWidth: '95vw',
        maxHeight: '90vh',
        data: { groupId: group.id }
      });
    });
  }

  openEditDialog(group: EmailGroup): void {
    import('../email-group-edit-dialog/email-group-edit-dialog').then(m => {
      const dialogRef = this.dialog.open(m.EmailGroupEditDialog, {
        width: '800px',
        maxWidth: '95vw',
        maxHeight: '90vh',
        data: { groupId: group.id }
      });

      dialogRef.afterClosed().subscribe(result => {
        if (result) {
          this.loadGroups();
        }
      });
    });
  }

  deleteGroup(group: EmailGroup): void {
    Swal.fire({
      title: 'Grup Sil',
      html: `
        <div class="text-start p-3">
          <p><strong>Grup:</strong> ${group.grupAdi}</p>
          <p><strong>Tip:</strong> ${this.emailGroupService.getGrupTipiText(group.grupTipi)}</p>
          <p><strong>Üye Sayısı:</strong> ${group.uyeSayisi}</p>
          <p class="text-warning"><strong>⚠️ Bu işlem geri alınamaz!</strong></p>
        </div>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Sil',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#f44336'
    }).then((result) => {
      if (result.isConfirmed) {
        this.performDelete(group.id);
      }
    });
  }

  private performDelete(id: number): void {
    this.loading.set(true);
    this.emailGroupService.deleteGroup(id).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Grup başarıyla silindi');
          this.loadGroups();
        } else {
          this.toastr.error(response.message || 'Grup silinemedi');
          this.loading.set(false);
        }
      },
      error: (error) => {
        console.error('Delete error:', error);
        this.toastr.error(error.error?.message || 'Grup silinirken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  getGrupTipiBadgeClass(grupTipi: string): string {
    return this.emailGroupService.getGrupTipiBadge(grupTipi);
  }

  getGrupTipiText(grupTipi: string): string {
    return this.emailGroupService.getGrupTipiText(grupTipi);
  }

  formatDate(date: Date | string): string {
    if (!date) return '-';
    const d = new Date(date);
    return d.toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  // Computed filtered groups based on type and status filters
  get filteredGroups(): EmailGroup[] {
    let filtered = this.groups();

    // Filter by type
    if (this.filterType() !== 'ALL') {
      filtered = filtered.filter(g => g.grupTipi === this.filterType());
    }

    // Filter by status
    if (this.filterStatus() !== 'ALL') {
      if (this.filterStatus() === 'ACTIVE') {
        filtered = filtered.filter(g => g.isActive);
      } else if (this.filterStatus() === 'INACTIVE') {
        filtered = filtered.filter(g => !g.isActive);
      }
    }

    return filtered;
  }
}
