import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatRadioModule } from '@angular/material/radio';
import { HttpClient } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-scheduled-announcements',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatRadioModule
  ],
  templateUrl: './scheduled-announcements.html',
  styleUrl: './scheduled-announcements.css'
})
export class ScheduledAnnouncementsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private toastr = inject(ToastrService);

  scheduleForm!: FormGroup;
  loading = signal<boolean>(false);
  showForm = signal<boolean>(false);
  duyuruId = signal<number | null>(null);
  announcementTitle = signal<string>('');
  schedules = signal<any[]>([]);
  minDate = new Date(); // Template için minimum tarih

  displayedColumns = ['zamanlananTarih', 'durum', 'aliciSayisi', 'actions'];

  ngOnInit(): void {
    this.initForm();

    // Check for duyuruId query parameter
    this.route.queryParams.subscribe(params => {
      if (params['duyuruId']) {
        const id = parseInt(params['duyuruId']);
        this.duyuruId.set(id);
        this.showForm.set(true);
        this.loadAnnouncementDetails(id);
      }
    });

    this.loadSchedules();
  }

  initForm(): void {
    this.scheduleForm = this.fb.group({
      zamanlamaTipi: ['TEK', Validators.required],
      // Tek seferlik
      zamanlananTarih: [''],
      zamanlananSaat: ['09:00'],
      // Tekrarlı
      baslangicTarihi: [''],
      baslangicSaati: ['09:00'],
      bitisTarihi: [''],
      tekrarGunAraligi: [7, [Validators.min(1), Validators.max(365)]]
    });

    // Zamanlama tipine göre validator'ları ayarla
    this.scheduleForm.get('zamanlamaTipi')?.valueChanges.subscribe(tip => {
      if (tip === 'TEK') {
        this.scheduleForm.get('zamanlananTarih')?.setValidators([Validators.required]);
        this.scheduleForm.get('zamanlananSaat')?.setValidators([Validators.required]);
        this.scheduleForm.get('baslangicTarihi')?.clearValidators();
        this.scheduleForm.get('baslangicSaati')?.clearValidators();
        this.scheduleForm.get('bitisTarihi')?.clearValidators();
        this.scheduleForm.get('tekrarGunAraligi')?.clearValidators();
      } else {
        this.scheduleForm.get('zamanlananTarih')?.clearValidators();
        this.scheduleForm.get('zamanlananSaat')?.clearValidators();
        this.scheduleForm.get('baslangicTarihi')?.setValidators([Validators.required]);
        this.scheduleForm.get('baslangicSaati')?.setValidators([Validators.required]);
        this.scheduleForm.get('bitisTarihi')?.setValidators([Validators.required]);
        this.scheduleForm.get('tekrarGunAraligi')?.setValidators([Validators.required, Validators.min(1), Validators.max(365)]);
      }
      this.scheduleForm.updateValueAndValidity();
    });
  }

  loadAnnouncementDetails(id: number): void {
    this.loading.set(true);
    this.http.get<any>(`${environment.apiUrl}/api/announcements/${id}`).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.announcementTitle.set(response.data.konu);
          }
        },
        error: () => {
          this.toastr.error('Duyuru bilgileri yüklenemedi');
        }
      });
  }

  loadSchedules(): void {
    const duyuruId = this.duyuruId();
    if (!duyuruId) {
      // Tüm zamanlamaları getir (şimdilik boş liste)
      this.schedules.set([]);
      return;
    }

    this.loading.set(true);
    this.http.get<any>(`${environment.apiUrl}/api/schedules/announcement/${duyuruId}`).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.schedules.set(response.data);
          }
        },
        error: () => {
          this.schedules.set([]);
        }
      });
  }

  saveSchedule(): void {
    if (this.scheduleForm.invalid || !this.duyuruId()) {
      this.toastr.error('Lütfen tüm alanları doldurun');
      return;
    }

    const formValue = this.scheduleForm.value;
    this.loading.set(true);

    if (formValue.zamanlamaTipi === 'TEK') {
      // Tek seferlik zamanlama
      const scheduledDateTime = new Date(formValue.zamanlananTarih);
      const [hours, minutes] = formValue.zamanlananSaat.split(':');
      scheduledDateTime.setHours(parseInt(hours), parseInt(minutes), 0);

      const request = {
        duyuruId: this.duyuruId(),
        zamanlanmaTarihi: scheduledDateTime.toISOString()
      };

      this.http.post<any>(`${environment.apiUrl}/api/schedules`, request).pipe(
        finalize(() => this.loading.set(false))
      ).subscribe({
          next: (response) => {
            if (response.success) {
              this.toastr.success('Zamanlama başarıyla oluşturuldu');
              this.router.navigate(['/planli-mailler']);
            } else {
              this.toastr.error(response.message || 'Zamanlama oluşturulamadı');
            }
          },
          error: (error) => {
            this.toastr.error(error.error?.message || 'Zamanlama oluşturulurken hata oluştu');
          }
        });
    } else {
      // Tekrarlı zamanlama
      const startDateTime = new Date(formValue.baslangicTarihi);
      const [startHours, startMinutes] = formValue.baslangicSaati.split(':');
      startDateTime.setHours(parseInt(startHours), parseInt(startMinutes), 0);

      const endDateTime = new Date(formValue.bitisTarihi);
      const [endHours, endMinutes] = formValue.baslangicSaati.split(':');
      endDateTime.setHours(parseInt(endHours), parseInt(endMinutes), 0);

      const request = {
        duyuruId: this.duyuruId(),
        baslangicTarihi: startDateTime.toISOString(),
        bitisTarihi: endDateTime.toISOString(),
        tekrarGunAraligi: formValue.tekrarGunAraligi
      };

      this.http.post<any>(`${environment.apiUrl}/api/schedules/bulk`, request).pipe(
        finalize(() => this.loading.set(false))
      ).subscribe({
          next: (response) => {
            if (response.success) {
              const count = response.data?.length || 0;
              this.toastr.success(`${count} adet zamanlama başarıyla oluşturuldu`);
              this.router.navigate(['/planli-mailler']);
            } else {
              this.toastr.error(response.message || 'Zamanlamalar oluşturulamadı');
            }
          },
          error: (error) => {
            this.toastr.error(error.error?.message || 'Zamanlamalar oluşturulurken hata oluştu');
          }
        });
    }
  }

  getSchedulePreview(): string | null {
    const formValue = this.scheduleForm.value;
    if (formValue.zamanlamaTipi !== 'TEKRARLI') return null;
    if (!formValue.baslangicTarihi || !formValue.bitisTarihi || !formValue.tekrarGunAraligi) return null;

    const start = new Date(formValue.baslangicTarihi);
    const end = new Date(formValue.bitisTarihi);
    const interval = formValue.tekrarGunAraligi;

    const diffDays = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
    const count = Math.floor(diffDays / interval) + 1;

    return `Toplam ${count} adet zamanlama oluşturulacak (Her ${interval} günde bir)`;
  }

  cancel(): void {
    // Duyuru listesine geri dön
    this.router.navigate(['/duyurular']);
  }
}
