import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { UserDataService } from '../../services/userdata.service';

export const authGuard: CanActivateFn = (route, state) => {
  const userDataService = inject(UserDataService);
  const router = inject(Router);

  if (userDataService.isAuthenticated()) {
    return true;
  } else {
    router.navigate(['/login']);
    return false;
  }
};
