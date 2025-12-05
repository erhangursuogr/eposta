import { Component, inject, OnInit, signal, computed } from '@angular/core';

import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTableModule } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { AnnouncementService } from '../../../services/announcement.service';
import { AnnouncementPreviewDialog } from '../announcement-preview-dialog/announcement-preview-dialog';
import { Announcement } from '../../../common/models/announcement.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-announcement-sent-history',
  standalone: true,
  imports: [
    RouterModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCardModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatTableModule
],
  templateUrl: './announcement-sent-history.html',
  styleUrl: './announcement-sent-history.css'
})
export class AnnouncementSentHistoryComponent implements OnInit {
  private readonly announcementService = inject(AnnouncementService);
  private readonly dialog = inject(MatDialog);

  announcements = signal<Announcement[]>([]);
  loading = signal(false);
  searchTerm = signal('');
  startDate = signal<Date | null>(null);
  endDate = signal<Date | null>(null);

  displayedColumns: string[] = ['konu', 'olusturan', 'ilkOnaylayan', 'sonOnaylayan', 'olusturmaTarihi', 'gonderimTarihi', 'actions'];

  // Computed - filtrelenmiş duyurular
  filteredAnnouncements = computed(() => {
    let result = this.announcements();

    // Sadece gönderilmiş duyurular
    result = result.filter(a => a.durum === 'GONDERILDI');

    // Arama
    const search = this.searchTerm().toLowerCase();
    if (search) {
      result = result.filter(a =>
        a.konu?.toLowerCase().includes(search) ||
        a.olusturanKullaniciAdi?.toLowerCase().includes(search)
      );
    }

    // Tarih filtresi
    if (this.startDate()) {
      const start = new Date(this.startDate()!);
      start.setHours(0, 0, 0, 0);
      result = result.filter(a => {
        const sentDate = new Date(a.gonderimTarihi || a.olusturmaTarihi);
        return sentDate >= start;
      });
    }

    if (this.endDate()) {
      const end = new Date(this.endDate()!);
      end.setHours(23, 59, 59, 999);
      result = result.filter(a => {
        const sentDate = new Date(a.gonderimTarihi || a.olusturmaTarihi);
        return sentDate <= end;
      });
    }

    // Sıralama: En son gönderilenden en eskiye
    return result.sort((a, b) => {
      const dateA = new Date(a.gonderimTarihi || a.olusturmaTarihi).getTime();
      const dateB = new Date(b.gonderimTarihi || b.olusturmaTarihi).getTime();
      return dateB - dateA;
    });
  });

  ngOnInit(): void {
    this.loadAnnouncements();
  }

  loadAnnouncements(): void {
    this.loading.set(true);
    this.announcementService.getAnnouncements().pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.announcements.set(response.data);
        }
      },
      error: () => {
      }
    });
  }

  viewDetails(announcement: Announcement): void {
    this.dialog.open(AnnouncementPreviewDialog, {
      data: { announcementId: announcement.id },
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      panelClass: 'preview-dialog-container'
    });
  }

  private showDetailsModal(announcement: Announcement, movements: any[]): void {
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

    const timeline = movements.map(m => {
      const badge = getStatusBadge(m.yeniDurum);
      return `
        <div style="display: flex; align-items: start; gap: 12px; padding: 12px; border-left: 3px solid ${badge.color}; margin-bottom: 12px; background: #f9f9f9; border-radius: 4px;">
          <div style="font-size: 24px;">${badge.icon}</div>
          <div style="flex: 1;">
            <div style="font-weight: 600; color: #333;">${badge.text}</div>
            <div style="color: #666; font-size: 14px; margin-top: 4px;">
              <strong>${m.kullaniciAdi || 'Sistem'}</strong>
              ${m.islemTipi === 'OLUSTURMA' ? 'oluşturdu' :
                m.islemTipi === 'ONAYLAMA' ? 'onayladı' :
                m.islemTipi === 'REDDETME' ? 'reddetti' :
                m.islemTipi === 'GONDERIM' ? 'gönderdi' : 'işlem yaptı'}
            </div>
            <div style="color: #999; font-size: 13px; margin-top: 4px;">
              📅 ${new Date(m.islemTarihi).toLocaleString('tr-TR')}
            </div>
            ${m.aciklama ? `<div style="color: #555; font-size: 13px; margin-top: 8px; font-style: italic;">"${m.aciklama}"</div>` : ''}
          </div>
        </div>
      `;
    }).join('');

    Swal.fire({
      title: `📧 ${announcement.konu}`,
      html: `
        <div style="text-align: left;">
          <h4 style="margin: 20px 0 12px 0; color: #333;">📋 Onay Süreci</h4>
          <div style="max-height: 400px; overflow-y: auto;">
            ${timeline}
          </div>
        </div>
      `,
      width: '600px',
      showCloseButton: true,
      showConfirmButton: false,
      customClass: {
        popup: 'history-modal'
      }
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleString('tr-TR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.startDate.set(null);
    this.endDate.set(null);
  }

  truncateText(text: string | null | undefined, maxLength: number): string {
    if (!text) return '';
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
  }
}
