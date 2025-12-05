import { Component, inject } from '@angular/core';

import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { TemplateCategoryService } from '../../../services/template-category.service';

@Component({
  selector: 'app-category-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatSelectModule
],
  templateUrl: './category-dialog.html',
  styleUrl: './category-dialog.css'
})
export class CategoryDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CategoryDialogComponent>);
  private readonly categoryService = inject(TemplateCategoryService);

  categoryForm: FormGroup;
  saving = false;
  errorMessage = '';

  // Önceden tanımlı renkler
  colors = [
    { name: 'Kırmızı', value: '#f44336' },
    { name: 'Pembe', value: '#e91e63' },
    { name: 'Mor', value: '#9c27b0' },
    { name: 'Lacivert', value: '#3f51b5' },
    { name: 'Mavi', value: '#2196f3' },
    { name: 'Açık Mavi', value: '#03a9f4' },
    { name: 'Turkuaz', value: '#00bcd4' },
    { name: 'Yeşil', value: '#4caf50' },
    { name: 'Açık Yeşil', value: '#8bc34a' },
    { name: 'Turuncu', value: '#ff9800' },
    { name: 'Kahverengi', value: '#795548' },
    { name: 'Gri', value: '#607d8b' }
  ];

  // Önceden tanımlı ikonlar
  icons = [
    { name: 'Klasör', value: 'folder' },
    { name: 'Etiket', value: 'label' },
    { name: 'Yıldız', value: 'star' },
    { name: 'Kalp', value: 'favorite' },
    { name: 'Kitap', value: 'book' },
    { name: 'Belge', value: 'description' },
    { name: 'İş', value: 'work' },
    { name: 'Ev', value: 'home' },
    { name: 'Okul', value: 'school' },
    { name: 'Bildirim', value: 'notifications' },
    { name: 'Bilgi', value: 'info' },
    { name: 'Uyarı', value: 'warning' }
  ];

  constructor() {
    this.categoryForm = this.fb.group({
      kategoriAdi: ['', Validators.required],
      renk: ['#2196f3', Validators.required],
      ikon: ['folder', Validators.required]
    });
  }

  onSave(): void {
    if (this.categoryForm.invalid) return;

    this.saving = true;
    this.errorMessage = '';

    this.categoryService.createCategory(this.categoryForm.value).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.dialogRef.close(response.data);
        } else {
          this.errorMessage = response.message || 'Kategori oluşturulamadı';
          this.saving = false;
        }
      },
      error: () => {
        this.errorMessage = 'Kategori oluşturulurken hata oluştu';
        this.saving = false;
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
