import { Component, inject, signal, OnInit } from '@angular/core';

import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { ToastrService } from 'ngx-toastr';
import { EmailGroupService } from '../../../../services/email-group.service';
import { UserDataService } from '../../../../services/userdata.service';
import type { EmailGroupDetail, EmailGroupMember, UpdateEmailGroupRequest } from '../../../../common/models/email-group.model';

@Component({
  selector: 'app-email-group-edit-dialog',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatSlideToggleModule
],
  templateUrl: './email-group-edit-dialog.html',
  styleUrl: './email-group-edit-dialog.css'
})
export class EmailGroupEditDialog implements OnInit {
  private dialogRef = inject(MatDialogRef<EmailGroupEditDialog>);
  private emailGroupService = inject(EmailGroupService);
  private userDataService = inject(UserDataService);
  private fb = inject(FormBuilder);
  private toastr = inject(ToastrService);
  data: { groupId: number } = inject(MAT_DIALOG_DATA);

  group = signal<EmailGroupDetail | null>(null);
  members = signal<EmailGroupMember[]>([]);
  loading = signal(true);
  saving = signal(false);

  editForm!: FormGroup;

  // NORMAL grup için yeni üye ekleme
  newMemberEmail = '';
  newMemberName = '';

  get isAdmin(): boolean {
    return this.userDataService.user()?.role === 'ADMIN';
  }

  ngOnInit(): void {
    this.initializeForm();
    this.loadGroup();
  }

  initializeForm(): void {
    this.editForm = this.fb.group({
      grupAdi: ['', [Validators.required, Validators.maxLength(100)]],
      aciklama: ['', Validators.maxLength(500)],
      aktif: [true],
      viewAdi: [''],
      filterKosulu: ['', Validators.maxLength(500)],
      listeciEmail: ['', Validators.email]
    });
  }

  loadGroup(): void {
    this.emailGroupService.getGroupById(this.data.groupId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const group = response.data;
          this.group.set(group);

          // Form değerlerini doldur
          this.editForm.patchValue({
            grupAdi: group.grupAdi,
            aciklama: group.aciklama || '',
            aktif: group.aktif === 'Y',
            viewAdi: group.viewAdi || '',
            filterKosulu: group.filterKosulu || '',
            listeciEmail: group.listeciEmail || ''
          });

          // MANUEL/DOSYA gruplar için üyeleri yükle
          if (group.grupTipi === 'MANUEL' || group.grupTipi === 'DOSYA') {
            this.loadMembers();
          }
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toastr.error('Grup bilgileri yüklenemedi');
      }
    });
  }

  loadMembers(): void {
    this.emailGroupService.getGroupMembers(this.data.groupId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.members.set(response.data);
        }
      }
    });
  }

  addMember(): void {
    if (!this.newMemberEmail || !this.newMemberName) {
      this.toastr.warning('Email ve ad soyad zorunludur');
      return;
    }

    let email = this.newMemberEmail.trim();
    if (!email.includes('@')) {
      email = email + '@deu.edu.tr';
    }

    this.emailGroupService.addMember(this.data.groupId, { email, adSoyad: this.newMemberName }).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Üye eklendi');
          this.newMemberEmail = '';
          this.newMemberName = '';
          this.loadMembers();
        } else {
          this.toastr.error(response.message || 'Üye eklenemedi');
        }
      },
      error: (error) => {
        this.toastr.error(error.error?.message || 'Üye eklenirken hata oluştu');
      }
    });
  }

  removeMember(memberId: number): void {
    this.emailGroupService.removeMember(this.data.groupId, memberId).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Üye silindi');
          this.loadMembers();
        } else {
          this.toastr.error(response.message || 'Üye silinemedi');
        }
      },
      error: (error) => {
        this.toastr.error(error.error?.message || 'Üye silinirken hata oluştu');
      }
    });
  }

  onSubmit(): void {
    if (this.editForm.invalid) {
      this.toastr.warning('Lütfen tüm zorunlu alanları doldurunuz');
      return;
    }

    const request: UpdateEmailGroupRequest = {
      grupAdi: this.editForm.value.grupAdi,
      aciklama: this.editForm.value.aciklama
    };

    const grupTipi = this.group()?.grupTipi;

    // Aktif/Pasif durumu (sadece ADMIN değiştirebilir)
    if (this.isAdmin) {
      request.aktif = this.editForm.value.aktif ? 'Y' : 'N';
    }

    if (grupTipi === 'DINAMIK') {
      request.filterKosulu = this.editForm.value.filterKosulu;
    }

    // DEBIS gruplarında listeci email güncellenemez (manuel veritabanı işlemi)

    this.saving.set(true);
    this.emailGroupService.updateGroup(this.data.groupId, request).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastr.success('Grup güncellendi');
          this.dialogRef.close(true);
        } else {
          this.toastr.error(response.message || 'Grup güncellenemedi');
          this.saving.set(false);
        }
      },
      error: (error) => {
        this.toastr.error(error.error?.message || 'Grup güncellenirken hata oluştu');
        this.saving.set(false);
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }

  getGrupTipiText(grupTipi: string): string {
    return this.emailGroupService.getGrupTipiText(grupTipi);
  }

  getGrupTipiBadge(grupTipi: string): string {
    return this.emailGroupService.getGrupTipiBadge(grupTipi);
  }
}
