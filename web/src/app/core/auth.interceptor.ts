import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** Adjunta Bearer token y redirige a /login ante 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;

  let peticion = req;
  if (token && !req.url.includes('/api/auth/login')) {
    peticion = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  return next(peticion).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && !req.url.includes('/api/auth/login')) {
        auth.expirarSesion();
      }
      return throwError(() => err);
    })
  );
};
