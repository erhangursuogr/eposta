import { Component, OnInit, inject, signal, computed } from '@angular/core';

import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { ToastrService } from 'ngx-toastr';

import { AnnouncementService } from '../../../services/announcement.service';
import { UserDataService } from '../../../services/userdata.service';
import { Announcement, AnnouncementStatus } from '../../../common/models/announcement.model';
import { AnnouncementPreviewDialog } from '../../announcements/announcement-preview-dialog/announcement-preview-dialog';

@Component({
  selector: 'app-rejected-announcements',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  templateUrl: './rejected-announcements.html',
  styleUrl: './rejected-announcements.css'
})
export class RejectedAnnouncements implements OnInit {
  private announcementService = inject(AnnouncementService);
  private userDataService = inject(UserDataService);
  private router = inject(Router);
  private toastr = inject(ToastrService);
  private dialog = inject(MatDialog);

  loading = signal<boolean>(false);
  announcements = signal<Announcement[]>([]);
  currentUser = computed(() => this.userDataService.user());

  ngOnInit(): void {
    this.loadRejectedAnnouncements();
  }

  loadRejectedAnnouncements(): void {
    this.loading.set(true);
    this.announcementService.getRejectedAnnouncements().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          // Backend'den zaten filtrelenmiş veri geliyor (Hareket tablosundan)
          this.announcements.set(response.data);
        }
        this.loading.set(false);
      },
      error: () => {
        this.toastr.error('Reddedilen duyurular yüklenemedi');
        this.loading.set(false);
      }
    });
  }

  viewDetails(announcement: Announcement): void {
    this.dialog.open(AnnouncementPreviewDialog, {
      width: '900px',
      maxHeight: '90vh',
      maxWidth: '95vw',
      panelClass: 'preview-dialog-container',
      data: { announcementId: announcement.id }
    });
  }

  formatDate(date: string | Date | null | undefined): string {
    if (!date) return '-';
    const d = new Date(date);
    return d.toLocaleDateString('tr-TR', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  getDurumLabel(durum: string): string {
    switch (durum) {
      case 'TASLAK': return 'Taslak';
      case 'ILK_ONAY_BEKLIYOR': return 'İlk Onay Bekliyor';
      case 'SON_ONAY_BEKLIYOR': return 'Son Onay Bekliyor';
      default: return durum;
    }
  }
}
