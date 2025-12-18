import { Component, OnInit, OnDestroy, AfterViewInit, inject, signal, ViewEncapsulation, ViewChild, ElementRef, DestroyRef, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormControl, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { environment } from '../../../../environments/environment';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatStepperModule } from '@angular/material/stepper';
import { MatRadioModule } from '@angular/material/radio';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import suneditor from 'suneditor';
import tr from 'suneditor/src/lang/tr';
import plugins from 'suneditor/src/plugins';

import { AnnouncementService } from '../../../services/announcement.service';
import { UploadedFile } from '../../../services/file.service';
import { ApiResponse } from '../../../common/models/api-response.model';
import { UserDataService } from '../../../services/userdata.service';
import { TemplateService } from '../../../services/template.service';
import { AnnouncementStatus } from '../../../common/models/announcement.model';
import { MatDivider } from "@angular/material/divider";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatDialogModule } from '@angular/material/dialog';
import { AnnouncementFileManagerService } from './services/announcement-file-manager.service';
import { AnnouncementFormStateService } from './services/announcement-form-state.service';
import { AnnouncementWorkflowService } from './services/announcement-workflow.service';

@Component({
  selector: 'app-announcement-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatChipsModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCheckboxModule,
    MatCardModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatStepperModule,
    MatRadioModule,
    MatDivider,
    MatProgressSpinnerModule,
    MatDialogModule
],
  templateUrl: './announcement-form.html',
  styleUrl: './announcement-form.css',
  encapsulation: ViewEncapsulation.None
})
export class AnnouncementForm implements OnInit, AfterViewInit, OnDestroy {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private announcementService = inject(AnnouncementService);
  private templateService = inject(TemplateService);
  private toastr = inject(ToastrService);
  private http = inject(HttpClient);
  private userDataService = inject(UserDataService);
  private destroyRef = inject(DestroyRef);


  // Services
  fileManager = inject(AnnouncementFileManagerService);
  formState = inject(AnnouncementFormStateService);
  workflow = inject(AnnouncementWorkflowService);

  // Current user
  currentUser = computed(() => this.userDataService.user());
  userRole = computed(() => this.userDataService.user().role);

  // Merkez personeli kontrolü (gorevYeri === 0 ise merkez)
  isMerkezPersoneli = computed(() => this.userDataService.user().gorevYeri === 0);

  // SunEditor
  @ViewChild('suneditor', { static: false }) editorElement!: ElementRef;
  private editor: any;

  // Form (delegated to FormStateService)
  get announcementForm(): FormGroup {
    return this.formState.announcementForm;
  }
  emailControl = new FormControl('');

  // State
  loading = signal<boolean>(false);
  isEditMode = signal<boolean>(false);
  announcementId?: number;
  sessionId: string = ''; // Upload session ID (backend'den gelecek)
  sessionIdReady = signal<boolean>(false); // GÜVENLİK: SessionID hazır mı? (race condition önlemi)
  announcement = signal<any>(null); // Duyuru bilgisi (red notları için)
  announcementStatus = signal<AnnouncementStatus | null>(null); // Duyuru durumu
  approvalConfirmed = signal<boolean>(false); // ADMIN/MANAGER için onay checkbox
  approvalNote = signal<string>(''); // MANAGER/ADMIN onay notu

  // Expose enum to template
  readonly AnnouncementStatus = AnnouncementStatus;

  // Form State Signals (delegated to service)
  emailGroups = this.formState.emailGroups;
  selectedGroups = this.formState.selectedGroups;
  manualEmails = this.formState.manualEmails;
  approvers = this.formState.approvers;
  categories = this.formState.categories;
  senderCategories = this.formState.senderCategories;

  // File Manager Signals (delegated to service)
  uploadedFiles = this.fileManager.uploadedFiles;
  uploadProgress = this.fileManager.uploadProgress;
  isDraggingOver = this.fileManager.isDraggingOver;

  ngOnInit(): void {
    // Form ve state'i sıfırla (singleton servis olduğu için önceki değerler kalabilir)
    this.formState.resetForm();
    this.formState.initForm();
    this.formState.loadEmailGroups();
    this.loadApprovers();
    this.loadCategories();
    this.loadSenderCategories();
    this.generateSecureSessionId(); // SECURITY: Backend'den güvenli session ID al

    // Merkez personeli değilse (gorevYeri !== 0), varsayılan olarak EMAIL_DUYURU kullan
    // ve gondericiKategori alanından required validasyonunu kaldır
    if (!this.isMerkezPersoneli()) {
      this.formState.announcementForm.patchValue({ gondericiKategori: 'EMAIL_DUYURU' });
      this.formState.announcementForm.get('gondericiKategori')?.clearValidators();
      this.formState.announcementForm.get('gondericiKategori')?.updateValueAndValidity();
    }

    // Check if edit mode
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      if (params['id']) {
        this.announcementId = +params['id'];
        this.isEditMode.set(true);
        this.loadAnnouncement(this.announcementId);
      }
    });

    // Check for template query param
    this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      if (params['templateId']) {
        this.loadTemplate(+params['templateId']);
      }
    });
  }

  ngAfterViewInit(): void {
    if (this.editorElement) {
      this.editor = suneditor.create(this.editorElement.nativeElement, {
        lang: tr,
        plugins: plugins,
        placeholder: 'Yazı tipi Times New Roman, boyut 12pt, olarak ayarlanmıştır. Lütfen içeriğinizi buraya yapıştırın veya yazın.',
        popupDisplay: 'local',
        font: ['Arial', 'Calibri', 'Comic Sans MS', 'Courier New', 'Georgia', 'Impact', 'Tahoma', 'Times New Roman', 'Verdana'],
        minHeight: '40rem',
        height: '700',
        width: '100%',
        buttonList: [
          ['undo', 'redo'],
          ['font', 'fontSize', 'formatBlock'],
          ['bold', 'underline', 'italic', 'strike', 'subscript', 'superscript','paragraphStyle','blockquote','textStyle'],
          ['fontColor', 'hiliteColor'],
          ['removeFormat'],
          ['outdent', 'indent'],
          ['align', 'horizontalRule', 'list', 'table'],
          ['link', 'image', 'video'],
          ['fullScreen', 'showBlocks', 'codeView'],
          ['preview', 'print']
        ],
        imageResizing: true,
        imageHeightShow: false,
        imageFileInput: true,
        imageUploadSizeLimit: 2 * 1024 * 1024, // 2MB per image (base64 embed)
        imageUrlInput: true, // URL girişi etkinleştir
        videoFileInput: false,
        callBackSave: (contents: string) => {
          this.announcementForm.patchValue({ icerik: contents }, { emitEvent: false });
        },
        pasteTagsWhitelist: 'p|h1|h2|h3|h4|h5|h6|ul|ol|li|strong|em|u|s|a|img|table|thead|tbody|tr|th|td|br|span|div',
        attributesWhitelist: {
          // GÜVENLİK: 'all: style' yerine sadece gerekli taglarda style izni
          p: 'style',
          div: 'style',
          span: 'style',
          table: 'cellpadding|cellspacing|border|style',
          td: 'style',
          th: 'style',
          img: 'src|alt|style|width|height',
          a: 'href|target|rel'
        },
        // Word paste config
        addTagsWhitelist: 'p|div|pre|blockquote|h1|h2|h3|h4|h5|h6|ol|ul|li|hr|figure|figcaption|img|iframe|audio|video|table|thead|tbody|tr|th|td|a|b|strong|var|i|em|u|ins|s|span|strike|del|sub|sup|code|svg|path',
        pasteTagsBlacklist: '',
        imageAccept: '.jpg,.jpeg,.png,.gif,.bmp,.webp',
        defaultStyle: 'font-family: Times New Roman; font-size: 12pt; line-height: 1.5;'
      });

      // Sync with form control
      this.editor.onChange = (contents: string) => {
        // PERFORMANS: emitEvent: true ile validation tetiklenir, dirty/touched state güncellenir
        this.announcementForm.patchValue({ icerik: contents }, { emitEvent: true });
        this.announcementForm.get('icerik')?.markAsTouched();
      };

      // Set initial value if editing
      const initialValue = this.announcementForm.get('icerik')?.value;
      if (initialValue) {
        this.editor.setContents(initialValue);
      }
    }
  }

  ngOnDestroy(): void {
    if (this.editor) {
      this.editor.destroy();
    }
  }

  // private confirmCancel(): void {
  //   // Form değişmişse onay sor
  //   if (this.announcementForm.dirty) {
  //     Swal.fire({
  //       title: 'Değişiklikler kaydedilmedi',
  //       text: 'Kaydedilmemiş değişiklikler var. Çıkmak istediğinizden emin misiniz?',
  //       icon: 'warning',
  //       showCancelButton: true,
  //       confirmButtonText: 'Evet, Çık',
  //       cancelButtonText: 'İptal',
  //       confirmButtonColor: '#d33'
  //     }).then((result) => {
  //       if (result.isConfirmed) {
  //         this.router.navigate(['/duyurular']);
  //       }
  //     });
  //   } else {
  //     this.router.navigate(['/duyurular']);
  //   }
  // }


  private loadApprovers(): void {
    // İki aşamalı onay sisteminde:
    // - EDITOR onaya gönderirken yönetici seçmez (onaylayanKullaniciId = null)
    // - Kontrolör onaylarken manager seçer
    // Bu nedenle form'dan onaylayıcı seçimi kaldırıldı
  }

  private loadCategories(): void {
    this.announcementService.getCategories().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.formState.setCategories(response.data);
        }
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'İmza kategori listesi yüklenemedi';
        this.toastr.error(errorMessage);
      }
    });
  }

  private loadSenderCategories(): void {
    // SMTP gönderici kategorilerini yükle
    this.http.get<ApiResponse<any>>(`${environment.apiUrl}/api/admin/system-settings/smtp-categories`).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          // EMAIL_DUYURU kategorisini filtrele (sadece sistem tarafından kullanılır)
          const filtered = response.data.filter((cat: any) => cat.key !== 'EMAIL_SISTEM');
          this.formState.setSenderCategories(filtered);
        }
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Gönderici email listesi yüklenemedi';
        this.toastr.error(errorMessage);
        // Fallback: Default değer
        this.formState.setSenderCategories([
          { key: 'EMAIL_DUYURU', displayName: 'Genel Duyuru', email: 'duyuru@deu.edu.tr' }
        ]);
      }
    });
  }

  private loadAnnouncement(id: number): void {
    this.loading.set(true);
    this.announcementService.getAnnouncementById(id).pipe(
      finalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const announcement = response.data;

          // Duyuru bilgisini signal'e set et (red notları için)
          this.announcement.set(announcement);

          // Duyuru durumunu set et
          this.announcementStatus.set(announcement.durum);

          this.announcementForm.patchValue({
            konu: announcement.konu,
            icerik: announcement.icerik,
            duyuruKategorisi: announcement.duyuruKategorisi || '',
            gondericiKategori: announcement.gondericiKategori || 'EMAIL_DUYURU',
            onaylayanKullaniciId: announcement.onaylayanKullaniciId,
            aciklama: announcement.aciklama || ''
          });

          // Load recipient groups
          if (announcement.grupIdList && announcement.grupIdList.length > 0) {
            this.selectedGroups.set(announcement.grupIdList);
          }

          // Load manual emails
          if (announcement.aliciEmailList && announcement.aliciEmailList.length > 0) {
            this.manualEmails.set(announcement.aliciEmailList);
          }

          // Load files if any
          if (announcement.dosyaVarMi) {
            this.loadAnnouncementFiles(id);
          }

          // Set editor content after view init
          if (this.editor && announcement.icerik) {
            this.editor.setContents(announcement.icerik);
          }
        }
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Duyuru yüklenemedi';
        this.toastr.error(errorMessage);
        this.router.navigate(['/duyurular']);
      }
    });
  }

  private loadAnnouncementFiles(announcementId: number): void {
    this.announcementService.getAnnouncementFiles(announcementId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          // Map backend Dosya to frontend UploadedFile
          const files: UploadedFile[] = response.data.map((f: any) => ({
            id: f.id,
            dosyaAdi: f.dosyaAdi,
            dosyaYolu: f.dosyaYolu,
            dosyaBoyutu: f.dosyaBoyutu,
            mimeType: f.dosyaTipi,
            yuklemeTarihi: f.yuklemeTarihi,
            yukleyenKullaniciId: f.yukleyenKullaniciId,
            yukleyenKullaniciAdi: f.yukleyenKullanici?.adSoyad || ''
          }));
          this.fileManager.setUploadedFiles(files);
        }
      }
    });
  }

  // Group selection
  // Group & Email Management (delegated to FormStateService)
  toggleGroup(groupId: number): void {
    this.formState.toggleGroup(groupId);
  }

  isGroupSelected(groupId: number): boolean {
    return this.formState.isGroupSelected(groupId);
  }

  addEmail(): void {
    const email = this.emailControl.value?.trim();
    if (email && this.formState.addEmail(email)) {
      this.emailControl.setValue('');
    }
  }

  removeEmail(email: string): void {
    this.formState.removeEmail(email);
  }

  // File upload (delegated to FileManagerService)
  onFileSelected(event: any): void {
    // GÜVENLİK: SessionID hazır değilse dosya yüklemeyi engelle (race condition önlemi)
    if (!this.sessionIdReady()) {
      this.toastr.warning('Dosya yükleme hazırlanıyor, lütfen birkaç saniye bekleyin');
      // Input'u temizle ki kullanıcı tekrar denesin
      if (event.target) {
        event.target.value = '';
      }
      return;
    }
    this.fileManager.onFileSelected(event, this.announcementId, this.sessionId);
  }

  removeFile(fileId: number): void {
    this.fileManager.removeFile(fileId);
  }

  formatFileSize(bytes: number): string {
    return this.fileManager.formatFileSize(bytes);
  }

  getFileUrl(fileId: number): string {
    return this.fileManager.getFileUrl(fileId);
  }


  // Form submission (delegated to WorkflowService)
  saveDraft(): void {
    if (this.announcementForm.get('konu')?.invalid) {
      this.toastr.error('Başlık alanı zorunludur');
      return;
    }

    if (this.announcementForm.get('icerik')?.invalid) {
      this.toastr.error('İçerik alanı zorunludur');
      return;
    }

    if (this.selectedGroups().length === 0 && this.manualEmails().length === 0) {
      this.toastr.error('En az bir alıcı grubu veya e-posta adresi seçmelisiniz');
      return;
    }

    const formData = this.formState.getFormData();
    const editorContent = this.editor?.getContents() || '';

    this.workflow.saveDraft(
      this.announcementId,
      this.isEditMode(),
      formData,
      editorContent,
      this.sessionId
    );
  }

  submitForApproval(): void {
    if (this.announcementForm.invalid) {
      this.toastr.error('Lütfen tüm zorunlu alanları doldurun');
      this.announcementForm.markAllAsTouched();
      return;
    }

    if (this.selectedGroups().length === 0 && this.manualEmails().length === 0) {
      this.toastr.error('En az bir alıcı grubu veya e-posta adresi seçmelisiniz');
      return;
    }

    const formData = this.formState.getFormData();
    const editorContent = this.editor?.getContents() || '';

    this.workflow.submitForApproval(
      this.announcementId,
      this.isEditMode(),
      formData,
      editorContent,
      this.sessionId
    );
  }

  // ADMIN/MANAGER için: Sadece Onayla (gönderme)
  approve(): void {
    if (!this.announcementId) {
      this.toastr.error('Duyuru ID bulunamadı');
      return;
    }

    this.workflow.approve(
      this.announcementId,
      this.approvalNote(),
      this.announcementForm.value.konu
    );
  }

  // EDITOR için: Onaylanmış duyuruyu gönder (onay kutucuğu kontrolü yok)
  sendApprovedAnnouncement(): void {
    if (this.announcementForm.invalid) {
      this.toastr.error('Lütfen tüm zorunlu alanları doldurun');
      this.announcementForm.markAllAsTouched();
      return;
    }

    if (this.selectedGroups().length === 0 && this.manualEmails().length === 0) {
      this.toastr.error('En az bir alıcı grubu veya e-posta adresi seçmelisiniz');
      return;
    }

    const totalRecipients = this.workflow.getTotalRecipientCount(
      this.emailGroups(),
      this.selectedGroups(),
      this.manualEmails()
    );

    this.workflow.sendApprovedAnnouncement(
      this.announcementId!,
      this.announcementForm.value.konu,
      totalRecipients
    );
  }

  // ADMIN/MANAGER için: Onayla ve Gönder
  approveAndSend(): void {
    if (this.announcementForm.invalid) {
      this.toastr.error('Lütfen tüm zorunlu alanları doldurun');
      this.announcementForm.markAllAsTouched();
      return;
    }

    if (this.selectedGroups().length === 0 && this.manualEmails().length === 0) {
      this.toastr.error('En az bir alıcı grubu veya e-posta adresi seçmelisiniz');
      return;
    }

    const totalRecipients = this.workflow.getTotalRecipientCount(
      this.emailGroups(),
      this.selectedGroups(),
      this.manualEmails()
    );

    this.workflow.approveAndSend(
      this.announcementId!,
      this.approvalConfirmed(),
      this.approvalNote(),
      this.announcementForm.value.konu,
      totalRecipients
    );
  }


  cancel(): void {
    this.router.navigate(['/duyurular']);
  }

  // Preview
  previewAnnouncement(): void {
    if (!this.isEditMode() || !this.announcementId) {
      this.toastr.warning('Önizleme için önce duyuruyu kaydetmelisiniz');
      return;
    }

    if (!this.announcementForm.value.icerik) {
      this.toastr.warning('Önizleme için içerik girmelisiniz');
      return;
    }

    // Backend'den preview al (imza ile birlikte)
    this.announcementService.getAnnouncementPreview(this.announcementId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          // response.data bir obje (AnnouncementPreviewDto), HtmlContent içeriyor
          const htmlContent = (response.data as any).htmlContent || response.data;
          Swal.fire({
            title: this.announcementForm.value.konu || 'Duyuru Önizlemesi',
            html: `<div style="text-align:left;">${htmlContent}</div>`,
            width: '800px',
            showCloseButton: true,
            showConfirmButton: false,
            customClass: {
              popup: 'preview-popup'
            }
          });
        }
      },
      error: (error) => {
        this.toastr.error('Önizleme yüklenemedi');
        console.error('Preview error:', error);
      }
    });
  }

  // Test email
  sendTestEmail(): void {
    const editorContent = this.editor?.getContents() || '';

    if (!editorContent) {
      this.toastr.warning('Test e-postası göndermek için içerik girmelisiniz');
      return;
    }

    Swal.fire({
      title: 'Test E-postası Gönder',
      input: 'email',
      inputLabel: 'E-posta Adresi',
      inputPlaceholder: '@deu.edu.tr',
      showCancelButton: true,
      confirmButtonText: 'Gönder',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#004B87',
      inputValidator: (value) => {
        if (!value) {
          return 'E-posta adresi gerekli!';
        }
        if (!value.endsWith('@deu.edu.tr')) {
          return 'Sadece @deu.edu.tr uzantılı e-postalar!';
        }
        return null;
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        const formData = this.formState.getFormData();
        this.workflow.sendTestEmail(
          this.announcementId,
          formData,
          editorContent,
          result.value
        );
      }
    });
  }

getIconName(grupTipi: string): string {
  const iconMap: { [key: string]: string } = {
    DOSYA: 'group',
    DINAMIK: 'sync',
    MANUEL: 'view_list',
    DEBIS: 'email',
  };
  // Eşleşme yoksa varsayılan bir ikon döner
  return iconMap[grupTipi] || 'help_outline';
}

viewHistory(): void {
  if (!this.announcementId) {
    this.toastr.warning('Duyuru henüz kaydedilmedi');
    return;
  }

  this.announcementService.getAnnouncementMovements(this.announcementId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
    next: (response) => {
      if (response.success && response.data) {
        this.showHistoryModal(response.data);
      } else {
        this.toastr.error('Duyuru geçmişi yüklenemedi');
      }
    },
    error: (error) => {
      const errorMessage = error?.error?.message || 'Duyuru geçmişi yüklenirken hata oluştu';
      this.toastr.error(errorMessage);
    }
  });
}

private showHistoryModal(movements: any[]): void {
  const getStatusBadge = (durum: string): {icon: string, color: string, text: string} => {
    switch (durum) {
      case 'TASLAK': return {icon: '📝', color: '#9e9e9e', text: 'Taslak'};
      case 'ILK_ONAY_BEKLIYOR': return {icon: '⏳', color: '#2196f3', text: 'İlk Onay Bekliyor'};
      case 'SON_ONAY_BEKLIYOR': return {icon: '⏱️', color: '#ff9800', text: 'Son Onay Bekliyor'};
      case 'ONAYLANDI': return {icon: '✅', color: '#4caf50', text: 'Onaylandı'};
      case 'REDDEDILDI': return {icon: '❌', color: '#f44336', text: 'Reddedildi'};
      case 'GONDERILDI': return {icon: '📧', color: '#00bcd4', text: 'Gönderildi'};
      default: return {icon: 'ℹ️', color: '#757575', text: durum};
    }
  };

  const getActionIcon = (islemTipi: string): string => {
    switch (islemTipi) {
      case 'OLUSTURMA': return '➕';
      case 'GUNCELLEME': return '✏️';
      case 'ONAYA_GONDERME': return '📤';
      case 'ONAYLAMA': return '✓';
      case 'REDDETME': return '✗';
      case 'GONDERIM': return '📨';
      default: return '•';
    }
  };

  const timelineHtml = movements.map(m => {
    const statusBadge = getStatusBadge(m.yeniDurum);
    const actionIcon = getActionIcon(m.islemTipi);
    const oldStatus = m.oncekiDurum ? getStatusBadge(m.oncekiDurum) : null;

    return `
      <div style="padding: 16px; border-left: 3px solid ${statusBadge.color}; margin-bottom: 16px; background: #f5f5f5; border-radius: 4px;">
        <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
          <span style="font-size: 20px;">${actionIcon}</span>
          <strong style="color: #333;">${m.islemTipi.replace(/_/g, ' ')}</strong>
        </div>

        ${oldStatus ? `
          <div style="margin: 8px 0; font-size: 13px;">
            <span style="background: ${oldStatus.color}; color: white; padding: 2px 8px; border-radius: 3px;">${oldStatus.icon} ${oldStatus.text}</span>
            <span style="margin: 0 8px;">→</span>
            <span style="background: ${statusBadge.color}; color: white; padding: 2px 8px; border-radius: 3px;">${statusBadge.icon} ${statusBadge.text}</span>
          </div>
        ` : `
          <div style="margin: 8px 0; font-size: 13px;">
            <span style="background: ${statusBadge.color}; color: white; padding: 2px 8px; border-radius: 3px;">${statusBadge.icon} ${statusBadge.text}</span>
          </div>
        `}

        <div style="color: #666; font-size: 12px; margin-top: 8px;">
          <div><strong>👤 İşlemi Yapan:</strong> ${m.kullaniciAdi || 'Sistem'}</div>
          ${m.secilenOnaylayiciAdi ? `<div><strong>👔 Atanan:</strong> ${m.secilenOnaylayiciAdi}</div>` : ''}
          ${m.aciklama ? `<div style="margin-top: 4px;"><strong>📋 Açıklama:</strong> ${m.aciklama}</div>` : ''}
          <div style="margin-top: 4px;"><strong>🕐 Tarih:</strong> ${new Date(m.islemTarihi).toLocaleString('tr-TR')}</div>
        </div>
      </div>
    `;
  }).join('');

  Swal.fire({
    title: '📜 Duyuru Geçmişi',
    html: `
      <div style="max-height: 500px; overflow-y: auto; text-align: left; padding: 8px;">
        ${movements.length > 0 ? timelineHtml : '<p style="text-align: center; color: #999;">Henüz işlem geçmişi bulunmamaktadır.</p>'}
      </div>
    `,
    width: '700px',
    showCloseButton: true,
    showConfirmButton: false,
    customClass: {
      container: 'history-modal'
    }
  });
}

formatDate(date: string | Date | null | undefined): string {
  if (!date) return '-';
  const d = new Date(date);
  return d.toLocaleDateString('tr-TR', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}

private loadTemplate(templateId: number): void {
  this.templateService.getTemplateById(templateId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
    next: (response) => {
      if (response.success && response.data) {
        const template = response.data;
        this.announcementForm.patchValue({
          konu: template.konuSablonu || '',
          icerik: template.icerikSablonu || ''
        });
        if (this.editor && template.icerikSablonu) {
          this.editor.setContents(template.icerikSablonu);
        }
        this.toastr.success(`"${template.sablonAdi}" şablonu yüklendi`);
      }
    },
    error: (error) => {
      const errorMessage = error?.error?.message || 'Şablon yüklenemedi';
      this.toastr.error(errorMessage);
    }
  });
}

/**
 * SECURITY: Backend'den güvenli session ID generate et
 * Format: {userId}_{guid}
 */
private generateSecureSessionId(): void {
  this.sessionIdReady.set(false); // Reset state
  this.http.get<ApiResponse<string>>(`${environment.apiUrl}/api/files/generate-session`)
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.sessionId = response.data;
          this.sessionIdReady.set(true); // GÜVENLİK: SessionID hazır
        } else {
          // Backend başarısız cevap verdi, fallback kullan
          this.sessionId = 'session-' + Date.now() + '-' + Math.random().toString(36).substring(2, 15);
          this.sessionIdReady.set(true);
        }
      },
      error: (err) => {
        console.error('Session ID generation failed', err);
        // Fallback: Client-side generate (eski yöntem)
        this.sessionId = 'session-' + Date.now() + '-' + Math.random().toString(36).substring(2, 15);
        this.sessionIdReady.set(true); // Fallback ile de hazır
      }
    });
}

// =============================================
// DRAG & DROP FILE UPLOAD (delegated to FileManagerService)
// =============================================

onDragOver(event: DragEvent): void {
  this.fileManager.onDragOver(event);
}

onDragLeave(event: DragEvent): void {
  this.fileManager.onDragLeave(event);
}

onDrop(event: DragEvent): void {
  // GÜVENLİK: SessionID hazır değilse dosya yüklemeyi engelle (race condition önlemi)
  if (!this.sessionIdReady()) {
    event.preventDefault();
    this.toastr.warning('Dosya yükleme hazırlanıyor, lütfen birkaç saniye bekleyin');
    this.fileManager.isDraggingOver.set(false);
    return;
  }
  this.fileManager.onDrop(event, this.announcementId, this.sessionId);
}

}
