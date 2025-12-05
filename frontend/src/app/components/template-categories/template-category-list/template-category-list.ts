import { Component, computed, inject, OnInit, signal } from '@angular/core';

import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TemplateCategoryService } from '../../../services/template-category.service';
import { TemplateCategory } from '../../../common/models/template-category.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-template-category-list',
  imports: [
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatCardModule,
    MatMenuModule,
    MatProgressSpinnerModule
],
  templateUrl: './template-category-list.html',
  styleUrl: './template-category-list.css'
})
export class TemplateCategoryList implements OnInit {
  private readonly categoryService = inject(TemplateCategoryService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  // State
  categories = signal<TemplateCategory[]>([]);
  loading = signal(false);

  // Computed - sıralamaya göre kategoriler
  sortedCategories = computed(() => {
    return [...this.categories()].sort((a, b) => a.siraNo - b.siraNo);
  });

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.categoryService.getAllCategories().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.categories.set(response.data);
        } else {
          this.showMessage(response.message || 'Kategoriler yüklenemedi', 'error');
        }
        this.loading.set(false);
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Kategoriler yüklenirken hata oluştu';
        this.showMessage(errorMessage, 'error');
        this.loading.set(false);
      }
    });
  }

  navigateToForm(id?: number): void {
    if (id) {
      this.router.navigate(['/template-categories/edit', id]);
    } else {
      this.router.navigate(['/template-categories/new']);
    }
  }

  toggleStatus(category: TemplateCategory): void {
    const action = category.aktif === 'Y' ? 'deactivate' : 'activate';
    const message = category.aktif === 'Y' ? 'pasifleştirildi' : 'aktifleştirildi';

    const observable = category.aktif === 'Y'
      ? this.categoryService.deactivateCategory(category.id)
      : this.categoryService.activateCategory(category.id);

    observable.subscribe({
      next: (response) => {
        if (response.success) {
          this.showMessage(`Kategori başarıyla ${message}`, 'success');
          this.loadCategories();
        } else {
          this.showMessage(response.message || `Kategori ${message} edilemedi`, 'error');
        }
      },
      error: (error) => {
        const errorMessage = error?.error?.message || `Kategori ${message} edilirken hata oluştu`;
        this.showMessage(errorMessage, 'error');
      }
    });
  }

  deleteCategory(category: TemplateCategory): void {
    Swal.fire({
      title: 'Emin misiniz?',
      html: `<b>${category.kategoriAdi}</b> kategorisini silmek istediğinize emin misiniz?<br><small>Bu kategoriye ait şablonlar varsa silinemez.</small>`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Evet, sil',
      cancelButtonText: 'İptal'
    }).then((result) => {
      if (result.isConfirmed) {
        this.categoryService.deleteCategory(category.id).subscribe({
          next: (response) => {
            if (response.success) {
              this.showMessage('Kategori başarıyla silindi', 'success');
              this.loadCategories();
            } else {
              this.showMessage(response.message || 'Kategori silinemedi', 'error');
            }
          },
          error: (error) => {
            const errorMessage = error?.error?.message || 'Kategori silinirken hata oluştu';
            this.showMessage(errorMessage, 'error');
          }
        });
      }
    });
  }

  private showMessage(message: string, type: 'success' | 'error'): void {
    this.snackBar.open(message, 'Kapat', {
      duration: 3000,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass: type === 'success' ? 'snackbar-success' : 'snackbar-error'
    });
  }
}
