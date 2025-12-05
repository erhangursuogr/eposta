import { Component, OnInit, inject, signal } from '@angular/core';

import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

import { SystemSettingsService, CreateSystemSettingRequest } from '../../../../services/system-settings.service';
import { SystemSetting } from '../../../../common/models/system-settings.model';

interface SmtpGroup {
  category: string;
  displayName: string;
  settings: {
    fromEmail: string;
    fromName: string;
    smtpUsername: string;
    smtpPassword: string;
  };
  fromEmailId?: number;
  fromNameId?: number;
  smtpUsernameId?: number;
  smtpPasswordId?: number;
  isExpanded: boolean;
}

@Component({
  selector: 'app-email-smtp-settings-list',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule
],
  templateUrl: './email-smtp-settings-list.html',
  styleUrl: './email-smtp-settings-list.css'
})
export class EmailSmtpSettingsList implements OnInit {
  private systemSettingsService = inject(SystemSettingsService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);

  loading = signal(false);
  saving = signal(false);
  savingGroup = signal<string | null>(null);
  smtpGroups = signal<SmtpGroup[]>([]);
  groupForms = new Map<string, FormGroup>();

  // Computed values for stats
  activeGroupsCount = signal(0);
  totalSettingsCount = signal(0);

  ngOnInit(): void {
    this.loadSmtpGroups();
  }

  loadSmtpGroups(): void {
    this.loading.set(true);
    this.systemSettingsService.getAllSettings(undefined, true).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.processSmtpGroups(response.data);
        }
        this.loading.set(false);
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'SMTP ayarları yüklenemedi';
        this.toastr.error(errorMessage);
        this.loading.set(false);
      }
    });
  }

  processSmtpGroups(allSettings: SystemSetting[]): void {
    // EMAIL_* kategorilerini bul (EMAIL_KATEGORI, EMAIL_ORTAK, EMAIL_IMZA hariç)
    // EMAIL_ORTAK = Ortak SMTP sunucu ayarları (SMTP_SERVER, SMTP_PORT, ENABLE_SSL) - Genel Sistem Ayarları'nda
    const smtpCategories = [...new Set(
      allSettings
        .filter(s => s.category.startsWith('EMAIL_') &&
                    s.category !== 'EMAIL_KATEGORI' &&
                    s.category !== 'EMAIL_ORTAK' &&
                    s.category !== 'EMAIL_IMZA')
        .map(s => s.category)
    )];

    const groups: SmtpGroup[] = smtpCategories.map(category => {
      const categorySettings = allSettings.filter(s => s.category === category);
      const displayName = category.replace('EMAIL_', '').replace('_', ' ');

      const fromEmailSetting = categorySettings.find(s => s.key === 'FROM_EMAIL');
      const fromNameSetting = categorySettings.find(s => s.key === 'FROM_NAME');
      const smtpUsernameSetting = categorySettings.find(s => s.key === 'SMTP_USERNAME');
      const smtpPasswordSetting = categorySettings.find(s => s.key === 'SMTP_PASSWORD');

      const fromEmail = fromEmailSetting?.value || '';
      const fromName = fromNameSetting?.value || '';
      const smtpUsername = smtpUsernameSetting?.value || '';
      const smtpPassword = smtpPasswordSetting?.value || '';

      // Form oluştur
      const form = this.fb.group({
        fromEmail: [fromEmail, [Validators.required, Validators.email]],
        fromName: [fromName, [Validators.required]],
        smtpUsername: [smtpUsername],
        smtpPassword: [smtpPassword]
      });

      this.groupForms.set(category, form);

      return {
        category,
        displayName,
        settings: { fromEmail, fromName, smtpUsername, smtpPassword },
        fromEmailId: fromEmailSetting?.id,
        fromNameId: fromNameSetting?.id,
        smtpUsernameId: smtpUsernameSetting?.id,
        smtpPasswordId: smtpPasswordSetting?.id,
        isExpanded: false
      };
    });

    this.smtpGroups.set(groups);

    // Update stats
    this.activeGroupsCount.set(groups.length);
    this.totalSettingsCount.set(groups.length * 4); // Her grupta 4 ayar var
  }

  toggleGroup(group: SmtpGroup): void {
    group.isExpanded = !group.isExpanded;
    this.smtpGroups.update(g => [...g]);
  }

  testConnection(group: SmtpGroup): void {
    this.loading.set(true);

    this.systemSettingsService.testSmtpConnection(group.category).subscribe({
      next: (response) => {
        this.loading.set(false);
        if (response.success) {
          this.toastr.success(response.message || 'SMTP bağlantı testi başarılı!', 'Başarılı');

          // Detayları göster (opsiyonel)
          if (response.data?.details) {
            const details = response.data.details;
          }
        } else {
          this.toastr.error(response.message || 'SMTP bağlantı testi başarısız', 'Hata');
        }
      },
      error: (error) => {
        this.loading.set(false);
        console.error('SMTP test error:', error);
        this.toastr.error(error.error?.message || 'SMTP bağlantı testi sırasında hata oluştu', 'Hata');
      }
    });
  }

  getGroupForm(category: string): FormGroup | undefined {
    return this.groupForms.get(category);
  }

  hasChanges(category: string): boolean {
    const form = this.groupForms.get(category);
    return form?.dirty || false;
  }

  resetGroup(group: SmtpGroup): void {
    const form = this.groupForms.get(group.category);
    if (form) {
      form.patchValue({
        fromEmail: group.settings.fromEmail,
        fromName: group.settings.fromName,
        smtpUsername: group.settings.smtpUsername,
        smtpPassword: group.settings.smtpPassword
      });
      form.markAsPristine();
    }
  }

  async saveGroup(group: SmtpGroup): Promise<void> {
    const form = this.groupForms.get(group.category);
    if (!form || !form.valid) return;

    const result = await Swal.fire({
      title: 'SMTP Ayarlarını Kaydet',
      html: `<strong>${group.displayName}</strong> SMTP ayarlarını kaydetmek istediğinizden emin misiniz?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Kaydet',
      cancelButtonText: 'İptal'
    });

    if (!result.isConfirmed) return;

    this.savingGroup.set(group.category);
    const formValue = form.value;

    // Bulk update isteği hazırla
    const bulkUpdates: any[] = [];

    if (group.fromEmailId) {
      bulkUpdates.push({ id: group.fromEmailId, value: formValue.fromEmail });
    }
    if (group.fromNameId) {
      bulkUpdates.push({ id: group.fromNameId, value: formValue.fromName });
    }
    if (group.smtpUsernameId) {
      bulkUpdates.push({ id: group.smtpUsernameId, value: formValue.smtpUsername || '' });
    }
    if (group.smtpPasswordId) {
      bulkUpdates.push({ id: group.smtpPasswordId, value: formValue.smtpPassword || '' });
    }

    this.systemSettingsService.bulkUpdateSettings(bulkUpdates).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('SMTP ayarları başarıyla kaydedildi');
          form.markAsPristine();
          this.loadSmtpGroups();
        } else {
          this.toastr.error(response.message || 'Kaydetme başarısız');
        }
        this.savingGroup.set(null);
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'SMTP ayarları kaydedilirken hata oluştu';
        this.toastr.error(errorMessage);
        this.savingGroup.set(null);
      }
    });
  }

  async createSmtpGroup(): Promise<void> {
    const result = await Swal.fire({
      title: 'Yeni SMTP Grubu Oluştur',
      html: `
        <input id="groupName" class="swal2-input" placeholder="Grup Adı (örn: HASTANE)" style="width: 80%;" autocomplete="off">
        <input id="fromEmail" class="swal2-input" type="email" placeholder="Gönderen Email" style="width: 80%;" autocomplete="email">
        <input id="fromName" class="swal2-input" placeholder="Gönderen Adı" style="width: 80%;" autocomplete="name">
        <input id="smtpUsername" class="swal2-input" placeholder="SMTP Kullanıcı Adı (opsiyonel)" style="width: 80%;" autocomplete="username">
        <input id="smtpPassword" class="swal2-input" type="password" placeholder="SMTP Şifre (opsiyonel)" style="width: 80%;" autocomplete="current-password">
      `,
      showCancelButton: true,
      confirmButtonText: 'Oluştur',
      cancelButtonText: 'İptal',
      preConfirm: () => {
        const groupName = (document.getElementById('groupName') as HTMLInputElement).value.toUpperCase();
        const fromEmail = (document.getElementById('fromEmail') as HTMLInputElement).value;
        const fromName = (document.getElementById('fromName') as HTMLInputElement).value;
        const smtpUsername = (document.getElementById('smtpUsername') as HTMLInputElement).value;
        const smtpPassword = (document.getElementById('smtpPassword') as HTMLInputElement).value;

        if (!groupName || !fromEmail || !fromName) {
          Swal.showValidationMessage('Grup adı, email ve gönderen adı zorunludur');
          return false;
        }

        return { groupName, fromEmail, fromName, smtpUsername, smtpPassword };
      }
    });

    if (!result.isConfirmed || !result.value) return;

    const { groupName, fromEmail, fromName, smtpUsername, smtpPassword } = result.value;
    const category = `EMAIL_${groupName}`;

    // 4 ayar oluştur
    const settings: CreateSystemSettingRequest[] = [
      {
        category,
        key: 'FROM_EMAIL',
        value: fromEmail,
        description: `${groupName} gönderen email adresi`,
        isSecret: false,
        isActive: true
      },
      {
        category,
        key: 'FROM_NAME',
        value: fromName,
        description: `${groupName} gönderen adı`,
        isSecret: false,
        isActive: true
      },
      {
        category,
        key: 'SMTP_USERNAME',
        value: smtpUsername || '',
        description: `${groupName} SMTP kullanıcı adı`,
        isSecret: false,
        isActive: true
      },
      {
        category,
        key: 'SMTP_PASSWORD',
        value: smtpPassword || '',
        description: `${groupName} SMTP şifresi`,
        isSecret: true,
        isActive: true
      }
    ];

    // Sırayla oluştur
    let successCount = 0;
    for (const setting of settings) {
      this.systemSettingsService.createSetting(setting).subscribe({
        next: (response) => {
          if (response.success) successCount++;
          if (successCount === 4) {
            this.toastr.success('SMTP grubu başarıyla oluşturuldu');
            this.loadSmtpGroups();
          }
        },
        error: (error) => {
          const errorMessage = error?.error?.message || 'Ayar oluşturma hatası';
          this.toastr.error(errorMessage);
        }
      });
    }
  }

  deleteGroup(group: SmtpGroup, event: Event): void {
    event.stopPropagation();
    this.deleteSmtpGroup(group);
  }

  async deleteSmtpGroup(group: SmtpGroup): Promise<void> {
    // EMAIL_SISTEM kategorisi silinemez (sistem bildirimleri için özel kategori)
    if (group.category === 'EMAIL_SISTEM') {
      await Swal.fire({
        title: 'Sistem Kategorisi',
        html: '<strong>EMAIL_SISTEM</strong> kategorisi sistem bildirimleri için özel bir kategoridir ve silinemez.',
        icon: 'error',
        confirmButtonText: 'Tamam'
      });
      return;
    }

    const result = await Swal.fire({
      title: 'SMTP Grubunu Sil',
      html: `<strong>${group.displayName}</strong> SMTP grubunu ve tüm ayarlarını silmek istediğinizden emin misiniz?<br><br>Bu işlem geri alınamaz!`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Sil',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#d33'
    });

    if (!result.isConfirmed) return;

    // Tüm EMAIL_* ayarlarını bul ve sil
    this.systemSettingsService.getAllSettings(group.category, true).subscribe({
      next: async (response) => {
        if (response.success && response.data) {
          // Tüm silme işlemlerini sırayla bekle
          for (const setting of response.data) {
            await new Promise<void>((resolve) => {
              this.systemSettingsService.deleteSetting(setting.id).subscribe({
                next: () => resolve(),
                error: () => resolve() // Hata olsa bile devam et
              });
            });
          }
          this.toastr.success('SMTP grubu başarıyla silindi');
          this.loadSmtpGroups();
        }
      },
      error: () => this.toastr.error('SMTP grubu silinirken hata oluştu')
    });
  }


  trackByGroup(index: number, group: SmtpGroup): string {
    return group.category;
  }
}
