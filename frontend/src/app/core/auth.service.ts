import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, tap } from 'rxjs';
import { AuthResponse, CurrentUser } from './models';

const tokenKey = 'fieldbook.token';
const userKey = 'fieldbook.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(null);
  readonly currentUser = signal<CurrentUser | null>(null);
  readonly isAuthenticated = computed(() => this.token() !== null && this.currentUser() !== null);

  /**
   * Restores a session that was saved in this browser.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  constructor() {
    this.restore();
  }

  /**
   * Signs in and keeps the token for later requests.
   * @param email The cleaned email.
   * @param password The password as typed.
   * @returns The token response.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  login(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/login', { email, password })
      .pipe(tap((response) => this.establishSession(response)));
  }

  /**
   * Creates an account and signs the user in with the returned token.
   * @param email The cleaned email.
   * @param password The password as typed.
   * @param displayName The cleaned name.
   * @returns The token response.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  register(email: string, password: string, displayName: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/register', { email, password, displayName })
      .pipe(tap((response) => this.establishSession(response)));
  }

  /**
   * Asks the API to revoke the current token, then drops the local session.
   * The token stays in place until the request is sent, so the interceptor can attach it.
   * The local session is cleared even when the API cannot be reached.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  logout(): void {
    if (this.token() === null) {
      this.endLocalSession();
      return;
    }

    this.http
      .post('/api/auth/logout', {})
      .pipe(finalize(() => this.endLocalSession()))
      .subscribe({ error: () => undefined });
  }

  /**
   * Removes the saved session and opens the sign-in page. It does not call the API.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  endLocalSession(): void {
    this.clear();
    void this.router.navigate(['/login']);
  }

  /**
   * Saves the token and profile, then updates the signals.
   * @param response The auth response from the API.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  private establishSession(response: AuthResponse): void {
    localStorage.setItem(tokenKey, response.token);
    localStorage.setItem(userKey, JSON.stringify(response.user));
    this.token.set(response.token);
    this.currentUser.set(response.user);
  }

  /**
   * Loads a complete saved session, or clears a partial one.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  private restore(): void {
    const token = localStorage.getItem(tokenKey);
    const user = this.readUser();
    if (token && user) {
      this.token.set(token);
      this.currentUser.set(user);
      return;
    }

    this.clear();
  }

  /**
   * Reads the saved profile and rejects a value that is not a user.
   * @returns The profile, or null.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  private readUser(): CurrentUser | null {
    const raw = localStorage.getItem(userKey);
    if (!raw) {
      return null;
    }

    try {
      const parsed = JSON.parse(raw) as CurrentUser;
      if (!parsed?.id || !parsed?.email) {
        return null;
      }

      return parsed;
    } catch {
      return null;
    }
  }

  /**
   * Removes the token and profile from memory and from local storage.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  private clear(): void {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(userKey);
    this.token.set(null);
    this.currentUser.set(null);
  }
}
