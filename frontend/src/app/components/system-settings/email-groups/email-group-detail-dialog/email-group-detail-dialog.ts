import { Component, inject, signal, OnInit } from '@angular/core';

import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { EmailGroupService } from '../../../../services/email-group.service';
import type { EmailGroupDetail, EmailGroupMember } from '../../../../common/models/email-group.model';

@Component({
  selector: 'app-email-group-detail-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    MatDividerModule
],
  templateUrl: './email-group-detail-dialog.html',
  styleUrl: './email-group-detail-dialog.css'
})
export class EmailGroupDetailDialog implements OnInit {
  private dialogRef = inject(MatDialogRef<EmailGroupDetailDialog>);
  private emailGroupService = inject(EmailGroupService);
  data: { groupId: number } = inject(MAT_DIALOG_DATA);

  group = signal<EmailGroupDetail | null>(null);
  members = signal<EmailGroupMember[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadGroupDetail();
    this.loadMembers();
  }

  loadGroupDetail(): void {
    this.emailGroupService.getGroupById(this.data.groupId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.group.set(response.data);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  loadMembers(): void {
    // NORMAL ve STATIK gruplar için üyeleri yükle
    this.emailGroupService.getGroupMembers(this.data.groupId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.members.set(response.data);
        }
      }
    });
  }

  getGrupTipiText(grupTipi: string): string {
    return this.emailGroupService.getGrupTipiText(grupTipi);
  }

  getGrupTipiBadge(grupTipi: string): string {
    return this.emailGroupService.getGrupTipiBadge(grupTipi);
  }

  formatDate(date: Date | string): string {
    if (!date) return '-';
    const d = new Date(date);
    return d.toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
