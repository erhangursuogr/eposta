import { Component, inject, signal } from '@angular/core';

import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatStepperModule } from '@angular/material/stepper';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ToastrService } from 'ngx-toastr';

import { EmailGroupService } from '../../../../services/email-group.service';
import { GrupTipi, CreateEmailGroupRequest } from '../../../../common/models/email-group.model';

@Component({
  selector: 'app-email-group-create-dialog',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatStepperModule,
    MatCardModule,
    MatChipsModule,
    MatProgressSpinnerModule
],
  templateUrl: './email-group-create-dialog.html',
  styleUrl: './email-group-create-dialog.css'
})
export class EmailGroupCreateDialog {
  private dialogRef = inject(MatDialogRef<EmailGroupCreateDialog>);
  private emailGroupService = inject(EmailGroupService);
  private fb = inject(FormBuilder);
  private toastr = inject(ToastrService);

  // State
  loading = signal<boolean>(false);
  selectedType = signal<GrupTipi | null>(null);

  // Forms
  basicInfoForm!: FormGroup;
  normalGroupForm!: FormGroup;
  statikGroupForm!: FormGroup;
  dinamikGroupForm!: FormGroup;
  debisGroupForm!: FormGroup;

  // Normal grup için üye listesi
  normalMembers: Array<{email: string, adSoyad: string}> = [];
  newMemberEmail = '';
  newMemberName = '';

  // Statik grup için dosya
  selectedFile: File | null = null;
  filePreview: Array<{email: string, adSoyad?: string}> = [];

  // Dinamik grup için preview
  dynamicPreview: Array<{email: string, adSoyad?: string}> = [];
  dynamicTotalCount = 0;
  previewLoading = false;

  // Grup tipleri
  grupTipleri = [
    {
      value: GrupTipi.NORMAL,
      label: 'Standart Grup',
      icon: 'group',
      description: 'Manuel olarak üye ekleyip çıkarabileceğiniz klasik grup. TO/CC/BCC seçenekleri kullanılabilir.',
      color: '#1976D2'
    },
    {
      value: GrupTipi.STATIK,
      label: 'Dosyadan Yüklenen Grup',
      icon: 'upload_file',
      description: 'Excel/CSV/TXT dosyasından toplu üye yükleme. Sadece BCC olarak kullanılabilir.',
      color: '#7B1FA2'
    },
    {
      value: GrupTipi.DINAMIK,
      label: 'Dinamik View Grubu',
      icon: 'sync',
      description: 'Oracle View\'den gerçek zamanlı üye sorgulama. Her gönderimde güncel liste kullanılır. Sadece BCC.',
      color: '#388E3C'
    },
    {
      value: GrupTipi.DEBIS,
      label: 'Debis Listeci Grubu',
      icon: 'email',
      description: 'Mevcut Debis listeci sistemi entegrasyonu. Sadece BCC olarak kullanılabilir.',
      color: '#F57C00'
    }
  ];


  ngOnInit(): void {
    this.initializeForms();
  }

  initializeForms(): void {
    // Temel bilgi formu (tüm tipler için)
    this.basicInfoForm = this.fb.group({
      grupAdi: ['', [Validators.required, Validators.maxLength(100)]],
      aciklama: ['', Validators.maxLength(500)]
    });

    // NORMAL grup formu
    this.normalGroupForm = this.fb.group({});

    // STATIK grup formu
    this.statikGroupForm = this.fb.group({});

    // DINAMIK grup formu
    this.dinamikGroupForm = this.fb.group({
      viewAdi: ['', Validators.required],
      filterKosulu: ['', Validators.maxLength(500)]
    });

    // DEBIS grup formu (sadece bilgilendirme, input yok)
    this.debisGroupForm = this.fb.group({});
  }

  selectType(type: GrupTipi): void {
    this.selectedType.set(type);
  }

  // NORMAL grup - üye ekleme
  addNormalMember(): void {
    if (!this.newMemberEmail || !this.newMemberName) {
      this.toastr.warning('Email ve ad soyad zorunludur');
      return;
    }

    // Email düzenleme - @deu.edu.tr otomatik ekleme
    let email = this.newMemberEmail.trim();

    // Eğer @ yoksa veya @deu.edu.tr yoksa ekle
    if (!email.includes('@')) {
      email = email + '@deu.edu.tr';
    } else if (!email.endsWith('@deu.edu.tr')) {
      // @ var ama @deu.edu.tr değil - kullanıcı başka domain girmiş
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(email)) {
        this.toastr.error('Geçerli bir email adresi giriniz');
        return;
      }
      // Geçerli ama farklı domain - kabul et
    }

    // Duplicate kontrolü
    if (this.normalMembers.some(m => m.email === email)) {
      this.toastr.warning('Bu email adresi zaten ekli');
      return;
    }

    this.normalMembers.push({
      email: email,
      adSoyad: this.newMemberName
    });

    // Formu temizle
    this.newMemberEmail = '';
    this.newMemberName = '';
  }

  removeNormalMember(index: number): void {
    this.normalMembers.splice(index, 1);
  }

  // STATIK grup - dosya seçme
  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    const validExtensions = ['.xlsx', '.xls', '.csv', '.txt'];
    const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();

    if (!validExtensions.includes(extension)) {
      this.toastr.error('Sadece Excel (.xlsx, .xls), CSV (.csv) veya TXT (.txt) dosyaları yüklenebilir');
      return;
    }

    this.selectedFile = file;
    this.previewFile(file);
  }

  previewFile(file: File): void {
    const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();

    // Excel dosyaları için önizleme gösterme (binary format)
    if (extension === '.xlsx' || extension === '.xls') {
      this.filePreview = [];
      this.toastr.info('Excel dosyası seçildi. Önizleme sadece CSV/TXT için desteklenir. Upload sonrası sonuç görüntülenecek.');
      return;
    }

    // CSV ve TXT için text preview
    const reader = new FileReader();
    reader.onload = (e: any) => {
      const content = e.target.result;
      // Basit preview - ilk 10 satır
      const lines = content.split('\n').slice(0, 10);
      this.filePreview = lines
        .filter((line: string) => line.trim())
        .map((line: string) => {
          const parts = line.split(/[,;\t]/);
          return {
            email: parts[0]?.trim() || '',
            adSoyad: parts[1]?.trim()
          };
        });
    };
    reader.readAsText(file);
  }

  // Form submit
  onSubmit(): void {
    if (this.basicInfoForm.invalid) {
      this.toastr.warning('Lütfen tüm zorunlu alanları doldurunuz');
      return;
    }

    const selectedType = this.selectedType();
    if (!selectedType) {
      this.toastr.warning('Lütfen bir grup tipi seçiniz');
      return;
    }

    // Tip bazlı validasyon
    if (selectedType === GrupTipi.NORMAL && this.normalMembers.length === 0) {
      this.toastr.warning('En az bir üye eklemelisiniz');
      return;
    }

    if (selectedType === GrupTipi.STATIK && !this.selectedFile) {
      this.toastr.warning('Lütfen bir dosya seçiniz');
      return;
    }

    if (selectedType === GrupTipi.DINAMIK && this.dinamikGroupForm.invalid) {
      this.toastr.warning('Lütfen view adını seçiniz');
      return;
    }

    const request: CreateEmailGroupRequest = {
      grupAdi: this.basicInfoForm.value.grupAdi,
      aciklama: this.basicInfoForm.value.aciklama,
      grupTipi: selectedType
    };

    // Tip bazlı ek alanlar
    if (selectedType === GrupTipi.NORMAL) {
      request.statikUyeler = this.normalMembers;
    }

    if (selectedType === GrupTipi.DINAMIK) {
      request.viewAdi = this.dinamikGroupForm.value.viewAdi;
      request.filterKosulu = this.dinamikGroupForm.value.filterKosulu;
    }

    this.createGroup(request);
  }

  private createGroup(request: CreateEmailGroupRequest): void {
    this.loading.set(true);

    // STATIK grup için dosya upload
    if (this.selectedType() === GrupTipi.STATIK && this.selectedFile) {
      this.createStatikGroupWithFile(request);
      return;
    }

    // Diğer tipler için normal create
    this.emailGroupService.createGroup(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Grup başarıyla oluşturuldu');
          this.dialogRef.close(true);
        } else {
          this.toastr.error(response.message || 'Grup oluşturulamadı');
          this.loading.set(false);
        }
      },
      error: (error) => {
        console.error('Create group error:', error);
        this.toastr.error(error.error?.message || 'Grup oluşturulurken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  private createStatikGroupWithFile(request: CreateEmailGroupRequest): void {
    // İlk önce grup oluştur
    this.emailGroupService.createGroup(request).subscribe({
      next: (response: any) => {
        if (response.success) {
          // Grup oluşturulduktan sonra dosya upload
          const groupId = response.data?.id;
          if (groupId && this.selectedFile) {
            this.uploadFileToGroup(groupId);
          } else {
            this.toastr.success('Grup oluşturuldu, ancak dosya yüklenemedi');
            this.dialogRef.close(true);
          }
        } else {
          this.toastr.error(response.message || 'Grup oluşturulamadı');
          this.loading.set(false);
        }
      },
      error: (error) => {
        console.error('Create group error:', error);
        this.toastr.error(error.error?.message || 'Grup oluşturulurken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  private uploadFileToGroup(groupId: number): void {
    if (!this.selectedFile) return;

    this.emailGroupService.importMembers(groupId, this.selectedFile).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success(response.data?.message || 'Dosya başarıyla yüklendi');
          this.dialogRef.close(true);
        } else {
          this.toastr.error(response.message || 'Dosya yüklenemedi');
          this.loading.set(false);
        }
      },
      error: (error) => {
        console.error('Upload file error:', error);
        this.toastr.error(error.error?.message || 'Dosya yüklenirken hata oluştu');
        this.loading.set(false);
      }
    });
  }

  // DINAMIK grup - view preview
  previewDynamicGroup(): void {
    const viewAdi = this.dinamikGroupForm.value.viewAdi?.trim();
    const filterKosulu = this.dinamikGroupForm.value.filterKosulu?.trim();

    if (!viewAdi) {
      this.toastr.warning('View adını giriniz');
      return;
    }

    this.previewLoading = true;
    this.dynamicPreview = [];
    this.dynamicTotalCount = 0;

    this.emailGroupService.previewDynamicGroup({ viewAdi, filterKosulu }).subscribe({
      next: (response) => {
        this.previewLoading = false;
        if (response.success && response.data) {
          const preview = response.data;
          if (preview.isValid) {
            this.dynamicTotalCount = preview.toplamUye;
            this.dynamicPreview = preview.onizlemeUyeler.map(m => ({
              email: m.email,
              adSoyad: m.adSoyad
            }));
            this.toastr.success(`View sorgusu başarılı! Toplam ${preview.toplamUye} üye bulundu.`);
          } else {
            this.toastr.error(preview.errorMessage || 'View sorgusu başarısız');
          }
        } else {
          this.toastr.error(response.message || 'Önizleme başarısız');
        }
      },
      error: (error) => {
        this.previewLoading = false;
        console.error('Preview error:', error);
        const errorMsg = error.error?.message || error.error?.data?.errorMessage || 'View sorgulanırken hata oluştu';
        this.toastr.error(errorMsg);
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
