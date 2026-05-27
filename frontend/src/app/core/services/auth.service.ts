import { Injectable, signal, inject } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { ApiService } from './api.service';
import { LoginResponse } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private router = inject(Router);

  user = signal<{ fullName: string; email: string; role: string } | null>(this.loadUser());

  login(username: string, password: string) {
    return this.api.postPublic<LoginResponse>('auth/login', { username, password }).pipe(
      tap(res => {
        localStorage.setItem('contanexo_token', res.token);
        localStorage.setItem('contanexo_user', JSON.stringify({
          fullName: res.fullName, email: res.email, role: res.role
        }));
        this.user.set({ fullName: res.fullName, email: res.email, role: res.role });
      })
    );
  }

  logout() {
    localStorage.removeItem('contanexo_token');
    localStorage.removeItem('contanexo_user');
    this.user.set(null);
    this.router.navigate(['/login']);
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('contanexo_token');
  }

  private loadUser() {
    const raw = localStorage.getItem('contanexo_user');
    return raw ? JSON.parse(raw) : null;
  }
}
