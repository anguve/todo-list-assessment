import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';

@Component({ selector: 'app-probe', template: '' })
class Probe {}

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    localStorage.setItem('fieldbook.token', 'abc');
    localStorage.setItem(
      'fieldbook.user',
      JSON.stringify({ id: '1', email: 'ada@example.com', displayName: 'Ada' }),
    );

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

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    await TestBed.inject(Router).navigateByUrl('/todos');
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('adds the bearer token and clears the session on 401', async () => {
    http.get('/api/todos').subscribe({ error: () => undefined });

    const request = httpMock.expectOne('/api/todos');
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc');
    request.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    await TestBed.inject(ApplicationRef).whenStable();
    expect(localStorage.getItem('fieldbook.token')).toBeNull();
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('does not clear the session when login itself returns 401', () => {
    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });

    const request = httpMock.expectOne('/api/auth/login');
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc');
    request.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(localStorage.getItem('fieldbook.token')).toBe('abc');
    expect(TestBed.inject(Router).url).toBe('/todos');
  });

  it('does not start another sign-out when logout itself returns 401', () => {
    http.post('/api/auth/logout', {}).subscribe({ error: () => undefined });

    const request = httpMock.expectOne('/api/auth/logout');
    request.flush({}, { status: 401, statusText: 'Unauthorized' });

    httpMock.expectNone('/api/auth/logout');
    expect(localStorage.getItem('fieldbook.token')).toBe('abc');
  });
});
