import { Injectable, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { FileService, UploadedFile } from '../../../../services/file.service';
import { environment } from '../../../../../environments/environment';

/**
 * Duyuru Formu Dosya Yükleme & Yönetim Servisi
 *
 * Sorumluluklar:
 * - Sıralı dosya yükleme (HTTP bağlantı limitini aşmamak için)
 * - Sürükle-bırak dosya yükleme
 * - Onaylı dosya silme
 * - Yükleme ilerleme takibi
 * - Dosya boyutu formatlama ve doğrulama
 */
@Injectable({
  providedIn: 'root'
})
export class AnnouncementFileManagerService {
  private fileService = inject(FileService);
  private toastr = inject(ToastrService);
  private destroyRef = inject(DestroyRef);

  // Durum
  uploadedFiles = signal<UploadedFile[]>([]);
  uploadProgress = signal<number>(0);
  isDraggingOver = signal<boolean>(false);

  // Yapılandırma
  private readonly MAX_FILE_SIZE = 50 * 1024 * 1024; // 50MB

  /**
   * Dosya seçimini işle (input'tan)
   */
  onFileSelected(event: any, announcementId?: number, sessionId?: string): void {
    const files: FileList = event.target.files;

    if (files.length === 0) {
      return;
    }

    // HTTP bağlantı limitini aşmamak için dosyaları sırayla yükle
    this.uploadFilesSequentially(Array.from(files), 0, announcementId, sessionId);

    // Input'u sıfırla
    event.target.value = '';
  }

  /**
   * Dosyaları tek tek sırayla yükle
   */
  private uploadFilesSequentially(
    files: File[],
    index: number,
    announcementId?: number,
    sessionId?: string
  ): void {
    if (index >= files.length) {
      return;
    }

    const file = files[index];
    this.uploadFile(file, announcementId, sessionId, () => {
      // Mevcut dosya tamamlandıktan sonra bir sonrakini yükle
      this.uploadFilesSequentially(files, index + 1, announcementId, sessionId);
    });
  }

  /**
   * İlerleme takibi ile tek dosya yükle
   */
  private uploadFile(
    file: File,
    announcementId?: number,
    sessionId?: string,
    onComplete?: () => void
  ): void {
    // Dosya boyutunu doğrula
    if (file.size > this.MAX_FILE_SIZE) {
      this.toastr.error(`${file.name} çok büyük (max 50MB)`);
      if (onComplete) onComplete();
      return;
    }

    this.uploadProgress.set(0);

    // Duyuru henüz kaydedilmemişse sessionId ile, yoksa announcementId ile yükle
    this.fileService
      .uploadFile(file, announcementId, sessionId || '')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            // FileUploadResult'ı UploadedFile formatına çevir
            const uploadedFile: UploadedFile = {
              id: response.data.fileId,
              dosyaAdi: response.data.fileName,
              dosyaYolu: '',
              dosyaBoyutu: response.data.fileSize,
              mimeType: response.data.fileType,
              yuklemeTarihi: response.data.uploadDate,
              yukleyenKullaniciId: 0,
              yukleyenKullaniciAdi: ''
            };

            this.uploadedFiles.set([...this.uploadedFiles(), uploadedFile]);
            this.toastr.success(`${file.name} yüklendi`);
            this.uploadProgress.set(100);

            setTimeout(() => {
              this.uploadProgress.set(0);
              if (onComplete) onComplete();
            }, 500);
          }
        },
        error: () => {
          this.toastr.error(`${file.name} yüklenemedi`);
          this.uploadProgress.set(0);
          if (onComplete) onComplete();
        }
      });
  }

  /**
   * Onaylı dosya silme
   */
  removeFile(fileId: number): void {
    Swal.fire({
      title: 'Dosya Sil',
      text: 'Bu dosyayı silmek istediğinizden emin misiniz?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Evet, Sil',
      cancelButtonText: 'İptal',
      confirmButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed) {
        this.fileService
          .deleteFile(fileId)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (response) => {
              if (response.success) {
                this.uploadedFiles.set(
                  this.uploadedFiles().filter((f) => f.id !== fileId)
                );
                this.toastr.success('Dosya silindi');
              }
            },
            error: () => {
              this.toastr.error('Dosya silinemedi');
            }
          });
      }
    });
  }

  /**
   * Dosya boyutunu gösterim için formatla (bytes → KB/MB/GB)
   */
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  /**
   * Dosya indirme URL'ini al
   */
  getFileUrl(fileId: number): string {
    return `${environment.apiUrl}/api/files/${fileId}/download`;
  }

  /**
   * Sürükle-Bırak: Sürükleme olayını işle
   */
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDraggingOver.set(true);
  }

  /**
   * Sürükle-Bırak: Sürükleme ayrılma olayını işle
   */
  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDraggingOver.set(false);
  }

  /**
   * Sürükle-Bırak: Dosya bırakma olayını işle
   */
  onDrop(event: DragEvent, announcementId?: number, sessionId?: string): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDraggingOver.set(false);

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFiles(files, announcementId, sessionId);
    }
  }

  /**
   * Bırakılan dosyaları işle
   */
  private handleFiles(
    files: FileList,
    announcementId?: number,
    sessionId?: string
  ): void {
    const filesArray = Array.from(files);
    this.uploadFilesSequentially(filesArray, 0, announcementId, sessionId);
  }

  /**
   * Yüklenmiş dosyaları ayarla (mevcut duyuru yüklenirken)
   */
  setUploadedFiles(files: UploadedFile[]): void {
    this.uploadedFiles.set(files);
  }

  /**
   * Tüm yüklenmiş dosyaları temizle
   */
  clearUploadedFiles(): void {
    this.uploadedFiles.set([]);
    this.uploadProgress.set(0);
  }
}
