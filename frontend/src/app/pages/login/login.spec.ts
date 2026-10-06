import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Login } from './login';

@Component({ selector: 'app-probe', template: '' })
class Probe {}

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([{ path: 'todos', component: Probe }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('reveals the password and hides it again', async () => {
    const input = fixture.nativeElement.querySelector('#password') as HTMLInputElement;
    expect(input.type).toBe('password');

    (fixture.nativeElement.querySelector('button[aria-label="Show password"]') as HTMLButtonElement).click();
    await render();
    expect(input.type).toBe('text');

    (fixture.nativeElement.querySelector('button[aria-label="Hide password"]') as HTMLButtonElement).click();
    await render();
    expect(input.type).toBe('password');
  });

  it('shows a validation error for an invalid email', async () => {
    setValue('email', 'not-an-email');
    await render();
    expect(text()).toContain('Enter a valid email.');
  });

  it('shows validation errors when the empty form is submitted', async () => {
    submit();
    await render();
    expect(text()).toContain('Enter a valid email.');
    expect(text()).toContain('Enter your password.');
  });

  it('disables the button while the request is in flight', async () => {
    setValue('email', 'ada@example.com');
    setValue('password', 'password1');
    submit();
    await render();

    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    expect(button.textContent).toContain('Signing in');

    httpMock.expectOne('/api/auth/login').flush({
      token: 'abc',
      expiresAt: new Date().toISOString(),
      user: { id: '1', email: 'ada@example.com', displayName: 'Ada' },
    });
  });

  /**
   * Types into one input and marks it touched.
   * @param id The input id.
   * @param value The text to type.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  function setValue(id: string, value: string): void {
    const input = fixture.nativeElement.querySelector(`#${id}`) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
  }

  /**
   * Submits the sign-in form.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  function submit(): void {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
  }

  /**
   * Renders the page and waits until it is stable.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  async function render(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
  }

  /**
   * Returns the visible text of the page.
   * @returns The text content.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  function text(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }
});
