import { Injectable, inject, signal, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { AnnouncementService } from '../../../../services/announcement.service';
import { FileService } from '../../../../services/file.service';
import { environment } from '../../../../../environments/environment';

/**
 * Duyuru İş Akışı Servisi
 *
 * Sorumluluklar:
 * - İki seviyeli onay iş akışı (EDİTÖR → KOORDİNATÖR → MÜDÜR)
 * - Taslak kaydetme, onaya gönderme
 * - Onaylama, gönderme, onayla+gönder işlemleri
 * - Önizleme, test e-postası
 * - Geçmiş takibi
 */
@Injectable({
  providedIn: 'root'
})
export class AnnouncementWorkflowService {
  private router = inject(Router);
  private http = inject(HttpClient);
  private announcementService = inject(AnnouncementService);
  private fileService = inject(FileService);
  private toastr = inject(ToastrService);
  private destroyRef = inject(DestroyRef);

  // Durum
  loading = signal<boolean>(false);

  /**
   * Duyuruyu taslak olarak kaydet (TASLAK)
   */
  saveDraft(
    announcementId: number | undefined,
    isEditMode: boolean,
    formData: any,
    editorContent: string,
    sessionId: string
  ): void {
    this.submitAnnouncement(
      announcementId,
      isEditMode,
      formData,
      editorContent,
      sessionId,
      false // submitForApproval = false
    );
  }

  /**
   * Duyuruyu onaya gönder (ILK_ONAY_BEKLIYOR)
   */
  submitForApproval(
    announcementId: number | undefined,
    isEditMode: boolean,
    formData: any,
    editorContent: string,
    sessionId: string
  ): void {
    // Önce kaydet (yeni ise), sonra onaya gönder
    if (!announcementId) {
      Swal.fire({
        title: 'Onaya Gönder',
        text: 'Duyuru önce kaydedilecek, sonra onaya gönderilecektir. Devam etmek istiyor musunuz?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Evet, Kaydet ve Gönder',
        cancelButtonText: 'İptal',
        confirmButtonColor: '#004B87'
      }).then((result) => {
        if (result.isConfirmed) {
          this.submitAnnouncement(
            announcementId,
            isEditMode,
            formData,
            editorContent,
            sessionId,
            true // submitForApproval = true
          );
        }
      });
    } else {
      // Zaten kaydedilmiş, direkt onaya gönder
      Swal.fire({
        title: 'Onaya Gönder',
        text: 'Duyuruyu onaya göndermek istediğinizden emin misiniz?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Evet, Gönder',
        cancelButtonText: 'İptal',
        confirmButtonColor: '#004B87'
      }).then((result) => {
        if (result.isConfirmed) {
          this.submitAnnouncement(
            announcementId,
            isEditMode,
            formData,
            editorContent,
            sessionId,
            true // submitForApproval = true
          );
        }
      });
    }
  }

  /**
   * Dahili: Duyuruyu kaydet (oluştur/güncelle + opsiyonel olarak onaya gönder)
   */
  private submitAnnouncement(
    announcementId: number | undefined,
    isEditMode: boolean,
    formData: any,
    editorContent: string,
    sessionId: string,
    submitForApproval: boolean
  ): void {
    this.loading.set(true);

    const request = {
      ...formData,
      icerik: editorContent
    };

    const saveObservable = isEditMode
      ? this.announcementService.updateAnnouncement({ id: announcementId!, ...request })
      : this.announcementService.createAnnouncement(request);

    saveObservable
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response) => {
          if (response.success) {
            const duyuruId: number = isEditMode
              ? announcementId!
              : (response.data as unknown as number);

            // Link session files if this is a new announcement
            if (!isEditMode && duyuruId) {
              this.linkSessionFilesToAnnouncement(duyuruId, sessionId);
            }

            this.toastr.success(
              isEditMode ? 'Duyuru güncellendi' : 'Duyuru taslak olarak kaydedildi'
            );

            if (submitForApproval) {
              // Submit for approval (use existing ID in edit mode, or response.data.id in create mode)
              if (!duyuruId) {
                this.toastr.error('Duyuru ID bulunamadı');
                this.router.navigate(['/duyurular']);
                return;
              }

              this.announcementService
                .submitForApproval(duyuruId)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                  next: (response) => {
                    if (response.success) {
                      this.toastr.success(
                        response.message || 'Duyuru onaya gönderildi'
                      );
                      this.router.navigate(['/duyurular']);
                    } else {
                      this.toastr.error(
                        response.message || 'Duyuru onaya gönderilemedi'
                      );
                    }
                  },
                  error: (error) => {
                    console.error('Submit for approval error:', error);
                    this.toastr.error(
                      error.error?.message || 'Onaya gönderilirken hata oluştu'
                    );
                  }
                });
            } else if (!isEditMode && duyuruId) {
              // Yeni duyuru taslak olarak kaydedildi - edit moduna geç (sayfada kal)
              this.router.navigate(['/duyuru-duzenle', duyuruId,], { replaceUrl: true });
            }
            // Edit modda güncelleme sonrası aynı sayfada kal
          }
        },
        error: (error) => {
          console.error('Save error:', error);
          this.toastr.error(
            error.error?.message ||
              (isEditMode ? 'Duyuru güncellenemedi' : 'Duyuru oluşturulamadı')
          );
        }
      });
  }

  /**
   * Link session files to announcement
   */
  private linkSessionFilesToAnnouncement(
    announcementId: number,
    sessionId: string
  ): void {
    this.fileService
      .linkSessionFiles(sessionId, announcementId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (response.success) {
            // Files linked successfully
          }
        },
        error: (error) => {
          console.error('Failed to link session files:', error);
        }
      });
  }

  /**
   * Approve announcement (COORDINATOR or MANAGER)
   */
  approve(
    announcementId: number,
    approvalNote: string,
    konu: string
  ): void {
    const approvalNoteText = approvalNote.trim();

    Swal.fire({
      title: 'Duyuruyu Onayla',
      html: `
        <div class="text-start p-3">
          <p><strong>Konu:</strong> ${konu}</p>
          ${approvalNoteText ? `<p><strong>Onay Notu:</strong> ${approvalNoteText}</p>` : ''}
          <hr/>
          <p class="text-info">
            <i class="material-icons" style="vertical-align: middle;">info</i>
            Duyuru onaylanacak ancak henüz gönderilmeyecektir.
          </p>
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Onayla',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#4CAF50'
    }).then((result) => {
      if (result.isConfirmed) {
        this.performApprove(announcementId, approvalNote);
      }
    });
  }

  /**
   * Perform approve action
   */
  private performApprove(announcementId: number, approvalNote: string): void {
    this.loading.set(true);

    this.announcementService
      .approveAnnouncement(announcementId, approvalNote)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.toastr.success('Duyuru başarıyla onaylandı!');
            this.router.navigate(['/duyurular']);
          } else {
            this.toastr.error(response.message || 'Duyuru onaylanamadı');
          }
        },
        error: (error) => {
          console.error('Approval error:', error);
          this.toastr.error(
            error.error?.message || 'Duyuru onaylanırken hata oluştu'
          );
        }
      });
  }

  /**
   * Approve and send announcement (ADMIN/MANAGER shortcut)
   */
  approveAndSend(
    announcementId: number,
    approvalConfirmed: boolean,
    approvalNote: string,
    konu: string,
    totalRecipients: number
  ): void {
    if (!approvalConfirmed) {
      this.toastr.error('Lütfen onay kutucuğunu işaretleyin');
      return;
    }

    const approvalNoteText = approvalNote.trim();

    Swal.fire({
      title: 'Onayla ve Gönder',
      html: `
        <div class="text-start p-3">
          <p><strong>Konu:</strong> ${konu}</p>
          <p><strong>Toplam Alıcı:</strong> ~${totalRecipients} kişi</p>
          ${approvalNoteText ? `<p><strong>Onay Notu:</strong> ${approvalNoteText}</p>` : ''}
          <hr/>
          <p class="text-warning fw-bold">
            <i class="material-icons" style="vertical-align: middle;">warning</i>
            Bu işlem duyuruyu onaylayacak VE hemen gönderecektir!
          </p>
        </div>
      `,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Onayla ve Gönder',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#f44336'
    }).then((result) => {
      if (result.isConfirmed) {
        this.performApproveAndSend(announcementId, approvalNote);
      }
    });
  }

  /**
   * Perform approve and send action
   */
  private performApproveAndSend(
    announcementId: number,
    approvalNote: string
  ): void {
    this.loading.set(true);

    this.http
      .post<any>(
        `${environment.apiUrl}/api/announcements/${announcementId}/approve-and-send`,
        { onayNotu: approvalNote }
      )
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.toastr.success('Duyuru onaylandı ve gönderildi!');
            this.router.navigate(['/duyurular']);
          } else {
            this.toastr.error(
              response.message || 'Duyuru onaylanamadı veya gönderilemedi'
            );
          }
        },
        error: (error) => {
          console.error('Approve and send error:', error);
          this.toastr.error(
            error.error?.message ||
              'Duyuru onaylanırken veya gönderilirken hata oluştu'
          );
        }
      });
  }

  /**
   * Send approved announcement (EDITOR/COORDINATOR/MANAGER)
   */
  sendApprovedAnnouncement(
    announcementId: number,
    konu: string,
    totalRecipients: number
  ): void {
    Swal.fire({
      title: 'Duyuruyu Gönder',
      html: `
        <div class="text-start p-3">
          <p><strong>Konu:</strong> ${konu}</p>
          <p><strong>Toplam Alıcı:</strong> ~${totalRecipients} kişi</p>
          <hr/>
          <p class="text-warning fw-bold">
            <i class="material-icons" style="vertical-align: middle;">info</i>
            Bu duyuru onaylanmış durumda. Gönderilecektir.
          </p>
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Evet, Gönder',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#f44336'
    }).then((result) => {
      if (result.isConfirmed) {
        this.performSendApprovedAnnouncement(announcementId);
      }
    });
  }

  /**
   * Perform send approved announcement
   */
  private performSendApprovedAnnouncement(announcementId: number): void {
    this.loading.set(true);

    this.http
      .post<any>(
        `${environment.apiUrl}/api/announcements/${announcementId}/send`,
        {}
      )
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.toastr.success('Duyuru başarıyla gönderildi!');
            this.router.navigate(['/duyurular']);
          } else {
            this.toastr.error(response.message || 'Duyuru gönderilemedi');
          }
        },
        error: (error) => {
          console.error('Send error:', error);
          this.toastr.error(
            error.error?.message || 'Duyuru gönderilirken hata oluştu'
          );
        }
      });
  }

  /**
   * Send test email
   */
  sendTestEmail(
    announcementId: number | undefined,
    formData: any,
    editorContent: string,
    testEmail: string
  ): void {
    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(testEmail)) {
      this.toastr.error('Geçersiz e-posta adresi');
      return;
    }

    this.loading.set(true);

    this.loading.set(true);

    // KURAL: Test email sadece kayıtlı duyurular için atılabilir via {id}/send-test endpoint
    if (!announcementId) {
      this.toastr.warning('Test emaili göndermek için önce taslağı kaydetmelisiniz.');
      this.loading.set(false);
      return;
    }

    this.announcementService
      .sendTestEmail(announcementId, testEmail)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.toastr.success(`Test e-posta ${testEmail} adresine gönderildi`);
          } else {
            this.toastr.error(response.message || 'Test e-posta gönderilemedi');
          }
        },
        error: (error) => {
          console.error('Test email error:', error);
          this.toastr.error(
            error.error?.message || 'Test e-posta gönderilirken hata oluştu'
          );
        }
      });
  }

  /**
   * Get total recipient count (approximate)
   */
  getTotalRecipientCount(
    emailGroups: any[],
    selectedGroups: number[],
    manualEmails: string[]
  ): number {
    let total = 0;

    // Add group member counts
    selectedGroups.forEach((groupId) => {
      const group = emailGroups.find((g) => g.id === groupId);
      if (group) {
        total += group.uyeSayisi || 0;
      }
    });

    // Add manual email count
    total += manualEmails.length;

    return total;
  }
}
