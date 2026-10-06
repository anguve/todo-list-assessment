import { NgTemplateOutlet } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import {
  FieldRequirement,
  checkField,
  emailRequirements,
  fieldRules,
  nameRequirements,
  passwordRequirements,
} from '../../core/field-rules';
import { readProblem } from '../../core/problem';

type RegisterField = 'name' | 'email' | 'password' | 'confirm';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, NgTemplateOutlet],
  templateUrl: './register.html',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly passwordVisible = signal(false);
  protected readonly confirmVisible = signal(false);
  protected readonly activeField = signal<RegisterField | null>(null);
  protected readonly attempted = signal(false);

  protected readonly form = new FormGroup(
    {
      displayName: new FormControl('', { nonNullable: true, validators: [fieldRules('name')] }),
      email: new FormControl('', { nonNullable: true, validators: [fieldRules('email')] }),
      password: new FormControl('', { nonNullable: true, validators: [fieldRules('password')] }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [fieldRules('confirm')],
      }),
    },
    { validators: passwordsMatch },
  );

  /**
   * Remembers which field is being edited so its rules can stay open.
   * @param field The field that received focus.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected focusField(field: RegisterField): void {
    this.activeField.set(field);
  }

  /**
   * Closes the rules for a field when focus leaves it.
   * @param field The field that lost focus.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected blurField(field: RegisterField): void {
    if (this.activeField() === field) {
      this.activeField.set(null);
    }
  }

  /**
   * Name rules for the value currently in the form.
   * @returns The checklist.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected nameRules(): FieldRequirement[] {
    return nameRequirements(this.form.controls.displayName.value);
  }

  /**
   * Email rules for the value currently in the form.
   * @returns The checklist.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected emailRules(): FieldRequirement[] {
    return emailRequirements(this.form.controls.email.value);
  }

  /**
   * Password rules for the value currently in the form.
   * @returns The checklist.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected passwordRules(): FieldRequirement[] {
    return passwordRequirements(this.form.controls.password.value);
  }

  /**
   * The confirmation rule, met only when both passwords are the same and not empty.
   * @returns The checklist.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected confirmRules(): FieldRequirement[] {
    const password = this.form.controls.password.value;
    const confirmPassword = this.form.controls.confirmPassword.value;
    return [
      {
        id: 'confirm-match',
        label: 'Matches the password',
        met: confirmPassword.length > 0 && password === confirmPassword,
      },
    ];
  }

  /**
   * Shows the checklist while the field is active, unfinished, or failed on submit.
   * @param field The field the checklist belongs to.
   * @param value The current field value.
   * @param rules The checklist for that value.
   * @returns True when the list should be on screen.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected rulesOpen(field: RegisterField, value: string, rules: FieldRequirement[]): boolean {
    const unfinished = rules.some((rule) => !rule.met);
    return (
      this.activeField() === field ||
      (value.length > 0 && unfinished) ||
      (this.attempted() && unfinished)
    );
  }

  /**
   * Shows or hides the password.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected togglePassword(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  /**
   * Shows or hides the password confirmation.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected toggleConfirm(): void {
    this.confirmVisible.update((visible) => !visible);
  }

  /**
   * Checks the form, creates the account, and opens the task list.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected submit(): void {
    this.errorMessage.set(null);
    this.attempted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const displayName = checkField('name', this.form.controls.displayName.getRawValue());
    const email = checkField('email', this.form.controls.email.getRawValue());
    const password = checkField('password', this.form.controls.password.getRawValue());
    this.submitting.set(true);
    this.auth
      .register(email.value, password.value, displayName.value)
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

/**
 * Requires the password and the confirmation to be the same.
 * @param control The register form.
 * @returns Null when they match, otherwise a mismatch error.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
}
