import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Todos } from './todos';

describe('Todos', () => {
  let fixture: ComponentFixture<Todos>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Todos],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Todos);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('shows the empty state after the list loads', async () => {
    httpMock.expectOne('/api/todos').flush([]);
    await render();

    expect(text()).toContain('Nothing written down yet.');
    expect(text()).toContain('0 tasks');
  });

  it('adds a task and then deletes it', async () => {
    httpMock.expectOne('/api/todos').flush([]);
    await render();

    setValue('title', 'Buy coffee');
    setValue('description', 'From the shop');
    submit();
    const created = httpMock.expectOne('/api/todos');
    expect(created.request.method).toBe('POST');
    expect(created.request.body).toEqual({ title: 'Buy coffee', description: 'From the shop' });
    created.flush({
      id: 'task-1',
      title: 'Buy coffee',
      description: 'From the shop',
      createdAt: '2026-10-06T12:00:00Z',
    });
    await render();

    expect(text()).toContain('Buy coffee');
    expect(text()).toContain('From the shop');
    expect(text()).toContain('1 task');
    expect(text()).toContain('6 Oct');

    buttonNamed('Delete').click();
    const deleted = httpMock.expectOne('/api/todos/task-1');
    expect(deleted.request.method).toBe('DELETE');
    deleted.flush(null);
    await render();

    expect(text()).toContain('Nothing written down yet.');
  });

  it('edits a task title and can cancel without saving', async () => {
    httpMock.expectOne('/api/todos').flush([
      {
        id: 'task-1',
        title: 'Buy coffee',
        description: 'From the shop',
        createdAt: '2026-10-06T12:00:00Z',
      },
    ]);
    await render();

    buttonNamed('Edit').click();
    await render();
    setValue('edit-task-1', 'A');
    await render();
    submitEdit();
    await render();
    expect(text()).toContain('Write a task first.');
    httpMock.expectNone({ method: 'PUT' });

    setValue('edit-task-1', 'Buy tea');
    await render();
    submitEdit();
    const updated = httpMock.expectOne('/api/todos/task-1');
    expect(updated.request.method).toBe('PUT');
    expect(updated.request.body).toEqual({ title: 'Buy tea', description: 'From the shop' });
    updated.flush({
      id: 'task-1',
      title: 'Buy tea',
      description: 'From the shop',
      createdAt: '2026-10-06T12:00:00Z',
    });
    await render();
    expect(text()).toContain('Buy tea');
    expect(text()).not.toContain('Buy coffee');

    buttonNamed('Edit').click();
    await render();
    setValue('edit-task-1', 'Buy milk');
    buttonNamed('Cancel').click();
    await render();
    expect(text()).toContain('Buy tea');
    httpMock.expectNone({ method: 'PUT' });
  });

  it('shows a validation error for a short title and does not call the API', async () => {
    httpMock.expectOne('/api/todos').flush([]);
    await render();

    setValue('title', 'A');
    submit();
    await render();

    expect(text()).toContain('Write a task first.');
    httpMock.expectNone({ method: 'POST' });
  });

  it('shows the API message when the list cannot be loaded', async () => {
    httpMock.expectOne('/api/todos').flush(
      { detail: 'The request could not be completed.' },
      { status: 500, statusText: 'Error' },
    );
    await render();

    expect(text()).toContain('The request could not be completed.');
  });

  /**
   * Finds a button by its visible label.
   * @param label The button text.
   * @returns The matching button.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  function buttonNamed(label: string): HTMLButtonElement {
    const button = [...fixture.nativeElement.querySelectorAll('button')].find((entry) =>
      (entry as HTMLButtonElement).textContent?.includes(label),
    ) as HTMLButtonElement | undefined;
    if (!button) {
      throw new Error(`Missing button ${label}`);
    }

    return button;
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
   * Submits the inline edit form.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  function submitEdit(): void {
    const form = fixture.nativeElement.querySelector('li form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
  }

  /**
   * Submits the new-task form.
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
