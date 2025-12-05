import { Component, inject, OnInit, signal } from '@angular/core';

import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TemplateCategoryService } from '../../../services/template-category.service';

// Material icon listesi (popüler olanlar)
const MATERIAL_ICONS = [
  'label', 'bookmark', 'favorite', 'star', 'flag', 'announcement',
  'notifications', 'celebration', 'emoji_events', 'workspace_premium',
  'trending_up', 'campaign', 'loyalty', 'card_giftcard', 'cake',
  'sentiment_satisfied', 'warning', 'error', 'info', 'help'
];

// Önceden tanımlı renkler
const PRESET_COLORS = [
  { name: 'Mavi', value: '#1976d2' },
  { name: 'Yeşil', value: '#388e3c' },
  { name: 'Kırmızı', value: '#d32f2f' },
  { name: 'Turuncu', value: '#f57c00' },
  { name: 'Mor', value: '#7b1fa2' },
  { name: 'Pembe', value: '#c2185b' },
  { name: 'Kahverengi', value: '#5d4037' },
  { name: 'Gri', value: '#616161' }
];

@Component({
  selector: 'app-template-category-form',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSnackBarModule,
    MatCardModule,
    MatProgressSpinnerModule
],
  templateUrl: './template-category-form.html',
  styleUrl: './template-category-form.css'
})
export class TemplateCategoryForm implements OnInit {
  private readonly categoryService = inject(TemplateCategoryService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);

  // State
  categoryForm!: FormGroup;
  loading = signal(false);
  isEditMode = signal(false);
  categoryId?: number;

  // Data
  readonly icons = MATERIAL_ICONS;
  readonly colors = PRESET_COLORS;

  ngOnInit(): void {
    this.initForm();

    const id = this.route.snapshot.params['id'];
    if (id) {
      this.categoryId = +id;
      this.isEditMode.set(true);
      this.loadCategory(this.categoryId);
    }
  }

  initForm(): void {
    this.categoryForm = this.fb.group({
      kategoriAdi: ['', [Validators.required, Validators.maxLength(100)]],
      aciklama: ['', Validators.maxLength(500)],
      renk: ['#1976d2', Validators.required],
      ikon: ['label', Validators.required],
      siraNo: [0, Validators.required]
    });
  }

  loadCategory(id: number): void {
    this.loading.set(true);
    this.categoryService.getCategoryById(id).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.categoryForm.patchValue({
            kategoriAdi: response.data.kategoriAdi,
            aciklama: response.data.aciklama,
            renk: response.data.renk,
            ikon: response.data.ikon,
            siraNo: response.data.siraNo
          });
        } else {
          this.showMessage(response.message || 'Kategori yüklenemedi', 'error');
          this.router.navigate(['/template-categories']);
        }
        this.loading.set(false);
      },
      error: () => {
        this.showMessage('Kategori yüklenirken hata oluştu', 'error');
        this.loading.set(false);
        this.router.navigate(['/template-categories']);
      }
    });
  }

  onSubmit(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const formData = this.categoryForm.value;

    const request = this.isEditMode() && this.categoryId
      ? this.categoryService.updateCategory(this.categoryId, formData)
      : this.categoryService.createCategory(formData);

    request.subscribe({
      next: (response) => {
        if (response.success) {
          this.showMessage(
            this.isEditMode() ? 'Kategori başarıyla güncellendi' : 'Kategori başarıyla oluşturuldu',
            'success'
          );
          this.router.navigate(['/template-categories']);
        } else {
          this.showMessage(response.message || 'İşlem başarısız', 'error');
        }
        this.loading.set(false);
      },
      error: () => {
        this.showMessage('İşlem sırasında hata oluştu', 'error');
        this.loading.set(false);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/template-categories']);
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
