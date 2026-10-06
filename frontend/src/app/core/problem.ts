import { HttpErrorResponse } from '@angular/common/http';

interface ProblemBody {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/**
 * Turns an HTTP error into one English sentence for the form.
 * @param error The value caught from a request.
 * @returns A message safe to show on the page.
 * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
 */
export function readProblem(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'The request could not be completed.';
  }

  const body = error.error as ProblemBody | null;
  if (body && typeof body === 'object') {
    const messages = body.errors ? Object.values(body.errors).flat().filter(Boolean) : [];
    if (messages.length > 0) {
      return messages.join(' ');
    }

    if (body.detail) {
      return body.detail;
    }

    if (body.title) {
      return body.title;
    }
  }

  if (error.status === 0) {
    return 'The server could not be reached.';
  }

  return 'The request could not be completed.';
}
