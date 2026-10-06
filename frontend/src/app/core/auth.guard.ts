import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Sends an anonymous visitor to the sign-in page.
 * @returns True when a session exists, otherwise the login route.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated()) {
    return true;
  }

  return inject(Router).createUrlTree(['/login']);
};

/**
 * Sends a signed-in visitor away from the sign-in and register pages.
 * @returns True when there is no session, otherwise the task list route.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated()) {
    return inject(Router).createUrlTree(['/todos']);
  }

  return true;
};
