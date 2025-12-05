import { Component, OnInit, inject, signal } from '@angular/core';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

import { SystemSettingsService, CreateSystemSettingRequest } from '../../../../services/system-settings.service';
import { SystemSetting } from '../../../../common/models/system-settings.model';
import { createToggleSwitch, TOGGLE_SWITCH_CSS, initializeToggleSwitch } from '../../../../common/utils/swal-utils';

interface EmailCategory {
  id: number;
  key: string;
  description: string;
  signature: string;
  hasSignature: boolean;
  isActive: boolean;
  gorevYeri?: number | null;
}

@Component({
  selector: 'app-email-category-list',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule
],
  templateUrl: './email-category-list.html',
  styleUrl: './email-category-list.css'
})
export class EmailCategoryList implements OnInit {
  private systemSettingsService = inject(SystemSettingsService);
  private toastr = inject(ToastrService);

  loading = signal(false);
  categories = signal<EmailCategory[]>([]);
  displayedColumns = ['key', 'description', 'hasSignature', 'isActive', 'actions'];

  // Dashboard stats
  totalCategories = signal(0);
  activeCategories = signal(0);
  categoriesWithSignature = signal(0);

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    // includeSecret: true, includeInactive: true - Tüm kategorileri (gizli ve pasif dahil) getir
    this.systemSettingsService.getAllSettings('EMAIL_IMZA', true, true).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const cats: EmailCategory[] = response.data.map(s => ({
            id: s.id,
            key: s.key,
            description: s.description,
            signature: s.value,
            hasSignature: !!s.value && s.value.length > 0,
            isActive: s.aktif === 'Y',
            gorevYeri: s.gorevYeri
          }));
          this.categories.set(cats);
          this.updateStats(cats);
        }
        this.loading.set(false);
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Kategoriler yüklenemedi';
        this.toastr.error(errorMessage);
        this.loading.set(false);
      }
    });
  }

  updateStats(cats: EmailCategory[]): void {
    this.totalCategories.set(cats.length);
    this.activeCategories.set(cats.filter(c => c.isActive).length);
    this.categoriesWithSignature.set(cats.filter(c => c.hasSignature).length);
  }

  async createCategory(): Promise<void> {
    const result = await Swal.fire({
      title: 'Yeni Email Kategorisi',
      html: `
        <input id="key" class="swal2-input" placeholder="Kategori Anahtarı (örn: HASTANE)" style="width: 80%;">
        <input id="description" class="swal2-input" placeholder="Açıklama (örn: Hastane email kategorisi)" style="width: 80%;">
        <input id="gorevYeri" class="swal2-input" type="number" placeholder="Görev Yeri Kodu" value="0" style="width: 80%;">
        <div style="font-size: 12px; color: #666; margin-top: 8px; text-align: left; padding: 0 10%;">
          <strong>Not:</strong> Görev yeri kodu 0 ise Rektörlük için kullanılır. Fakülte/Birim kodları için (örn: 500 Mühendislik, 100 Tıp)
          personel otomasyonu yetkililerinden öğrenebilirsiniz.
        </div>
        <textarea id="signature" class="swal2-textarea" placeholder="İmza HTML Kodu (opsiyonel)" style="width: 80%; height: 150px; margin-top: 12px;"></textarea>
      `,
      showCancelButton: true,
      confirmButtonText: 'Oluştur',
      cancelButtonText: 'İptal',
      preConfirm: () => {
        const key = (document.getElementById('key') as HTMLInputElement).value.toUpperCase();
        const description = (document.getElementById('description') as HTMLInputElement).value;
        const gorevYeriStr = (document.getElementById('gorevYeri') as HTMLInputElement).value;
        const signature = (document.getElementById('signature') as HTMLTextAreaElement).value;

        if (!key || !description) {
          Swal.showValidationMessage('Kategori anahtarı ve açıklama zorunludur');
          return false;
        }

        const gorevYeri = gorevYeriStr ? parseInt(gorevYeriStr) : null;

        return { key, description, gorevYeri, signature };
      }
    });

    if (!result.isConfirmed || !result.value) return;

    const { key, description, gorevYeri, signature } = result.value;

    const request: CreateSystemSettingRequest = {
      category: 'EMAIL_IMZA',
      key,
      value: signature || '',
      description,
      isSecret: false,
      isActive: true,
      gorevYeri: gorevYeri
    };

    this.systemSettingsService.createSetting(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Kategori başarıyla oluşturuldu');
          this.loadCategories();
        } else {
          this.toastr.error(response.message || 'Kategori oluşturulamadı');
        }
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Kategori oluşturulurken hata oluştu';
        this.toastr.error(errorMessage);
      }
    });
  }

  async editCategory(category: EmailCategory): Promise<void> {
    const result = await Swal.fire({
      title: `Kategoriyi Düzenle: ${category.key}`,
      html: `
        <input id="description" class="swal2-input" placeholder="Açıklama" value="${category.description || ''}" style="width: 80%;">
        <input id="gorevYeri" class="swal2-input" type="number" placeholder="Görev Yeri Kodu" value="${category.gorevYeri != null ? category.gorevYeri : ''}" style="width: 80%;">
        <div style="font-size: 12px; color: #666; margin-top: 8px; text-align: left; padding: 0 10%;">
          <strong>Not:</strong> Görev yeri kodu 0 ise Rektörlük için kullanılır. Fakülte/Birim kodları için (örn: 500 Mühendislik, 100 Tıp)
          personel otomasyonu yetkililerinden öğrenebilirsiniz.
        </div>
        <textarea id="signature" class="swal2-textarea" placeholder="İmza HTML Kodu (opsiyonel)" style="width: 80%; height: 150px; margin-top: 12px;">${category.signature || ''}</textarea>
        ${createToggleSwitch('isActive', 'Aktif', category.isActive)}
        ${TOGGLE_SWITCH_CSS}
      `,
      showCancelButton: true,
      confirmButtonText: 'Güncelle',
      cancelButtonText: 'İptal',
      didOpen: () => initializeToggleSwitch('isActive'),
      preConfirm: () => {
        const description = (document.getElementById('description') as HTMLInputElement).value;
        const gorevYeriStr = (document.getElementById('gorevYeri') as HTMLInputElement).value;
        const signature = (document.getElementById('signature') as HTMLTextAreaElement).value;
        const isActive = (document.getElementById('isActive') as HTMLInputElement).checked;

        const gorevYeri = gorevYeriStr ? parseInt(gorevYeriStr) : null;

        return { description, gorevYeri, signature, isActive };
      }
    });

    if (!result.isConfirmed || !result.value) return;

    const { description, gorevYeri, signature, isActive } = result.value;

    this.systemSettingsService.updateSingleSetting(category.id, {
      value: signature || '',
      description,
      isActive,
      gorevYeri
    }).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Kategori başarıyla güncellendi');
          this.loadCategories();
        } else {
          this.toastr.error(response.message || 'Kategori güncellenemedi');
        }
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Kategori güncellenirken hata oluştu';
        this.toastr.error(errorMessage);
      }
    });
  }

  async deleteCategory(category: EmailCategory): Promise<void> {
    const result = await Swal.fire({
      title: 'Kategoriyi Sil',
      html: `<strong>${category.key}</strong> kategorisini silmek istediğinizden emin misiniz?<br><br>Bu işlem geri alınamaz!`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Sil',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#d33'
    });

    if (!result.isConfirmed) return;

    this.systemSettingsService.deleteSetting(category.id).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Kategori başarıyla silindi');
          this.loadCategories();
        } else {
          this.toastr.error(response.message || 'Kategori silinemedi');
        }
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Kategori silinirken hata oluştu';
        this.toastr.error(errorMessage);
      }
    });
  }
}
