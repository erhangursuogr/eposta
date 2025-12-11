import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { UserDataService } from '../../services/userdata.service';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

/**
 * Login Guard - SSO modunda login sayfasını engeller
 */
export const loginGuard: CanActivateFn = async (route, state) => {
  const userDataService = inject(UserDataService);
  const authService = inject(AuthService);
  const router = inject(Router);

  // Zaten authenticated ise ana sayfaya yönlendir
  if (userDataService.isAuthenticated()) {
    router.navigate(['/']);
    return false;
  }

  // AUTH_MODE check
  const mode = await authService.getAuthMode();

  if (mode === '1') {
    // SSO mode: Login sayfası yok, Keycloak'a redirect
    window.location.href = environment.keycloakAuthUrl;
    return false;
  } else {
    // LDAP mode: Login sayfasını göster
    return true;
  }
};
