import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, IconComponent],
  template: `
    <div class="login-page">
      <div class="grid-bg" aria-hidden="true"></div>

      <main class="login-card" role="main">
        <a class="back" routerLink="/" aria-label="Volver al inicio">
          <app-icon name="chevronLeft" [size]="15" />
          Volver al sitio
        </a>

        <div class="brand">
          <svg viewBox="0 0 32 32" width="40" height="40" aria-hidden="true">
            <rect width="32" height="32" rx="8" fill="#0F172A" stroke="rgba(245,158,11,.45)" />
            <path d="M7 23V9h4l5 8 5-8h4v14h-4v-8l-5 8-5-8v8z" fill="#F59E0B" />
          </svg>
          <div>
            <strong>Monrys Capital</strong>
            <span class="eyebrow">Sistema de pagos</span>
          </div>
        </div>

        <h1>Iniciar sesión</h1>
        <p class="sub">Acceso restringido a usuarios autorizados.</p>

        <form class="form" [formGroup]="form" (ngSubmit)="entrar()" novalidate>
          <div class="form-group">
            <label class="form-label" for="usuario">Usuario <span class="req">*</span></label>
            <input
              id="usuario"
              class="input"
              type="text"
              formControlName="usuario"
              autocomplete="username"
              placeholder="Su usuario"
              [class.is-invalid]="mostrarError('usuario')"
            />
            @if (mostrarError('usuario')) {
              <span class="form-error">
                <app-icon name="alert" [size]="13" /> Requerido
              </span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="clave">Clave <span class="req">*</span></label>
            <input
              id="clave"
              class="input"
              type="password"
              formControlName="clave"
              autocomplete="current-password"
              placeholder="Su clave"
              [class.is-invalid]="mostrarError('clave')"
            />
            @if (mostrarError('clave')) {
              <span class="form-error">
                <app-icon name="alert" [size]="13" /> Requerida
              </span>
            }
          </div>

          @if (error) {
            <div class="alerta" role="alert">
              <app-icon name="alert" [size]="15" />
              {{ error }}
            </div>
          }

          <button class="btn btn-primary btn-block btn-lg" type="submit" [disabled]="cargando">
            @if (cargando) {
              <span class="spinner" aria-hidden="true"></span>
              Verificando…
            } @else {
              Entrar
            }
          </button>
        </form>

        <div class="demo">
          <span class="demo-tag">Modo demo</span>
          <span class="mono">ADMIN / admin123</span>
        </div>
      </main>
    </div>
  `,
  styles: [
    `
      .login-page {
        min-height: 100vh;
        display: grid;
        place-items: center;
        padding: 40px 16px;
        background: linear-gradient(180deg, #0f172a, #070b16);
        position: relative;
      }

      .grid-bg {
        position: absolute;
        inset: 0;
        background-image:
          linear-gradient(var(--grid-line) 1px, transparent 1px),
          linear-gradient(90deg, var(--grid-line) 1px, transparent 1px);
        background-size: 56px 56px;
        mask-image: radial-gradient(ellipse 70% 60% at 50% 40%, #000 20%, transparent 70%);
        pointer-events: none;
      }

      .login-card {
        position: relative;
        width: min(430px, 100%);
        background: var(--color-card);
        border: 1px solid var(--color-border);
        border-radius: 16px;
        padding: 34px 32px 28px;
        box-shadow: var(--shadow-xl);
      }

      .login-card::before {
        content: '';
        position: absolute;
        inset-inline: 32px;
        top: 0;
        height: 2px;
        background: linear-gradient(90deg, transparent, var(--color-primary), transparent);
      }

      .back {
        display: inline-flex;
        align-items: center;
        gap: 5px;
        font-size: 12.5px;
        color: var(--color-muted-foreground);
        cursor: pointer;
        margin-bottom: 22px;
        transition: color 200ms ease;
      }

      .back:hover {
        color: var(--color-primary);
      }

      .brand {
        display: flex;
        align-items: center;
        gap: 13px;
        margin-bottom: 24px;
      }

      .brand strong {
        display: block;
        font-size: 17px;
        font-weight: 700;
        letter-spacing: -0.01em;
        color: var(--color-foreground);
      }

      .brand .eyebrow {
        font-size: 10px;
      }

      h1 {
        font-size: 23px;
        font-weight: 600;
        letter-spacing: -0.02em;
      }

      .sub {
        color: var(--color-muted-foreground);
        font-size: 13.5px;
        margin-top: 5px;
        margin-bottom: 24px;
      }

      .form {
        display: flex;
        flex-direction: column;
        gap: 17px;
      }

      .alerta {
        display: flex;
        align-items: center;
        gap: 9px;
        padding: 10px 13px;
        border-radius: 8px;
        background: rgba(239, 68, 68, 0.12);
        border: 1px solid rgba(239, 68, 68, 0.45);
        color: #fca5a5;
        font-size: 13px;
      }

      .demo {
        margin-top: 22px;
        padding-top: 18px;
        border-top: 1px dashed var(--color-border);
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        flex-wrap: wrap;
      }

      .demo-tag {
        font-size: 10.5px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.14em;
        color: var(--color-accent);
        background: rgba(139, 92, 246, 0.13);
        border: 1px solid rgba(139, 92, 246, 0.4);
        border-radius: 999px;
        padding: 3px 10px;
      }

      .demo .mono {
        font-size: 13px;
        color: var(--color-primary);
        letter-spacing: 0.04em;
      }

      @media (max-width: 375px) {
        .login-card {
          padding: 26px 20px 22px;
        }
      }
    `,
  ],
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  cargando = false;
  error = '';
  private enviado = false;

  readonly form = this.fb.nonNullable.group({
    usuario: ['', Validators.required],
    clave: ['', Validators.required],
  });

  ngOnInit(): void {
    if (this.auth.autenticado) {
      this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard');
    }
  }

  mostrarError(campo: 'usuario' | 'clave'): boolean {
    const control = this.form.controls[campo];
    return this.enviado && control.invalid;
  }

  entrar(): void {
    this.enviado = true;
    this.error = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.cargando = true;
    const { usuario, clave } = this.form.getRawValue();
    this.auth.login(usuario, clave).subscribe({
      next: () => {
        this.cargando = false;
        const destino = this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';
        this.router.navigateByUrl(destino);
      },
      error: (err) => {
        this.cargando = false;
        if (err?.status === 401 || err?.status === 400) {
          this.error = 'Usuario o clave incorrectos.';
        } else if (err?.status === 0) {
          this.error = 'No se pudo contactar al servidor.';
        } else {
          this.error = 'No se pudo iniciar sesión. Intente de nuevo.';
        }
      },
    });
  }
}
