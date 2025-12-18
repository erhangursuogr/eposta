import { Injectable, inject, signal, DestroyRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastrService } from 'ngx-toastr';
import { EmailGroupService, EmailGroup } from '../../../../services/email-group.service';
import { AnnouncementFileManagerService } from './announcement-file-manager.service';

/**
 * Duyuru Formu Durum Yönetim Servisi
 *
 * Sorumluluklar:
 * - Form başlatma ve doğrulama
 * - E-posta grupları ve manuel e-posta yönetimi
 * - Session ID yönetimi
 * - Dropdown veri yükleme (onaylayanlar, kategoriler)
 */
@Injectable({
  providedIn: 'root'
})
export class AnnouncementFormStateService {
  private fb = inject(FormBuilder);
  private emailGroupService = inject(EmailGroupService);
  private toastr = inject(ToastrService);
  private destroyRef = inject(DestroyRef);
  private fileManager = inject(AnnouncementFileManagerService);

  // Form
  announcementForm!: FormGroup;

  // Durum Signalleri
  loading = signal<boolean>(false);
  emailGroups = signal<EmailGroup[]>([]);
  selectedGroups = signal<number[]>([]);
  manualEmails = signal<string[]>([]);
  approvers = signal<any[]>([]);
  categories = signal<{ key: string; displayName: string; hasSignature: boolean }[]>([]);
  senderCategories = signal<{ key: string; displayName: string; email: string }[]>([]);


  /**
   * Doğrulama kuralları ile formu başlat
   */
  initForm(): void {
    this.announcementForm = this.fb.group({
      konu: ['', Validators.required],
      icerik: ['', Validators.required],
      duyuruKategorisi: ['GENEL_DUYURU_IMZASIZ', Validators.required],
      gondericiKategori: ['EMAIL_DUYURU', Validators.required],
      aciklama: [''],
      onaylayanKullaniciId: [null],
      gruplar: [[]],
      bannerDosyaId: [null],
      zamanlanmisTarih: [null]
    });
  }

  /**
   * Aktif e-posta gruplarını yükle
   */
  loadEmailGroups(): void {
    this.emailGroupService
      .getActiveGroups()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.emailGroups.set(response.data);
          }
        },
        error: () => {
          this.toastr.error('E-posta grupları yüklenemedi');
        }
      });
  }

  /**
   * Grup seçimini aç/kapat
   */
  toggleGroup(groupId: number): void {
    const selected = [...this.selectedGroups()];
    const index = selected.indexOf(groupId);

    if (index >= 0) {
      selected.splice(index, 1);
    } else {
      selected.push(groupId);
    }

    this.selectedGroups.set(selected);
  }

  /**
   * Check if group is selected
   */
  isGroupSelected(groupId: number): boolean {
    return this.selectedGroups().includes(groupId);
  }

  /**
   * Add manual email address
   */
  addEmail(email: string): boolean {
    const trimmedEmail = email.trim().toLowerCase();

    if (!trimmedEmail) {
      return false;
    }

    // Email validation
    const emailRegex = /^[a-zA-Z0-9._%+-]+@deu\.edu\.tr$/;
    if (!emailRegex.test(trimmedEmail)) {
      this.toastr.error('Sadece @deu.edu.tr uzantılı e-postalar eklenebilir');
      return false;
    }

    // Check duplicate
    if (this.manualEmails().includes(trimmedEmail)) {
      this.toastr.warning('Bu e-posta zaten ekli');
      return false;
    }

    this.manualEmails.set([...this.manualEmails(), trimmedEmail]);
    this.toastr.success('E-posta eklendi');
    return true;
  }

  /**
   * Remove manual email address
   */
  removeEmail(email: string): void {
    this.manualEmails.set(this.manualEmails().filter((e) => e !== email));
  }

  /**
   * Reset form and state
   */
  resetForm(): void {
    if (this.announcementForm) {
      this.announcementForm.reset({
        duyuruKategorisi: 'GENEL_DUYURU_IMZASIZ',
        gondericiKategori: 'EMAIL_DUYURU'
      });
    }
    this.selectedGroups.set([]);
    this.manualEmails.set([]);
    this.fileManager.clearUploadedFiles(); // Dosyaları temizle
  }

  /**
   * Set approvers list
   */
  setApprovers(approvers: any[]): void {
    this.approvers.set(approvers);
  }

  /**
   * Set categories (signature categories)
   */
  setCategories(categories: { key: string; displayName: string; hasSignature: boolean }[]): void {
    this.categories.set(categories);
  }

  /**
   * Set sender categories (SMTP sender categories)
   */
  setSenderCategories(categories: { key: string; displayName: string; email: string }[]): void {
    this.senderCategories.set(categories);
  }

  /**
   * Get form data for submission
   */
  getFormData(): any {
    return {
      konu: this.announcementForm.value.konu,
      icerik: '', // Will be set by component (editor content)
      duyuruKategorisi: this.announcementForm.value.duyuruKategorisi,
      gondericiKategori: this.announcementForm.value.gondericiKategori,
      aciklama: this.announcementForm.value.aciklama || '',
      onaylayanKullaniciId: this.announcementForm.value.onaylayanKullaniciId,
      grupIdList: this.selectedGroups(),
      aliciEmailList: this.manualEmails(),
      bannerDosyaId: this.announcementForm.value.bannerDosyaId,
      zamanlanmisTarih: this.announcementForm.value.zamanlanmisTarih
    };
  }
}
