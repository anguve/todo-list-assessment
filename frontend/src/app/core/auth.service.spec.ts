import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

@Component({ selector: 'app-probe', template: '' })
class Probe {}

describe('AuthService', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'login', component: Probe },
          { path: 'todos', component: Probe },
        ]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    await TestBed.inject(Router).navigateByUrl('/todos');
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('sends the bearer token to sign-out, then clears the session', async () => {
    rememberSession();
    const auth = TestBed.inject(AuthService);

    auth.logout();

    const request = httpMock.expectOne('/api/auth/logout');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc');
    expect(localStorage.getItem('fieldbook.token')).toBe('abc');
    request.flush(null, { status: 204, statusText: 'No Content' });

    await TestBed.inject(ApplicationRef).whenStable();
    expect(localStorage.getItem('fieldbook.token')).toBeNull();
    expect(auth.isAuthenticated()).toBe(false);
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('clears the session when sign-out cannot reach the API', async () => {
    rememberSession();
    const auth = TestBed.inject(AuthService);

    auth.logout();
    httpMock
      .expectOne('/api/auth/logout')
      .flush({ title: 'Error' }, { status: 500, statusText: 'Error' });

    await TestBed.inject(ApplicationRef).whenStable();
    expect(localStorage.getItem('fieldbook.token')).toBeNull();
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('does not call the API when there is no session', async () => {
    const auth = TestBed.inject(AuthService);

    auth.logout();

    httpMock.expectNone('/api/auth/logout');
    await TestBed.inject(ApplicationRef).whenStable();
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('keeps the token returned by login', () => {
    const auth = TestBed.inject(AuthService);

    auth.login('ada@example.com', 'password1').subscribe();
    httpMock.expectOne('/api/auth/login').flush({
      token: 'fresh-token',
      expiresAt: new Date().toISOString(),
      user: { id: '1', email: 'ada@example.com', displayName: 'Ada' },
    });

    expect(auth.token()).toBe('fresh-token');
    expect(auth.currentUser()?.email).toBe('ada@example.com');
    expect(localStorage.getItem('fieldbook.token')).toBe('fresh-token');
  });
});

/**
 * Saves a fake session before AuthService is created.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function rememberSession(): void {
  localStorage.setItem('fieldbook.token', 'abc');
  localStorage.setItem(
    'fieldbook.user',
    JSON.stringify({ id: '1', email: 'ada@example.com', displayName: 'Ada' }),
  );
}
