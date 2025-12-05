import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { UserDataService } from '../../services/userdata.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const userDataService = inject(UserDataService);
  const router = inject(Router);

  // Önce authentication kontrol et
  if (!userDataService.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }

  // Route data'dan gerekli rolleri al
  const requiredRoles = route.data['roles'] as string[] | undefined;

  // Eğer rol gereksinimleri yoksa, sadece authentication yeterli
  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  // Kullanıcının rolünü al
  const userRole = userDataService.user().role;

  // Kullanıcının rolü gerekli roller arasında mı kontrol et
  if (userRole && requiredRoles.includes(userRole)) {
    return true;
  } else {
    // Yetkisiz erişim
    router.navigate(['/access-denied']);
    return false;
  }
};
