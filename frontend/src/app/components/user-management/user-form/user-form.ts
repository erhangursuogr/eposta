import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import Swal from 'sweetalert2';
import {
  UserService,
  UserDetailView,
  CreateUserRequest,
  UpdateUserRequest,
  Role
} from '../../../services/user.service';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
],
  templateUrl: './user-form.html',
  styleUrl: './user-form.css'
})
export class UserForm implements OnInit {
  private fb = inject(FormBuilder);
  private userService = inject(UserService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  // Signals
  loading = signal<boolean>(false);
  saving = signal<boolean>(false);
  isEditMode = signal<boolean>(false);
  currentUser = signal<UserDetailView | null>(null);

  // Form
  userForm!: FormGroup;
  roles: Role[] = [];
  userId: number | null = null;

  ngOnInit(): void {
    this.roles = this.userService.getStaticRoles();
    this.initForm();
  }

  initForm(): void {
    // Create mode: only email, rolId, aktif
    this.userForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      rolId: ['', Validators.required],
      aktif: ['Y', Validators.required]
    });
  }

  save(): void {
    if (!this.userForm.valid) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    // Create user
    const request: CreateUserRequest = {
      email: this.userForm.value.email,
      rolId: +this.userForm.value.rolId,
      aktif: this.userForm.value.aktif
    };

    this.userService.createUser(request).subscribe({
      next: (response) => {
        if (response.success) {
          Swal.fire('Başarılı', 'Kullanıcı oluşturuldu', 'success');
          this.router.navigate(['/kullanici-yonetimi']);
        } else {
          Swal.fire('Hata', response.message, 'error');
        }
        this.saving.set(false);
      },
      error: (err) => {
        console.error('Error creating user:', err);
        Swal.fire('Hata', err.error?.message || 'Kullanıcı oluşturulurken hata oluştu', 'error');
        this.saving.set(false);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/kullanici-yonetimi']);
  }

  getUserInitials(fullName: string): string {
    const parts = fullName.split(' ');
    if (parts.length >= 2) {
      return parts[0].charAt(0) + parts[parts.length - 1].charAt(0);
    }
    return fullName.charAt(0);
  }

  getRoleIcon(rolKodu: string): string {
    const iconMap: Record<string, string> = {
      'ADMIN': 'admin_panel_settings',
      'MANAGER': 'manage_accounts',
      'COORDINATOR': 'supervisor_account',
      'EDITOR': 'edit',
      'VIEWER': 'visibility'
    };
    return iconMap[rolKodu] || 'person';
  }
}
