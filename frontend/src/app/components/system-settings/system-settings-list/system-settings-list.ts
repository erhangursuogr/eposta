import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSelectModule } from '@angular/material/select';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

import { SystemSettingsService } from '../../../services/system-settings.service';
import { SystemSetting, SystemSettingCategory, CATEGORY_CONFIG } from '../../../common/models/system-settings.model';

@Component({
  selector: 'app-system-settings-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    MatSelectModule
  ],
  templateUrl: './system-settings-list.html',
  styleUrl: './system-settings-list.css'
})
export class SystemSettingsList implements OnInit {
  private systemSettingsService = inject(SystemSettingsService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);

  // State
  allSettings = signal<SystemSetting[]>([]);
  loading = signal<boolean>(false);
  saving = signal<boolean>(false);
  searchTerm = signal<string>('');
  showSecretSettings = signal<boolean>(false);
  selectedTab = signal<number>(0);

  // Forms - Her kategori için ayrı form
  categoryForms: Map<string, FormGroup> = new Map();

  // Kategorileri grupla
  categories = computed<SystemSettingCategory[]>(() => {
    const settings = this.allSettings();
    const categoriesMap = new Map<string, SystemSetting[]>();

    // EMAIL_* kategorilerini filtrele (EMAIL_KATEGORI, EMAIL_IMZA, EMAIL_DUYURU vb. hariç)
    // EMAIL_ORTAK = Ortak SMTP sunucu ayarları (tüm gruplar için) - burada kalır
    settings.forEach(setting => {
      // EMAIL ile başlayanları gösterme ANCAK EMAIL_ORTAK hariç (ortak SMTP sunucu ayarları)
      if (setting.category.startsWith('EMAIL_') && setting.category !== 'EMAIL_ORTAK') {
        return;
      }

      if (!categoriesMap.has(setting.category)) {
        categoriesMap.set(setting.category, []);
      }
      categoriesMap.get(setting.category)!.push(setting);
    });

    const categoryArray: SystemSettingCategory[] = [];
    categoriesMap.forEach((settings, category) => {
      const config = CATEGORY_CONFIG[category] || {
        displayName: category,
        icon: 'settings',
        color: 'primary',
        order: 999
      };

      categoryArray.push({
        category,
        displayName: config.displayName,
        icon: config.icon,
        color: config.color,
        settings: settings.sort((a, b) => a.key.localeCompare(b.key)),
        isExpanded: false
      });
    });

    // Sıralama: order'a göre
    return categoryArray.sort((a, b) => {
      const orderA = CATEGORY_CONFIG[a.category]?.order || 999;
      const orderB = CATEGORY_CONFIG[b.category]?.order || 999;
      return orderA - orderB;
    });
  });

  // Arama filtresi
  filteredCategories = computed<SystemSettingCategory[]>(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.categories();

    return this.categories()
      .map(cat => ({
        ...cat,
        settings: cat.settings.filter(s =>
          s.key.toLowerCase().includes(term) ||
          s.description.toLowerCase().includes(term) ||
          s.value.toLowerCase().includes(term)
        )
      }))
      .filter(cat => cat.settings.length > 0);
  });

  // Filtrelenmiş toplam ayar sayısı (EMAIL_* hariç)
  totalSettingsCount = computed<number>(() => {
    return this.categories().reduce((sum, cat) => sum + cat.settings.length, 0);
  });

  // Stats dashboard
  activeSettingsCount = computed<number>(() => {
    return this.allSettings().filter(s => s.aktif === 'Y' && !s.category.startsWith('EMAIL_')).length;
  });

  secretSettingsCount = computed<number>(() => {
    return this.allSettings().filter(s => s.gizli === 'Y' && !s.category.startsWith('EMAIL_')).length;
  });

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading.set(true);
    this.systemSettingsService.getAllSettings(undefined, this.showSecretSettings()).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.allSettings.set(response.data);
          this.initializeForms();
        }
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading settings:', error);
        this.toastr.error('Ayarlar yüklenirken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  initializeForms(): void {
    this.categoryForms.clear();

    this.categories().forEach(category => {
      const formControls: any = {};
      category.settings.forEach(setting => {
        formControls[setting.key] = [setting.value];
      });
      this.categoryForms.set(category.category, this.fb.group(formControls));
    });
  }

  toggleSecretSettings(): void {
    this.showSecretSettings.update(v => !v);
    this.loadSettings();
  }

  onSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
  }

  toggleCategory(category: SystemSettingCategory): void {
    category.isExpanded = !category.isExpanded;
  }

  getCategoryForm(category: string): FormGroup | undefined {
    return this.categoryForms.get(category);
  }

  hasChanges(category: string): boolean {
    const form = this.categoryForms.get(category);
    return form ? form.dirty : false;
  }

  resetCategory(category: SystemSettingCategory): void {
    const form = this.categoryForms.get(category.category);
    if (form) {
      form.reset();
      category.settings.forEach(setting => {
        form.get(setting.key)?.setValue(setting.value);
      });
      form.markAsPristine();
      this.toastr.info('Değişiklikler geri alındı');
    }
  }

  saveCategory(category: SystemSettingCategory): void {
    const form = this.categoryForms.get(category.category);
    if (!form || !form.valid) {
      this.toastr.warning('Lütfen tüm alanları kontrol edin');
      return;
    }

    Swal.fire({
      title: 'Ayarları Kaydet',
      text: `${category.displayName} kategorisindeki değişiklikler kaydedilecek. Onaylıyor musunuz?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Kaydet',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#1976d2',
      cancelButtonColor: '#757575'
    }).then((result) => {
      if (result.isConfirmed) {
        this.performSave(category, form);
      }
    });
  }

  private performSave(category: SystemSettingCategory, form: FormGroup): void {
    this.saving.set(true);

    // Form değerlerini al
    const formValues = form.value;
    const settings = Object.keys(formValues).map(key => ({
      key,
      value: formValues[key]
    }));

    this.systemSettingsService.updateEmailSettings({ settings }).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Ayarlar başarıyla kaydedildi');
          form.markAsPristine();
          this.loadSettings(); // Reload to get fresh data
        } else {
          this.toastr.error(response.message || 'Kaydetme işlemi başarısız');
        }
        this.saving.set(false);
      },
      error: (error) => {
        console.error('Error saving settings:', error);
        this.toastr.error('Ayarlar kaydedilirken hata oluştu');
        this.saving.set(false);
      }
    });
  }

  exportSettings(): void {
    const data = JSON.stringify(this.allSettings(), null, 2);
    const blob = new Blob([data], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `system-settings-${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    window.URL.revokeObjectURL(url);
    this.toastr.success('Ayarlar dışa aktarıldı');
  }

  getInputType(setting: SystemSetting): string {
    if (setting.gizli === 'Y') return 'password';
    if (setting.key.includes('PORT') || setting.key.includes('LIMIT') || setting.key.includes('MINUTES') || setting.key.includes('DAYS')) {
      return 'number';
    }
    if (setting.key.includes('EMAIL')) return 'email';
    return 'text';
  }

  getHintText(setting: SystemSetting): string {
    return setting.description || '';
  }

  isTextarea(setting: SystemSetting): boolean {
    return setting.key.includes('IMZA') ||
           setting.key.includes('TEMPLATE') ||
           setting.value.length > 100;
  }

  isAuthMode(setting: SystemSetting): boolean {
    return setting.category === 'AUTH' && setting.key === 'MODE';
  }

  onAuthModeToggle(event: any, settingKey: string, form: FormGroup): void {
    const newValue = event.checked ? '1' : '0';
    const control = form.get(settingKey);
    if (control) {
      control.setValue(newValue);
      control.markAsDirty();
    }
  }

  // CRUD operations
  openCreateDialog(): void {
    Swal.fire({
      title: 'Yeni Sistem Ayarı',
      html: `
        <select id="category" class="swal2-input" style="width: 80%;">
          ${Object.keys(CATEGORY_CONFIG).map(cat =>
            `<option value="${cat}">${CATEGORY_CONFIG[cat].displayName}</option>`
          ).join('')}
        </select>
        <input id="key" class="swal2-input" placeholder="Ayar Anahtarı (örn: SMTP_PORT)" style="width: 80%;">
        <textarea id="value" class="swal2-textarea" placeholder="Değer" style="width: 80%; height: 100px;"></textarea>
        <input id="description" class="swal2-input" placeholder="Açıklama" style="width: 80%;">
        <label style="display: block; margin-top: 10px;">
          <input type="checkbox" id="isSecret"> Gizli Ayar
        </label>
        <label style="display: block;">
          <input type="checkbox" id="isActive" checked> Aktif
        </label>
      `,
      showCancelButton: true,
      confirmButtonText: 'Oluştur',
      cancelButtonText: 'İptal',
      preConfirm: () => {
        const category = (document.getElementById('category') as HTMLSelectElement).value;
        const key = (document.getElementById('key') as HTMLInputElement).value;
        const value = (document.getElementById('value') as HTMLTextAreaElement).value;
        const description = (document.getElementById('description') as HTMLInputElement).value;
        const isSecret = (document.getElementById('isSecret') as HTMLInputElement).checked;
        const isActive = (document.getElementById('isActive') as HTMLInputElement).checked;

        if (!category || !key || !description) {
          Swal.showValidationMessage('Kategori, anahtar ve açıklama zorunludur');
          return false;
        }

        return { category, key, value, description, isSecret, isActive };
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.systemSettingsService.createSetting(result.value).subscribe({
          next: (response) => {
            if (response.success) {
              this.toastr.success('Ayar başarıyla oluşturuldu');
              this.loadSettings();
            } else {
              this.toastr.error(response.message || 'Ayar oluşturulamadı');
            }
          },
          error: (error) => {
            const errorMessage = error?.error?.message || 'Ayar oluşturulurken hata oluştu';
            this.toastr.error(errorMessage);
          }
        });
      }
    });
  }

  openEditDialog(setting: SystemSetting): void {
    Swal.fire({
      title: `${setting.category}.${setting.key}`,
      html: `
        <textarea id="value" class="swal2-textarea" placeholder="Değer" style="width: 80%; height: 100px;">${setting.value || ''}</textarea>
        <input id="description" class="swal2-input" placeholder="Açıklama" value="${setting.description || ''}" style="width: 80%;">
        <label style="display: block; margin-top: 10px;">
          <input type="checkbox" id="isActive" ${setting.aktif === 'Y' ? 'checked' : ''}> Aktif
        </label>
      `,
      showCancelButton: true,
      confirmButtonText: 'Güncelle',
      cancelButtonText: 'İptal',
      preConfirm: () => {
        const value = (document.getElementById('value') as HTMLTextAreaElement).value;
        const description = (document.getElementById('description') as HTMLInputElement).value;
        const isActive = (document.getElementById('isActive') as HTMLInputElement).checked;

        return { value, description, isActive };
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.systemSettingsService.updateSingleSetting(setting.id, result.value).subscribe({
          next: (response) => {
            if (response.success) {
              this.toastr.success('Ayar başarıyla güncellendi');
              this.loadSettings();
            } else {
              this.toastr.error(response.message || 'Ayar güncellenemedi');
            }
          },
          error: (error) => {
            const errorMessage = error?.error?.message || 'Ayar güncellenirken hata oluştu';
            this.toastr.error(errorMessage);
          }
        });
      }
    });
  }

  deleteSetting(setting: SystemSetting): void {
    Swal.fire({
      title: 'Ayarı Sil',
      html: `<strong>${setting.category}.${setting.key}</strong> ayarını silmek istediğinizden emin misiniz?<br><br>Bu işlem geri alınamaz!`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Sil',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed) {
        this.systemSettingsService.deleteSetting(setting.id).subscribe({
          next: (response) => {
            if (response.success) {
              this.toastr.success('Ayar başarıyla silindi');
              this.loadSettings();
            } else {
              this.toastr.error(response.message || 'Ayar silinemedi');
            }
          },
          error: (error) => {
            const errorMessage = error?.error?.message || 'Ayar silinirken hata oluştu';
            this.toastr.error(errorMessage);
          }
        });
      }
    });
  }

  // Utility methods
  trackByCategory(index: number, category: SystemSettingCategory): string {
    return category.category;
  }

  trackBySetting(index: number, setting: SystemSetting): number {
    return setting.id;
  }
}
