import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export type FieldKind = 'email' | 'name' | 'password' | 'confirm' | 'title' | 'description';

export interface FieldCheck {
  ok: boolean;
  value: string;
  message: string;
}

export interface FieldRequirement {
  id: string;
  label: string;
  met: boolean;
}

const emailPattern =
  /^[a-z0-9](?:[a-z0-9._%+-]{0,62}[a-z0-9])?@[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)+$/;
const namePattern = /^[\p{L}][\p{L}\p{M}0-9 .'-]{1,79}$/u;
const passwordPattern = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z0-9!@#$%^&*()_+\-=.,?]{8,64}$/;
const titlePattern = /^[\p{L}\p{N}][\p{L}\p{M}\p{N} .,!?'"()\-:;/&+]{1,119}$/u;
const descriptionPattern = /^[\p{L}\p{N}][\p{L}\p{M}\p{N} .,!?'"()\-:;/&+]{1,399}$/u;

export const fieldLimits = {
  email: { min: 6, max: 254 },
  name: { min: 2, max: 80 },
  password: { min: 8, max: 64 },
  title: { min: 2, max: 120 },
  description: { min: 2, max: 400 },
} as const;

/**
 * Builds an Angular validator that uses the same limits and patterns as the API.
 * @param kind The field being checked.
 * @returns A validator that stores the English message on the `rules` error.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
export function fieldRules(kind: FieldKind): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const result = checkField(kind, String(control.value ?? ''));
    return result.ok ? null : { rules: result.message };
  };
}

/**
 * Cleans and checks one field the same way the API does before it is sent.
 * @param kind The field being checked.
 * @param raw The value as typed.
 * @returns Whether it passed, the value to send, and the English message.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
export function checkField(kind: FieldKind, raw: string): FieldCheck {
  if (kind === 'email') {
    return checkEmail(raw);
  }

  if (kind === 'name') {
    return checkName(raw);
  }

  if (kind === 'title') {
    return checkTitle(raw);
  }

  if (kind === 'description') {
    return checkDescription(raw);
  }

  return checkPassword(raw, kind === 'confirm');
}

/**
 * Cleans an email, then checks its length and pattern.
 * @param raw The submitted email.
 * @returns The check result. The value is lowercase.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function checkEmail(raw: string): FieldCheck {
  const value = cleanEmail(raw);
  if (
    value.length < fieldLimits.email.min ||
    value.length > fieldLimits.email.max ||
    !emailPattern.test(value)
  ) {
    return { ok: false, value, message: 'Enter a valid email.' };
  }

  return { ok: true, value, message: '' };
}

/**
 * Cleans a display name, then checks its length and pattern.
 * @param raw The submitted name.
 * @returns The check result.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function checkName(raw: string): FieldCheck {
  const value = cleanText(raw);
  if (value.length < fieldLimits.name.min) {
    return { ok: false, value, message: 'Enter your name.' };
  }

  if (value.length > fieldLimits.name.max) {
    return { ok: false, value, message: 'Use 80 characters or fewer.' };
  }

  if (!namePattern.test(value)) {
    return {
      ok: false,
      value,
      message: 'Use letters, numbers, spaces, apostrophes, and hyphens.',
    };
  }

  return { ok: true, value, message: '' };
}

/**
 * Checks a password without rewriting it.
 * @param raw The password as typed.
 * @param confirm True when this is the confirmation field.
 * @returns The check result. The value is unchanged.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function checkPassword(raw: string, confirm: boolean): FieldCheck {
  if (!raw) {
    return {
      ok: false,
      value: raw,
      message: confirm ? 'Confirm your password.' : 'Enter your password.',
    };
  }

  if (raw.length < fieldLimits.password.min) {
    return { ok: false, value: raw, message: 'Use at least 8 characters.' };
  }

  if (raw.length > fieldLimits.password.max) {
    return { ok: false, value: raw, message: 'Use 64 characters or fewer.' };
  }

  if (!passwordPattern.test(raw)) {
    return {
      ok: false,
      value: raw,
      message: 'Use letters and numbers. Spaces and other symbols are not allowed.',
    };
  }

  return { ok: true, value: raw, message: '' };
}

/**
 * Cleans a task title, then checks its length and pattern.
 * @param raw The submitted title.
 * @returns The check result.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function checkTitle(raw: string): FieldCheck {
  const value = cleanText(raw);
  if (value.length < fieldLimits.title.min) {
    return { ok: false, value, message: 'Write a task first.' };
  }

  if (value.length > fieldLimits.title.max) {
    return { ok: false, value, message: 'Keep it under 120 characters.' };
  }

  if (!titlePattern.test(value)) {
    return { ok: false, value, message: 'Use letters, numbers, and simple punctuation.' };
  }

  return { ok: true, value, message: '' };
}

/**
 * Cleans a task description, then checks its length and pattern.
 * @param raw The submitted description.
 * @returns The check result.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function checkDescription(raw: string): FieldCheck {
  const value = cleanText(raw);
  if (value.length < fieldLimits.description.min) {
    return { ok: false, value, message: 'Write a description first.' };
  }

  if (value.length > fieldLimits.description.max) {
    return { ok: false, value, message: 'Keep the description under 400 characters.' };
  }

  if (!descriptionPattern.test(value)) {
    return { ok: false, value, message: 'Use letters, numbers, and simple punctuation.' };
  }

  return { ok: true, value, message: '' };
}

/**
 * Splits the name rules so the register form can tick them off while typing.
 * @param raw The name as typed.
 * @returns One entry per rule, in display order.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
export function nameRequirements(raw: string): FieldRequirement[] {
  const value = cleanText(raw);
  return [
    { id: 'name-min', label: 'At least 2 characters', met: value.length >= fieldLimits.name.min },
    {
      id: 'name-max',
      label: '80 characters or fewer',
      met: value.length > 0 && value.length <= fieldLimits.name.max,
    },
    { id: 'name-start', label: 'Starts with a letter', met: /^[\p{L}]/u.test(value) },
    {
      id: 'name-chars',
      label: 'Letters, numbers, spaces, apostrophes, and hyphens',
      met: value.length > 0 && /^[\p{L}\p{M}0-9 .'-]+$/u.test(value),
    },
  ];
}

/**
 * Splits the email rules so the register form can tick them off while typing.
 * @param raw The email as typed.
 * @returns One entry per rule, in display order.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
export function emailRequirements(raw: string): FieldRequirement[] {
  const value = cleanEmail(raw);
  return [
    { id: 'email-min', label: 'At least 6 characters', met: value.length >= fieldLimits.email.min },
    {
      id: 'email-max',
      label: '254 characters or fewer',
      met: value.length > 0 && value.length <= fieldLimits.email.max,
    },
    { id: 'email-shape', label: 'Includes @ and a domain', met: emailPattern.test(value) },
  ];
}

/**
 * Splits the password rules so the register form can tick them off while typing.
 * @param raw The password as typed.
 * @returns One entry per rule, in display order.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
export function passwordRequirements(raw: string): FieldRequirement[] {
  return [
    {
      id: 'password-min',
      label: 'At least 8 characters',
      met: raw.length >= fieldLimits.password.min,
    },
    {
      id: 'password-max',
      label: '64 characters or fewer',
      met: raw.length > 0 && raw.length <= fieldLimits.password.max,
    },
    { id: 'password-letter', label: 'Includes a letter', met: /[A-Za-z]/.test(raw) },
    { id: 'password-number', label: 'Includes a number', met: /\d/.test(raw) },
    {
      id: 'password-chars',
      label: 'No spaces or unusual symbols',
      met: raw.length > 0 && /^[A-Za-z0-9!@#$%^&*()_+\-=.,?]+$/.test(raw),
    },
  ];
}

/**
 * Trims an email, drops spaces and unsafe characters, and lowercases the rest.
 * @param raw The submitted email.
 * @returns The cleaned email.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function cleanEmail(raw: string): string {
  return raw
    .trim()
    .replace(/[\u0000-\u001F\u007F\s<>"']/g, '')
    .toLowerCase();
}

/**
 * Decodes simple entities, strips tags, and collapses whitespace.
 * @param raw The submitted text.
 * @returns The cleaned text.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
function cleanText(raw: string): string {
  let value = raw;
  for (let pass = 0; pass < 2; pass += 1) {
    value = value
      .replace(/&nbsp;/gi, ' ')
      .replace(/&amp;/gi, '&')
      .replace(/&quot;/gi, '"')
      .replace(/&apos;|&#39;/gi, "'")
      .replace(/&lt;/gi, '<')
      .replace(/&gt;/gi, '>');
  }

  value = value.replace(/<(script|style)\b[^>]*>[\s\S]*?<\/\1>/gi, '');
  value = value.replace(/<[^>]*>/g, '');
  value = value.replace(/[\u0000-\u001F\u007F<>]/g, '');
  return value.replace(/\s+/g, ' ').trim();
}
