import { Component, inject, OnInit, signal } from '@angular/core';

import { MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TemplateCategoryService } from '../../../services/template-category.service';
import { TemplateService } from '../../../services/template.service';
import { TemplateCategory } from '../../../common/models/template-category.model';
import { CategoryDialogComponent } from '../category-dialog/category-dialog';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-category-management-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatTooltipModule,
    MatSnackBarModule
],
  templateUrl: './category-management-dialog.html',
  styleUrl: './category-management-dialog.css'
})
export class CategoryManagementDialogComponent implements OnInit {
  private readonly dialogRef = inject(MatDialogRef<CategoryManagementDialogComponent>);
  private readonly categoryService = inject(TemplateCategoryService);
  private readonly templateService = inject(TemplateService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  categories = signal<TemplateCategory[]>([]);
  loading = signal(false);
  usageCounts = signal<Map<number, number>>(new Map());

  ngOnInit(): void {
    this.loadCategories();
    this.loadUsageCounts();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.categoryService.getAllCategories().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.categories.set(response.data);
        }
        this.loading.set(false);
      },
      error: () => {
        this.showMessage('Kategoriler yüklenemedi', 'error');
        this.loading.set(false);
      }
    });
  }

  loadUsageCounts(): void {
    this.templateService.getTemplates().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const counts = new Map<number, number>();
          response.data.forEach(template => {
            if (template.kategoriId) {
              counts.set(template.kategoriId, (counts.get(template.kategoriId) || 0) + 1);
            }
          });
          this.usageCounts.set(counts);
        }
      }
    });
  }

  getUsageCount(categoryId: number): number {
    return this.usageCounts().get(categoryId) || 0;
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(CategoryDialogComponent, {
      width: '500px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadCategories();
        this.loadUsageCounts();
        this.showMessage('Kategori oluşturuldu', 'success');
      }
    });
  }

  deleteCategory(category: TemplateCategory): void {
    const usageCount = this.getUsageCount(category.id);

    if (usageCount > 0) {
      this.showMessage(`Bu kategori ${usageCount} şablonda kullanılıyor, silinemez`, 'error');
      return;
    }

    Swal.fire({
      title: 'Kategori Sil',
      text: `"${category.kategoriAdi}" kategorisini silmek istediğinizden emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sil',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed) {
        this.categoryService.deleteCategory(category.id).subscribe({
          next: (response) => {
            if (response.success) {
              this.showMessage('Kategori silindi', 'success');
              this.loadCategories();
            } else {
              this.showMessage(response.message || 'Silme başarısız', 'error');
            }
          },
          error: () => this.showMessage('Silme sırasında hata oluştu', 'error')
        });
      }
    });
  }

  toggleActive(category: TemplateCategory): void {
    const action = category.aktif === 'Y' ? 'pasif' : 'aktif';
    const service$ = category.aktif === 'Y'
      ? this.categoryService.deactivateCategory(category.id)
      : this.categoryService.activateCategory(category.id);

    service$.subscribe({
      next: (response) => {
        if (response.success) {
          this.showMessage(`Kategori ${action} edildi`, 'success');
          this.loadCategories();
        } else {
          this.showMessage(response.message || 'İşlem başarısız', 'error');
        }
      },
      error: () => this.showMessage('İşlem sırasında hata oluştu', 'error')
    });
  }

  close(): void {
    this.dialogRef.close(true);
  }

  private showMessage(message: string, type: 'success' | 'error' | 'info'): void {
    this.snackBar.open(message, 'Kapat', {
      duration: 3000,
      panelClass: type === 'success' ? 'snackbar-success' : type === 'error' ? 'snackbar-error' : 'snackbar-info'
    });
  }
}
