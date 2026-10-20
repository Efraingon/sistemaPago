import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse, Usuario } from './models';
import { ToastService } from '../shared/toast/toast.service';

const TOKEN_KEY = 'monrys_token';
const USER_KEY = 'monrys_usuario';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly base = environment.apiUrl;
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  private readonly usuarioSubject = new BehaviorSubject<Usuario | null>(this.leerUsuario());
  readonly usuario$ = this.usuarioSubject.asObservable();

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get usuario(): Usuario | null {
    return this.usuarioSubject.value;
  }

  get autenticado(): boolean {
    return !!this.token;
  }

  get roles(): string[] {
    return this.usuarioSubject.value?.roles ?? [];
  }

  login(usuario: string, clave: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/api/auth/login`, { usuario, clave }).pipe(
      tap((res) => {
        localStorage.setItem(TOKEN_KEY, res.token);
        localStorage.setItem(USER_KEY, JSON.stringify(res.usuario));
        this.usuarioSubject.next(res.usuario);
      })
    );
  }

  refrescarSesion(): Observable<Usuario> {
    return this.http.get<Usuario>(`${this.base}/api/auth/me`).pipe(
      tap((u) => {
        if (u) {
          localStorage.setItem(USER_KEY, JSON.stringify(u));
          this.usuarioSubject.next(u);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.usuarioSubject.next(null);
    this.router.navigate(['/login']);
  }

  /** Salió por 401: avisa una sola vez. */
  expirarSesion(): void {
    if (!this.token) return;
    this.toast.info('La sesión expiró. Inicie sesión nuevamente.');
    this.logout();
  }

  private leerUsuario(): Usuario | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as Usuario) : null;
    } catch {
      return null;
    }
  }
}
