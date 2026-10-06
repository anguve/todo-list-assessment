import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Adds the bearer token and clears the session when a protected call returns 401.
 * @param req The outgoing request.
 * @param next The next handler.
 * @returns The response stream.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();
  const outgoing = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isCredentialRequest(req.url)
      ) {
        auth.endLocalSession();
      }

      return throwError(() => error);
    }),
  );
};

/**
 * Login, register, and sign-out are allowed to return 401 without starting another sign-out.
 * @param url The request URL.
 * @returns True for the credential routes.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function isCredentialRequest(url: string): boolean {
  return (
    url.includes('/api/auth/login') ||
    url.includes('/api/auth/register') ||
    url.includes('/api/auth/logout')
  );
}
