import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Register } from './register';

@Component({ selector: 'app-probe', template: '' })
class Probe {}

describe('Register', () => {
  let fixture: ComponentFixture<Register>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        provideRouter([{ path: 'todos', component: Probe }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('ticks the email rules as the address is typed', async () => {
    expect(text()).not.toContain('6–254 characters, like name@example.com.');

    focus('email');
    await render();
    expect(met('email-shape')).toBe('false');

    setValue('email', 'ada');
    focus('email');
    await render();
    expect(met('email-min')).toBe('false');
    expect(met('email-shape')).toBe('false');

    setValue('email', 'ada@example.com');
    focus('email');
    await render();
    expect(met('email-min')).toBe('true');
    expect(met('email-shape')).toBe('true');
  });

  it('shows a validation error for a short name', async () => {
    setValue('name', 'A');
    await render();
    expect(text()).toContain('Enter your name.');
  });

  it('reveals the password without revealing the confirmation', async () => {
    const password = fixture.nativeElement.querySelector('#password') as HTMLInputElement;
    const confirm = fixture.nativeElement.querySelector('#confirmPassword') as HTMLInputElement;
    const [showPassword, showConfirm] = [
      ...fixture.nativeElement.querySelectorAll('button[aria-label="Show password"]'),
    ] as HTMLButtonElement[];

    showPassword.click();
    await render();
    expect(password.type).toBe('text');
    expect(confirm.type).toBe('password');

    showConfirm.click();
    await render();
    expect(confirm.type).toBe('text');
  });

  it('shows a validation error for an invalid email', async () => {
    setValue('email', 'not-an-email');
    await render();
    expect(text()).toContain('Enter a valid email.');
  });

  it('shows a validation error for a short password', async () => {
    setValue('password', 'abc');
    await render();
    expect(text()).toContain('Use at least 8 characters.');
  });

  it('shows a validation error when the passwords do not match', async () => {
    setValue('password', 'password1');
    setValue('confirmPassword', 'password2');
    await render();
    expect(text()).toContain('Passwords do not match.');
  });

  it('disables the button while the request is in flight', async () => {
    setValue('name', 'Ada');
    setValue('email', 'ada@example.com');
    setValue('password', 'password1');
    setValue('confirmPassword', 'password1');
    submit();
    await render();

    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    expect(button.textContent).toContain('Creating account');

    httpMock.expectOne('/api/auth/register').flush({
      token: 'abc',
      expiresAt: new Date().toISOString(),
      user: { id: '1', email: 'ada@example.com', displayName: 'Ada' },
    });
  });

  /**
   * Focuses one input so its checklist opens.
   * @param id The input id.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  function focus(id: string): void {
    const input = fixture.nativeElement.querySelector(`#${id}`) as HTMLInputElement;
    input.dispatchEvent(new FocusEvent('focus'));
  }

  /**
   * Reads whether one checklist row is complete.
   * @param id The requirement id.
   * @returns The data-met value.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  function met(id: string): string | null {
    const row = fixture.nativeElement.querySelector(`[data-requirement="${id}"]`) as HTMLElement | null;
    return row?.getAttribute('data-met') ?? null;
  }

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
   * Submits the registration form.
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
