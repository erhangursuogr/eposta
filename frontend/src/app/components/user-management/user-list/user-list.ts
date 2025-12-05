import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';
import Swal from 'sweetalert2';
import { UserService, UserListView, UserStatistics, Role, UpdateUserRequest } from '../../../services/user.service';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatPaginatorModule,
    MatSlideToggleModule
  ],
  templateUrl: './user-list.html',
  styleUrl: './user-list.css'
})
export class UserList implements OnInit {
  private userService = inject(UserService);
  private router = inject(Router);

  // Signals
  allUsers = signal<UserListView[]>([]); // Tüm kullanıcılar
  users = signal<UserListView[]>([]); // Filtrelenmiş kullanıcılar
  statistics = signal<UserStatistics | null>(null);
  loading = signal<boolean>(false);
  currentPage = signal<number>(1);
  totalPages = signal<number>(1);

  // Filters
  searchText = '';
  selectedRole = '';
  activeFilter = '';
  pageSize = 50;

  // Static data
  roles: Role[] = [];

  // Table columns
  displayedColumns: string[] = ['avatar', 'name', 'email', 'department', 'title', 'role', 'status', 'lastLogin', 'actions'];

  private searchTimeout: any;

  ngOnInit(): void {
    this.roles = this.userService.getStaticRoles();
    this.loadUsers();
    this.loadStatistics();
  }

  loadUsers(): void {
    this.loading.set(true);

    // Tüm kullanıcıları bir kere çek
    this.userService.getUserList(undefined, undefined, undefined, 1, 1000).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.allUsers.set(response.data);
          this.applyFilters();
        } else {
          Swal.fire('Hata', response.message, 'error');
        }
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading users:', err);
        Swal.fire('Hata', 'Kullanıcılar yüklenirken hata oluştu', 'error');
        this.loading.set(false);
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.allUsers()];

    // Arama filtresi
    if (this.searchText) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(u =>
        u.adSoyad.toLowerCase().includes(search) ||
        u.email.toLowerCase().includes(search) ||
        u.kullaniciAdi.toLowerCase().includes(search) ||
        (u.departman && u.departman.toLowerCase().includes(search))
      );
    }

    // Rol filtresi
    if (this.selectedRole) {
      filtered = filtered.filter(u => u.rolKodu === this.selectedRole);
    }

    // Durum filtresi
    if (this.activeFilter) {
      filtered = filtered.filter(u => u.aktif === this.activeFilter);
    }

    this.users.set(filtered);
  }

  loadStatistics(): void {
    this.userService.getStatistics().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.statistics.set(response.data);
        }
      },
      error: (err) => {
        console.error('Error loading statistics:', err);
      }
    });
  }

  onSearchChange(): void {
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.applyFilters();
    }, 300);
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  createUser(): void {
    this.router.navigate(['/kullanici-yonetimi/yeni']);
  }

  onToggleClicked(event: MatSlideToggleChange, user: UserListView): void {
  // Toggle’ı hemen geri çevir (kullanıcı onaylamadan)
  event.source.checked = user.aktif === 'Y';

  // Sonra onay penceresini aç
    this.toggleUserStatus(user);
  }

toggleUserStatus(user: UserListView): void {
  const newStatus = user.aktif === 'Y' ? 'N' : 'Y';
  const statusText = newStatus === 'Y' ? 'aktif' : 'pasif';

  Swal.fire({
    title: 'Kullanıcı Durumu',
    html: `<strong>${user.adSoyad}</strong> kullanıcısını ${statusText} yapmak istediğinize emin misiniz?`,
    icon: 'question',
    showCancelButton: true,
    confirmButtonColor: newStatus === 'Y' ? '#4caf50' : '#ff9800',
    cancelButtonColor: '#9e9e9e',
    confirmButtonText: `Evet, ${statusText.charAt(0).toUpperCase() + statusText.slice(1)} Yap`,
    cancelButtonText: 'Hayır'
  }).then((result) => {
    if (result.isConfirmed) {
      const request: UpdateUserRequest = {
        adSoyad: user.adSoyad,
        email: user.email,
        departman: user.departman || null,
        unvan: user.unvan || null,
        rolId: this.getRoleIdByCode(user.rolKodu),
        aktif: newStatus
      };

      this.userService.updateUser(user.id, request).subscribe({
        next: (response) => {
          if (response.success) {
            Swal.fire('Başarılı', `Kullanıcı ${statusText} yapıldı`, 'success');

            // Signal güncelle
            this.allUsers.update(users => {
              const idx = users.findIndex(u => u.id === user.id);
              if (idx !== -1) users[idx].aktif = newStatus;
              return [...users];
            });

            this.applyFilters();
            this.loadStatistics();
          } else {
            Swal.fire('Hata', response.message, 'error');
          }
        },
        error: (err) => {
          console.error('Error updating user status:', err);
          Swal.fire('Hata', 'Kullanıcı durumu güncellenirken hata oluştu', 'error');
        }
      });
    } else {
      // ❌ Kullanıcı “Hayır” dediyse hiçbir şey yapılmaz
    }
  });
}


  getRoleIdByCode(rolKodu: string): number {
    const role = this.roles.find(r => r.rolKodu === rolKodu);
    return role?.id || 0;
  }

  changeUserRole(user: UserListView, newRoleId: number): void {
    const newRole = this.roles.find(r => r.id === newRoleId);
    if (!newRole) return;

    Swal.fire({
      title: 'Rol Değiştir',
      html: `<strong>${user.adSoyad}</strong> kullanıcısının rolünü <strong>${newRole.rolAdi}</strong> yapmak istediğinize emin misiniz?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#3f51b5',
      cancelButtonColor: '#9e9e9e',
      confirmButtonText: 'Evet, Değiştir',
      cancelButtonText: 'İptal'
    }).then((result) => {
      if (result.isConfirmed) {
        const request: UpdateUserRequest = {
          adSoyad: user.adSoyad,
          email: user.email,
          departman: user.departman || null,
          unvan: user.unvan || null,
          rolId: newRoleId,
          aktif: user.aktif
        };

        this.userService.updateUser(user.id, request).subscribe({
          next: (response) => {
            if (response.success) {
              Swal.fire('Başarılı', 'Kullanıcı rolü değiştirildi', 'success');

              // Signal güncelle
              this.allUsers.update(users => {
                const idx = users.findIndex(u => u.id === user.id);
                if (idx !== -1) {
                  users[idx].rolKodu = newRole.rolKodu;
                  users[idx].rolAdi = newRole.rolAdi;
                }
                return [...users];
              });

              this.applyFilters();
              this.loadStatistics();
            } else {
              Swal.fire('Hata', response.message, 'error');
            }
          },
          error: (err) => {
            console.error('Error updating user role:', err);
            Swal.fire('Hata', 'Kullanıcı rolü güncellenirken hata oluştu', 'error');
          }
        });
      }
    });
  }

  deleteUser(user: UserListView): void {
    Swal.fire({
      title: 'Kullanıcıyı Sil',
      html: `<strong>${user.adSoyad}</strong> kullanıcısını silmek istediğinize emin misiniz?<br/><br/>
             <small>Not: Eğer kullanıcının sistemde kayıtları varsa (duyuru, onay, log) sadece pasif yapılacaktır.</small>`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#9e9e9e',
      confirmButtonText: 'Evet, Sil',
      cancelButtonText: 'İptal'
    }).then((result) => {
      if (result.isConfirmed) {
        this.userService.deleteUser(user.id).subscribe({
          next: (response) => {
            if (response.success) {
              Swal.fire('Başarılı', response.message, 'success');

              // Lokal veriyi güncelle - kullanıcıyı listeden kaldır
              const allUsersCopy = [...this.allUsers()];
              const userIndex = allUsersCopy.findIndex(u => u.id === user.id);
              if (userIndex !== -1) {
                allUsersCopy.splice(userIndex, 1);
                this.allUsers.set(allUsersCopy);
                this.applyFilters();
              }

              this.loadStatistics();
            } else {
              // Backend kayıtları olduğu için pasif yaptı
              Swal.fire('Bilgi', response.message, 'info');

              // Lokal veriyi güncelle - kullanıcıyı pasif yap
              const allUsersCopy = [...this.allUsers()];
              const userIndex = allUsersCopy.findIndex(u => u.id === user.id);
              if (userIndex !== -1) {
                allUsersCopy[userIndex].aktif = 'N';
                this.allUsers.set(allUsersCopy);
                this.applyFilters();
              }

              this.loadStatistics();
            }
          },
          error: (err) => {
            console.error('Error deleting user:', err);
            Swal.fire('Hata', 'Kullanıcı silinirken hata oluştu', 'error');
          }
        });
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.currentPage.set(event.pageIndex + 1);
    this.loadUsers();
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
