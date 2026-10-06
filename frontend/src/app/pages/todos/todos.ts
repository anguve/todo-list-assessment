import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { checkField, fieldRules } from '../../core/field-rules';
import { TodoItem } from '../../core/models';
import { readProblem } from '../../core/problem';
import { TodoService } from '../../core/todo.service';

@Component({
  selector: 'app-todos',
  imports: [ReactiveFormsModule],
  templateUrl: './todos.html',
})
export class Todos {
  private readonly todoService = inject(TodoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly items = signal<TodoItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly savingEdit = signal(false);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly editingId = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly composer = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [fieldRules('title')] }),
    description: new FormControl('', { nonNullable: true, validators: [fieldRules('description')] }),
  });

  protected readonly editor = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [fieldRules('title')] }),
    description: new FormControl('', { nonNullable: true, validators: [fieldRules('description')] }),
  });

  /**
   * Loads the task list as soon as the page opens.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  constructor() {
    this.load();
  }

  /**
   * Checks the title and description, then appends the stored task to the list.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected add(): void {
    this.errorMessage.set(null);
    const title = checkField('title', this.composer.controls.title.getRawValue());
    const description = checkField('description', this.composer.controls.description.getRawValue());
    if (!title.ok) {
      this.composer.controls.title.markAsTouched();
    }

    if (!description.ok) {
      this.composer.controls.description.markAsTouched();
    }

    if (!title.ok || !description.ok) {
      return;
    }

    this.saving.set(true);
    this.todoService
      .add(title.value, description.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (item) => {
          this.items.update((items) => [...items, item]);
          this.composer.reset();
          this.saving.set(false);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.errorMessage.set(readProblem(error));
        },
      });
  }

  /**
   * Opens the title and description for one task.
   * @param item The task to edit.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected beginEdit(item: TodoItem): void {
    this.errorMessage.set(null);
    this.editingId.set(item.id);
    this.editor.setValue({ title: item.title, description: item.description });
    this.editor.markAsUntouched();
  }

  /**
   * Closes the title field without saving.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected cancelEdit(): void {
    this.editingId.set(null);
    this.savingEdit.set(false);
  }

  /**
   * Checks the new title and description, then stores them for the signed-in user.
   * @param item The task being edited.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected saveEdit(item: TodoItem): void {
    this.errorMessage.set(null);
    const title = checkField('title', this.editor.controls.title.getRawValue());
    const description = checkField('description', this.editor.controls.description.getRawValue());
    if (!title.ok) {
      this.editor.controls.title.markAsTouched();
    }

    if (!description.ok) {
      this.editor.controls.description.markAsTouched();
    }

    if (!title.ok || !description.ok) {
      return;
    }

    this.savingEdit.set(true);
    this.todoService
      .update(item.id, title.value, description.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.items.update((items) =>
            items.map((entry) => (entry.id === updated.id ? updated : entry)),
          );
          this.editingId.set(null);
          this.savingEdit.set(false);
        },
        error: (error: unknown) => {
          this.savingEdit.set(false);
          this.errorMessage.set(readProblem(error));
        },
      });
  }

  /**
   * Deletes one task and removes it from the list.
   * @param item The task to delete.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected remove(item: TodoItem): void {
    this.errorMessage.set(null);
    this.deletingId.set(item.id);
    this.todoService
      .remove(item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.items.update((items) => items.filter((entry) => entry.id !== item.id));
          this.deletingId.set(null);
        },
        error: (error: unknown) => {
          this.deletingId.set(null);
          this.errorMessage.set(readProblem(error));
        },
      });
  }

  /**
   * Formats a creation time for an Australian reader.
   * @param value The ISO timestamp from the API.
   * @returns A short date such as "6 Oct".
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected formatWhen(value: string): string {
    return new Intl.DateTimeFormat('en-AU', { day: 'numeric', month: 'short' }).format(
      new Date(value),
    );
  }

  /**
   * Loads the signed-in user's tasks and records a failure on the page.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  private load(): void {
    this.todoService
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.items.set(items);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.errorMessage.set(readProblem(error));
        },
      });
  }
}
