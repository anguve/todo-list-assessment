import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
})
export class App {
  protected readonly auth = inject(AuthService);

  /**
   * Signs the current user out from the header.
   * @author Andres Gutierrez Velez <sr.willardkraft@gmail.com>
   */
  protected logout(): void {
    this.auth.logout();
  }
}
