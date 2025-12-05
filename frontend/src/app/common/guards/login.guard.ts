import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { UserDataService } from '../../services/userdata.service';

export const loginGuard: CanActivateFn = (route, state) => {
  const userDataService = inject(UserDataService);
  const router = inject(Router);

  if (userDataService.isAuthenticated()) {
    // Zaten login olmuş, ana sayfaya yönlendir
    router.navigate(['/']);
    return false;
  } else {
    // Login olmamış, login sayfasına girebilir
    return true;
  }
};
