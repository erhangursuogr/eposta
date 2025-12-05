import { Component, computed, inject, OnInit, signal } from '@angular/core';

import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TemplateService } from '../../../services/template.service';
import { TemplateCategoryService } from '../../../services/template-category.service';
import { Template } from '../../../common/models/template.model';
import { TemplateCategory } from '../../../common/models/template-category.model';
import { UserDataService } from '../../../services/userdata.service';
import { EmptyStateComponent } from '../../common/empty-state/empty-state.component';
import { LoadingComponent } from '../../common/loading/loading.component';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-template-list',
  imports: [
    RouterModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatCardModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    EmptyStateComponent,
    LoadingComponent
],
  templateUrl: './template-list.html',
  styleUrl: './template-list.css'
})
export class TemplateListComponent implements OnInit {
  private readonly templateService = inject(TemplateService);
  private readonly categoryService = inject(TemplateCategoryService);
  private readonly userDataService = inject(UserDataService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  // State
  templates = signal<Template[]>([]);
  categories = signal<TemplateCategory[]>([]);
  loading = signal(false);
  searchTerm = signal('');
  filterStatus = signal<'ALL' | 'Y' | 'N'>('ALL');
  filterCategory = signal<number | null>(null);

  // User role check
  currentUser = this.userDataService.user;
  canManageTemplates = computed(() => {
    const role = this.currentUser()?.role;
    return role === 'ADMIN' || role === 'MANAGER' || role === 'COORDINATOR';
  });

  // Computed - Sadece kullanılmış kategorileri göster
  usedCategories = computed(() => {
    const templates = this.templates();
    const usedCategoryIds = new Set(templates.map(t => t.kategoriId).filter(id => id !== null));
    return this.categories().filter(c => usedCategoryIds.has(c.id));
  });

  // Computed
  filteredTemplates = computed(() => {
    let result = this.templates();

    // Durum filtresi
    if (this.filterStatus() !== 'ALL') {
      result = result.filter(t => t.aktif === this.filterStatus());
    }

    // Kategori filtresi
    if (this.filterCategory() !== null) {
      result = result.filter(t => t.kategoriId === this.filterCategory());
    }

    // Arama filtresi
    const search = this.searchTerm().toLowerCase();
    if (search) {
      result = result.filter(t =>
        t.sablonAdi.toLowerCase().includes(search) ||
        t.konuSablonu?.toLowerCase().includes(search)
      );
    }

    // Sıralama: Önce aktifler, sonra pasifler, her grup içinde oluşturma tarihine göre
    return result.sort((a, b) => {
      // Önce aktif/pasif durumuna göre sırala
      if (a.aktif === 'Y' && b.aktif === 'N') return -1;
      if (a.aktif === 'N' && b.aktif === 'Y') return 1;

      // Aynı durumdaysa, oluşturma tarihine göre (yeniden eskiye)
      return new Date(b.olusturmaTarihi).getTime() - new Date(a.olusturmaTarihi).getTime();
    });
  });

  displayedColumns: string[] = ['sablonAdi', 'aktif', 'kullanimSayisi', 'olusturmaTarihi', 'actions'];

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loadTemplates();
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getActiveCategories().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.categories.set(response.data);
        }
      },
      error: () => {
        // Sessizce hata yönet, kategori yüklenemezse de şablonlar gösterilmeli
      }
    });
  }

  loadTemplates(): void {
    this.loading.set(true);
    this.templateService.getTemplates().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.templates.set(response.data);
        } else {
          this.showMessage(response.message || 'Şablonlar yüklenemedi', 'error');
        }
        this.loading.set(false);
      },
      error: () => {
        this.showMessage('Şablonlar yüklenirken hata oluştu', 'error');
        this.loading.set(false);
      }
    });
  }

  navigateToCreate(): void {
    this.router.navigate(['/templates/new']);
  }

  navigateToEdit(id: number): void {
    this.router.navigate(['/templates', id, 'edit']);
  }

  useTemplate(id: number): void {
    this.router.navigate(['/duyuru-olustur'], { queryParams: { templateId: id } });
  }

  toggleActive(template: Template): void {
    const action = template.aktif === 'Y' ? 'pasif' : 'aktif';
    const service$ = template.aktif === 'Y'
      ? this.templateService.deactivateTemplate(template.id)
      : this.templateService.activateTemplate(template.id);

    service$.subscribe({
      next: (response) => {
        if (response.success) {
          this.showMessage(`Şablon ${action} edildi`, 'success');
          this.loadTemplates();
          this.loadCategories(); // Kategori listesini güncelle
        } else {
          this.showMessage(response.message || 'İşlem başarısız', 'error');
        }
      },
      error: () => this.showMessage('İşlem sırasında hata oluştu', 'error')
    });
  }

  duplicate(template: Template): void {
    this.templateService.duplicateTemplate(template.id).subscribe({
      next: (response) => {
        if (response.success) {
          this.showMessage('Şablon çoğaltıldı', 'success');
          this.loadTemplates();
          this.loadCategories(); // Kategori listesini güncelle
        } else {
          this.showMessage(response.message || 'Çoğaltma başarısız', 'error');
        }
      },
      error: () => this.showMessage('Çoğaltma sırasında hata oluştu', 'error')
    });
  }

  delete(template: Template): void {
    Swal.fire({
      title: 'Şablon Sil',
      text: `"${template.sablonAdi}" şablonunu silmek istediğinizden emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sil',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed) {
        this.templateService.deleteTemplate(template.id).subscribe({
          next: (response) => {
            if (response.success) {
              this.showMessage('Şablon silindi', 'success');
              this.loadTemplates();
              this.loadCategories(); // Kategori listesini güncelle
            } else {
              this.showMessage(response.message || 'Silme başarısız', 'error');
            }
          },
          error: () => this.showMessage('Silme sırasında hata oluştu', 'error')
        });
      }
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  private showMessage(message: string, type: 'success' | 'error'): void {
    this.snackBar.open(message, 'Kapat', {
      duration: 3000,
      panelClass: type === 'success' ? 'snackbar-success' : 'snackbar-error'
    });
  }
}
