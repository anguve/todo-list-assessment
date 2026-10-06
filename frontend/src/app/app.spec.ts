import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { authInterceptor } from './core/auth.interceptor';

@Component({ selector: 'app-probe', template: '' })
class Probe {}

describe('App', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([{ path: 'login', component: Probe }]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('shows the product name', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Fieldbook');
  });

  it('sends Sign out to the API and then leaves the session', async () => {
    localStorage.setItem('fieldbook.token', 'abc');
    localStorage.setItem(
      'fieldbook.user',
      JSON.stringify({ id: '1', email: 'ada@example.com', displayName: 'Ada' }),
    );

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const button = [...(fixture.nativeElement as HTMLElement).querySelectorAll('button')].find((entry) =>
      entry.textContent?.includes('Sign out'),
    ) as HTMLButtonElement;
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Ada');
    button.click();

    const request = httpMock.expectOne('/api/auth/logout');
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(localStorage.getItem('fieldbook.token')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Sign out');
  });
});
