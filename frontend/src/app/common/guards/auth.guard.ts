import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { UserDataService } from '../../services/userdata.service';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

export const authGuard: CanActivateFn = async (route, state) => {
  const userDataService = inject(UserDataService);
  const authService = inject(AuthService);
  const router = inject(Router);

  // Session check
  if (userDataService.isAuthenticated() || authService.checkSession()) {
    return true;
  }

  // AUTH_MODE check
  const mode = await authService.getAuthMode();

  if (mode === '1') {
    // SSO mode: Keycloak redirect
    window.location.href = environment.keycloakAuthUrl;
    return false;
  } else {
    // LDAP mode: Login page
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }
};
