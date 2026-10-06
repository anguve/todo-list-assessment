import { provideHttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { authGuard, guestGuard } from './auth.guard';

@Component({ selector: 'app-probe', template: '' })
class Probe {}

/**
 * Saves a fake session so a guard test can act as a signed-in user.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function rememberSession(): void {
  localStorage.setItem('fieldbook.token', 'token');
  localStorage.setItem(
    'fieldbook.user',
    JSON.stringify({ id: '1', email: 'ada@example.com', displayName: 'Ada' }),
  );
}

describe('authGuard', () => {
  afterEach(() => localStorage.clear());

  it('blocks the task route without a session', async () => {
    await setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/todos');
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('allows the task route with a session', async () => {
    rememberSession();
    await setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/todos');
    expect(TestBed.inject(Router).url).toBe('/todos');
  });
});

describe('guestGuard', () => {
  afterEach(() => localStorage.clear());

  it('lets a visitor open the sign-in page', async () => {
    await setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/login');
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('sends a signed-in user from login to the task list', async () => {
    rememberSession();
    await setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/login');
    expect(TestBed.inject(Router).url).toBe('/todos');
  });
});

/**
 * Builds the router used by the guard tests.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
async function setup(): Promise<void> {
  await TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideRouter([
        { path: 'login', component: Probe, canActivate: [guestGuard] },
        { path: 'todos', component: Probe, canActivate: [authGuard] },
      ]),
    ],
  }).compileComponents();
}
