import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TodoItem } from './models';

@Injectable({ providedIn: 'root' })
export class TodoService {
  private readonly http = inject(HttpClient);

  /**
   * Loads the signed-in user's tasks.
   * @returns The task list.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  list(): Observable<TodoItem[]> {
    return this.http.get<TodoItem[]>('/api/todos');
  }

  /**
   * Creates one task. The API takes the owner from the token.
   * @param title The cleaned title.
   * @param description The cleaned description.
   * @returns The stored task.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  add(title: string, description: string): Observable<TodoItem> {
    return this.http.post<TodoItem>('/api/todos', { title, description });
  }

  /**
   * Replaces the title and description of one of the signed-in user's tasks.
   * @param id The task id.
   * @param title The cleaned title.
   * @param description The cleaned description.
   * @returns The stored task.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  update(id: string, title: string, description: string): Observable<TodoItem> {
    return this.http.put<TodoItem>(`/api/todos/${id}`, { title, description });
  }

  /**
   * Deletes one of the signed-in user's tasks.
   * @param id The task id.
   * @returns An empty response when the delete succeeds.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  remove(id: string): Observable<void> {
    return this.http.delete<void>(`/api/todos/${id}`);
  }
}
