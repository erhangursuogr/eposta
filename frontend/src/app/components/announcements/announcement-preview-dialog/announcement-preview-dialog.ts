import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { Router } from '@angular/router';
import { AnnouncementService } from '../../../services/announcement.service';
import { EmailGroupService } from '../../../services/email-group.service';
import { FileService } from '../../../services/file.service';
import { Announcement } from '../../../common/models/announcement.model';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { forkJoin } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import DOMPurify from 'dompurify';

@Component({
  selector: 'app-announcement-preview-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    MatDividerModule
  ],
  templateUrl: './announcement-preview-dialog.html',
  styleUrl: './announcement-preview-dialog.css'
})
export class AnnouncementPreviewDialog implements OnInit {
  private dialogRef = inject(MatDialogRef<AnnouncementPreviewDialog>);
  private announcementService = inject(AnnouncementService);
  private emailGroupService = inject(EmailGroupService);
  private fileService = inject(FileService);
  private router = inject(Router);
  private sanitizer = inject(DomSanitizer);
  private toastr = inject(ToastrService);
  data: { announcementId: number } = inject(MAT_DIALOG_DATA);

  announcement = signal<Announcement | null>(null);
  loading = signal(true);
  recipients = signal<any[]>([]);
  recipientGroups = signal<any[]>([]);
  files = signal<any[]>([]);
  signature = signal<string>('');

  ngOnInit(): void {
    this.loadAnnouncement();
    this.loadFiles();
    this.loadPreview();
  }

  loadAnnouncement(): void {
    this.announcementService.getAnnouncementById(this.data.announcementId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.announcement.set(response.data);

          // Grup ID'leri varsa, grup detaylarını al
          const data = response.data as any;
          if (data.grupIdList && data.grupIdList.length > 0) {
            this.loadGroupDetails(data.grupIdList);
          }
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        const errorMessage = err.status === 404 ? 'Duyuru bulunamadı' :
          err.status === 403 ? 'Bu duyuruyu görüntüleme yetkiniz yok' :
          'Duyuru yüklenirken bir hata oluştu';
        this.toastr.error(errorMessage, 'Hata');
      }
    });
  }

  loadGroupDetails(groupIds: number[]): void {
    // Her grup için detay çek
    const groupRequests = groupIds.map(id =>
      this.emailGroupService.getGroupById(id)
    );

    forkJoin(groupRequests).subscribe({
      next: (responses) => {
        const groups = responses
          .filter(res => res.success && res.data)
          .map(res => res.data);
        this.recipientGroups.set(groups);
      },
      error: () => {
        this.toastr.warning('Grup bilgileri yüklenemedi', 'Uyarı');
      }
    });
  }

  loadFiles(): void {
    this.announcementService.getAnnouncementFiles(this.data.announcementId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.files.set(response.data);
        }
      },
      error: () => {
        this.toastr.warning('Dosyalar yüklenemedi', 'Uyarı');
      }
    });
  }

  loadPreview(): void {
    // Backend'den imza ile birlikte tam HTML preview al
    this.announcementService.getAnnouncementPreview(this.data.announcementId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const previewData = response.data as any;
          this.signature.set(previewData.htmlContent || '');
        }
      },
      error: () => {
        this.toastr.warning('Önizleme yüklenemedi', 'Uyarı');
      }
    });
  }

  getSafeHtml(html: string): SafeHtml {
    // SECURITY: DOMPurify ile XSS koruması (kurum içi kullanıcı olsa da güvenli olmalı)
    // SunEditor'ün tüm özelliklerini korurken tehlikeli script/event handler'ları temizler
    const clean = DOMPurify.sanitize(html, {
      ALLOWED_TAGS: ['h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'p', 'div', 'span', 'br', 'hr',
                     'ul', 'ol', 'li', 'blockquote', 'pre', 'code',
                     'strong', 'em', 'u', 's', 'sub', 'sup', 'mark',
                     'a', 'img', 'table', 'thead', 'tbody', 'tr', 'th', 'td',
                     'figure', 'figcaption', 'video', 'audio', 'iframe'],
      ALLOWED_ATTR: ['href', 'src', 'alt', 'title', 'width', 'height', 'class', 'style',
                     'target', 'rel', 'colspan', 'rowspan', 'align', 'valign'],
      ALLOW_DATA_ATTR: false, // data-* attributes'ları engelle
      ALLOWED_URI_REGEXP: /^(?:(?:(?:f|ht)tps?|mailto|tel|callto|cid|xmpp|data):|[^a-z]|[a-z+.\-]+(?:[^a-z+.\-:]|$))/i
    });
    return this.sanitizer.bypassSecurityTrustHtml(clean);
  }

  editAnnouncement(): void {
    this.dialogRef.close();
    this.router.navigate(['/duyuru-duzenle', this.data.announcementId]);
  }

  close(): void {
    this.dialogRef.close();
  }

  getStatusColor(status: string): string {
    const colorMap: Record<string, string> = {
      'TASLAK': 'warn',
      'ILK_ONAY_BEKLIYOR': 'accent',
      'SON_ONAY_BEKLIYOR': 'accent',
      'ONAYLANDI': 'primary',
      'GONDERILDI': 'primary',
      'REDDEDILDI': 'warn'
    };
    return colorMap[status] || 'primary';
  }

  getStatusText(status: string): string {
    const textMap: Record<string, string> = {
      'TASLAK': 'Taslak',
      'ILK_ONAY_BEKLIYOR': 'İlk Onay Bekliyor',
      'SON_ONAY_BEKLIYOR': 'Son Onay Bekliyor',
      'ONAYLANDI': 'Onaylandı',
      'GONDERILDI': 'Gönderildi',
      'REDDEDILDI': 'Reddedildi',
      'IPTAL_EDILDI': 'İptal Edildi'
    };
    return textMap[status] || status;
  }

  downloadFile(file: any): void {
    this.fileService.downloadFile(file.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = file.dosyaAdi;
        link.click();
        window.URL.revokeObjectURL(url);
        this.toastr.success('Dosya indiriliyor');
      },
      error: () => {
        this.toastr.error('Dosya indirilemedi');
      }
    });
  }
}
