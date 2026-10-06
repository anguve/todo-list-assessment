import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { checkField, fieldRules } from '../../core/field-rules';
import { readProblem } from '../../core/problem';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly passwordVisible = signal(false);

  protected readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [fieldRules('email')] }),
    password: new FormControl('', { nonNullable: true, validators: [fieldRules('password')] }),
  });

  /**
   * Shows or hides the password.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected togglePassword(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  /**
   * Checks the form, signs in, and opens the task list.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const email = checkField('email', this.form.controls.email.getRawValue());
    const password = checkField('password', this.form.controls.password.getRawValue());
    this.submitting.set(true);
    this.auth
      .login(email.value, password.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          void this.router.navigate(['/todos']);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(readProblem(error));
        },
      });
  }
}
