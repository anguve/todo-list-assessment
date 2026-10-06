import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'todos' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login').then((module) => module.Login),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/register/register').then((module) => module.Register),
  },
  {
    path: 'todos',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/todos/todos').then((module) => module.Todos),
  },
  { path: '**', redirectTo: 'todos' },
];
