import { Component, OnInit, signal, computed } from '@angular/core';

import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { environment } from '../../../../environments/environment';
import Swal from 'sweetalert2';

interface Schedule {
  id: number;
  duyuruId: number;
  konu: string;
  zamanlanmaTarihi: string;
  durum: string;
  gonderimTarihi?: string;
  aliciSayisi?: number;
  hangfireJobId?: string;
  hataMesaji?: string;
  iptalNotu?: string;
  olusturmaTarihi: string;
}

@Component({
  selector: 'app-scheduled-list',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatTooltipModule
],
  templateUrl: './scheduled-list.html',
  styleUrl: './scheduled-list.css'
})
export class ScheduledListComponent implements OnInit {
  schedules = signal<Schedule[]>([]);
  loading = signal<boolean>(false);
  selectedStatus = signal<string | null>(null);

  displayedColumns: string[] = ['konu', 'zamanlanmaTarihi', 'durum', 'aliciSayisi', 'actions'];

  // Filtered schedules based on selected status
  filteredSchedules = computed(() => {
    const status = this.selectedStatus();
    if (!status) return this.schedules();
    return this.schedules().filter(s => s.durum === status);
  });

  // Group schedules by announcement
  groupedSchedules = computed(() => {
    const schedules = this.filteredSchedules();
    const groups = new Map<number, Schedule[]>();

    schedules.forEach(schedule => {
      const existing = groups.get(schedule.duyuruId) || [];
      existing.push(schedule);
      groups.set(schedule.duyuruId, existing);
    });

    return Array.from(groups.values());
  });

  // Stats computed properties
  beklemedeSayisi = computed(() =>
    this.filteredSchedules().filter(s => s.durum === 'BEKLEMEDE').length
  );

  gonderildiSayisi = computed(() =>
    this.filteredSchedules().filter(s => s.durum === 'GONDERILDI').length
  );

  iptalSayisi = computed(() =>
    this.filteredSchedules().filter(s => s.durum === 'IPTAL').length
  );

  hataSayisi = computed(() =>
    this.filteredSchedules().filter(s => s.durum === 'HATA').length
  );

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadSchedules();
  }

  loadSchedules(): void {
    this.loading.set(true);
    const url = this.selectedStatus()
      ? `${environment.apiUrl}/api/schedules?durum=${this.selectedStatus()}`
      : `${environment.apiUrl}/api/schedules`;

    this.http.get<any>(url).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: (response) => {
        this.schedules.set(response.data || []);
      },
      error: (error) => {
        console.error('Error loading schedules:', error);
      }
    });
  }

  filterByStatus(status: string | null): void {
    this.selectedStatus.set(status);
    this.loadSchedules();
  }

  cancelSchedule(schedule: Schedule): void {
    Swal.fire({
      title: 'Zamanlamayı İptal Et',
      html: `<strong>${schedule.konu}</strong> duyurusunun<br>${new Date(schedule.zamanlanmaTarihi).toLocaleString('tr-TR')} tarihli zamanlamasını iptal etmek istediğinize emin misiniz?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#6c757d',
      confirmButtonText: 'Evet, İptal Et',
      cancelButtonText: 'Vazgeç'
    }).then((result) => {
      if (result.isConfirmed) {
        const request = { iptalNotu: 'Kullanıcı tarafından iptal edildi' };

        this.http.put<any>(`${environment.apiUrl}/api/schedules/${schedule.id}/cancel`, request).subscribe({
          next: (response) => {
            Swal.fire({
              title: 'İptal Edildi!',
              text: 'Zamanlama başarıyla iptal edildi',
              icon: 'success',
              confirmButtonColor: '#28a745'
            });
            this.loadSchedules();
          },
          error: (error) => {
            console.error('Error cancelling schedule:', error);
            Swal.fire({
              title: 'Hata!',
              text: 'Zamanlama iptal edilirken hata oluştu',
              icon: 'error',
              confirmButtonColor: '#d33'
            });
          }
        });
      }
    });
  }

  getStatusColor(durum: string): string {
    const statusMap: { [key: string]: string } = {
      'BEKLEMEDE': 'warning',
      'GONDERILDI': 'success',
      'IPTAL': 'default',
      'HATA': 'error'
    };
    return statusMap[durum] || 'default';
  }

  getStatusIcon(durum: string): string {
    const iconMap: { [key: string]: string } = {
      'BEKLEMEDE': 'schedule',
      'GONDERILDI': 'check_circle',
      'IPTAL': 'cancel',
      'HATA': 'error'
    };
    return iconMap[durum] || 'help';
  }

  getStatusLabel(durum: string): string {
    const labelMap: { [key: string]: string } = {
      'BEKLEMEDE': 'Beklemede',
      'GONDERILDI': 'Gönderildi',
      'IPTAL': 'İptal',
      'HATA': 'Hata'
    };
    return labelMap[durum] || durum;
  }

  formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString('tr-TR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
