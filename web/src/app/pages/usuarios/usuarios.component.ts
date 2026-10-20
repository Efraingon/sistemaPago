import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UsuarioService } from '../../core/services/usuario.service';
import { ToastService } from '../../shared/toast/toast.service';
import { Rol, Usuario } from '../../core/models';
import { ModalComponent } from '../../shared/modal/modal.component';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [ReactiveFormsModule, ModalComponent, IconComponent],
  template: `
    <div class="page-header">
      <div>
        <p class="eyebrow">Administración</p>
        <h1 class="page-title">Usuarios y roles</h1>
        <p class="page-sub">Alta, edición y asignación de roles por usuario.</p>
      </div>
      <button class="btn btn-primary" type="button" (click)="nuevo()">
        <app-icon name="plus" [size]="16" /> Nuevo usuario
      </button>
    </div>

    <div class="table-wrap">
      @if (cargando) {
        <div class="loading-panel"><span class="spinner"></span> Cargando usuarios…</div>
      } @else if (!usuarios.length) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="userCog" [size]="36" /></span>
          <strong>Sin usuarios</strong>
          <span class="small">Registre el primer usuario del sistema.</span>
        </div>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nombre</th>
              <th>Tipo</th>
              <th>Roles</th>
              <th>Estado</th>
              <th class="text-right">Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (u of usuarios; track u.id) {
              <tr>
                <td class="mono text-muted">{{ u.id }}</td>
                <td>{{ u.nombre || u.usuario || '—' }}</td>
                <td>{{ u.tipo || '—' }}</td>
                <td>
                  @if (u.roles?.length) {
                    @for (r of u.roles; track r) {
                      <span class="badge badge-gold" style="margin-inline-end: 5px">{{ r }}</span>
                    }
                  } @else {
                    <span class="text-muted small">Sin roles</span>
                  }
                </td>
                <td>
                  <span class="badge" [class.badge-success]="u.activo !== false" [class.badge-danger]="u.activo === false">
                    {{ u.activo !== false ? 'Activo' : 'Inactivo' }}
                  </span>
                </td>
                <td class="text-right" style="white-space: nowrap">
                  <button class="icon-btn" type="button" (click)="editar(u)" title="Editar" aria-label="Editar usuario">
                    <app-icon name="edit" [size]="15" />
                  </button>
                  <button class="icon-btn" type="button" (click)="roles(u)" title="Asignar roles" aria-label="Asignar roles">
                    <app-icon name="key" [size]="15" />
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>

    <!-- Modal usuario -->
    <app-modal [visible]="modalUsuario" [title]="editando ? 'Editar usuario' : 'Nuevo usuario'" (closed)="modalUsuario = false">
      <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
        <div class="form-group mb-3">
          <label class="form-label" for="nombre">Nombre completo <span class="req">*</span></label>
          <input id="nombre" class="input" formControlName="nombre" [class.is-invalid]="fallo('nombre')" placeholder="Nombre y apellido" />
          @if (fallo('nombre')) {
            <span class="form-error"><app-icon name="alert" [size]="12" /> Nombre requerido</span>
          }
        </div>

        <div class="form-row-2 form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="usuario">Usuario <span class="req">*</span></label>
            <input id="usuario" class="input mono" formControlName="usuario" [class.is-invalid]="fallo('usuario')" autocomplete="off" placeholder="usuario.login" />
            @if (fallo('usuario')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Usuario requerido</span>
            }
          </div>
          <div class="form-group">
            <label class="form-label" for="tipo">Tipo</label>
            <select id="tipo" class="select" formControlName="tipo">
              <option value="">—</option>
              <option value="admin">Administrador</option>
              <option value="operador">Operador</option>
              <option value="cajero">Cajero</option>
              <option value="consulta">Consulta</option>
            </select>
          </div>
        </div>

        <div class="form-row-2 form-row mb-3">
          <div class="form-group">
            <label class="form-label" for="clave">
              Clave {{ editando ? '(dejar vacío para conservar)' : '' }} @if (!editando) {<span class="req">*</span>}
            </label>
            <input id="clave" class="input" type="password" formControlName="clave" autocomplete="new-password" [class.is-invalid]="fallo('clave')" placeholder="••••••••" />
            @if (fallo('clave')) {
              <span class="form-error"><app-icon name="alert" [size]="12" /> Mínimo 6 caracteres</span>
            }
          </div>
          <div class="form-group">
            <span class="form-label">Estado</span>
            <label class="check mt-1" style="margin-top: 10px">
              <input type="checkbox" formControlName="activo" />
              Usuario activo
            </label>
          </div>
        </div>

        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalUsuario = false">Cancelar</button>
          <button class="btn btn-primary" type="submit" [disabled]="guardando">
            @if (guardando) {
              <span class="spinner" style="width:16px;height:16px"></span> Guardando…
            } @else {
              Guardar
            }
          </button>
        </div>
      </form>
    </app-modal>

    <!-- Modal roles -->
    <app-modal [visible]="modalRoles" [title]="'Roles de ' + (rolesUsuario?.nombre || '')" (closed)="modalRoles = false">
      @if (cargandoRoles) {
        <div class="loading-panel"><span class="spinner"></span> Cargando roles…</div>
      } @else if (!catalogo.length) {
        <div class="empty-state">
          <span class="empty-ico"><app-icon name="key" [size]="32" /></span>
          <strong>Sin catálogo de roles</strong>
          <span class="small">El backend no devolvió roles disponibles.</span>
        </div>
      } @else {
        <div class="roles-list mb-4">
          @for (rol of catalogo; track rol.id) {
            <label class="rol-item">
              <input type="checkbox" [checked]="seleccionados.has(rol.id)" (change)="toggleRol(rol.id)" />
              <span class="rol-meta">
                <strong>{{ rol.nombre }}</strong>
                @if (rol.permisos?.length) {
                  <small class="text-muted">{{ permisosTexto(rol) }}</small>
                }
              </span>
            </label>
          }
        </div>

        <div class="row gap-2" style="justify-content: flex-end">
          <button class="btn btn-ghost" type="button" (click)="modalRoles = false">Cancelar</button>
          <button class="btn btn-primary" type="button" [disabled]="guardandoRoles" (click)="guardarRoles()">
            @if (guardandoRoles) {
              <span class="spinner" style="width:16px;height:16px"></span> Guardando…
            } @else {
              Asignar roles
            }
          </button>
        </div>
      }
    </app-modal>
  `,
  styles: [
    `
      .icon-btn + .icon-btn {
        margin-inline-start: 6px;
      }
      .roles-list {
        display: flex;
        flex-direction: column;
        gap: 8px;
      }
      .rol-item {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 12px 14px;
        border: 1px solid var(--color-border);
        border-radius: 9px;
        cursor: pointer;
        transition: border-color 200ms ease, background-color 200ms ease;
      }
      .rol-item:hover {
        border-color: rgba(245, 158, 11, 0.45);
        background: rgba(245, 158, 11, 0.05);
      }
      .rol-item input {
        appearance: none;
        width: 17px;
        height: 17px;
        border: 1px solid var(--color-border);
        border-radius: 4px;
        background: var(--color-background-deep);
        cursor: pointer;
        display: grid;
        place-content: center;
        flex: 0 0 auto;
        transition: background-color 200ms ease, border-color 200ms ease;
      }
      .rol-item input::after {
        content: '';
        width: 9px;
        height: 9px;
        transform: scale(0);
        transition: transform 150ms ease;
        background: var(--color-on-primary);
        clip-path: polygon(14% 44%, 0 65%, 50% 100%, 100% 16%, 80% 0%, 43% 62%);
      }
      .rol-item input:checked {
        background: var(--color-primary);
        border-color: var(--color-primary);
      }
      .rol-item input:checked::after {
        transform: scale(1);
      }
      .rol-item input:focus-visible {
        outline: 2px solid var(--color-ring);
        outline-offset: 2px;
      }
      .rol-meta {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }
      .rol-meta strong {
        font-size: 14px;
        font-weight: 600;
      }
      .rol-meta small {
        font-size: 12px;
      }
    `,
  ],
})
export class UsuariosComponent implements OnInit {
  usuarios: Usuario[] = [];
  catalogo: Rol[] = [];
  cargando = false;
  cargandoRoles = false;

  modalUsuario = false;
  editando: Usuario | null = null;
  guardando = false;

  modalRoles = false;
  rolesUsuario: Usuario | null = null;
  seleccionados = new Set<number>();
  guardandoRoles = false;

  private readonly service = inject(UsuarioService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    nombre: ['', Validators.required],
    usuario: ['', Validators.required],
    tipo: [''],
    clave: ['', [Validators.minLength(6)]],
    activo: [true],
  });

  ngOnInit(): void {
    this.cargar();
    this.service.roles().subscribe({
      next: (r) => (this.catalogo = Array.isArray(r) ? r : []),
      error: () => (this.catalogo = []),
    });
  }

  cargar(): void {
    this.cargando = true;
    this.service.listar().subscribe({
      next: (lista) => {
        this.usuarios = Array.isArray(lista) ? lista : ((lista as any)?.items ?? []);
        this.cargando = false;
      },
      error: () => {
        this.usuarios = [];
        this.cargando = false;
      },
    });
  }

  fallo(campo: string): boolean {
    const c = this.form.get(campo);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  nuevo(): void {
    this.editando = null;
    this.form.reset({ nombre: '', usuario: '', tipo: '', clave: '', activo: true });
    this.modalUsuario = true;
  }

  editar(u: Usuario): void {
    this.editando = u;
    this.form.reset({
      nombre: u.nombre ?? '',
      usuario: u.usuario ?? '',
      tipo: u.tipo ?? '',
      clave: '',
      activo: u.activo !== false,
    });
    this.modalUsuario = true;
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    if (!this.editando && !v.clave) {
      this.form.controls.clave.setErrors({ requerido: true });
      return;
    }

    const body: Partial<Usuario> = {
      nombre: v.nombre,
      usuario: v.usuario,
      tipo: v.tipo,
      activo: v.activo,
    };
    if (v.clave) body.clave = v.clave;

    this.guardando = true;
    const req = this.editando ? this.service.actualizar(this.editando.id, body) : this.service.crear(body);
    req.subscribe({
      next: () => {
        this.guardando = false;
        this.modalUsuario = false;
        this.toast.success(this.editando ? 'Usuario actualizado.' : 'Usuario creado.');
        this.editando = null;
        this.cargar();
      },
      error: () => (this.guardando = false),
    });
  }

  // ---------- Roles ----------
  roles(u: Usuario): void {
    this.rolesUsuario = u;
    this.seleccionados = new Set<number>();
    this.cargandoRoles = true;
    this.modalRoles = true;

    const cargarCatalogo = this.catalogo.length
      ? this.service.roles()
      : this.service.roles();

    cargarCatalogo.subscribe({
      next: (r) => {
        this.catalogo = Array.isArray(r) ? r : [];
        this.seleccionados = new Set(
          this.catalogo
            .filter((rol) => (u.roles ?? []).some((nombre) => this.coincide(nombre, rol)))
            .map((rol) => rol.id)
        );
        this.cargandoRoles = false;
      },
      error: () => {
        this.cargandoRoles = false;
      },
    });
  }

  private coincide(nombreOClaude: string, rol: Rol): boolean {
    const n = String(nombreOClaude ?? '').toLowerCase().trim();
    return String(rol.nombre ?? '').toLowerCase().trim() === n || String(rol.id) === n;
  }

  toggleRol(id: number): void {
    if (this.seleccionados.has(id)) this.seleccionados.delete(id);
    else this.seleccionados.add(id);
  }

  permisosTexto(rol: Rol): string {
    const permisos = (rol.permisos ?? []).map((p) => (typeof p === 'string' ? p : p?.clave || p?.nombre || '')).filter(Boolean);
    if (!permisos.length) return 'Sin permisos detallados';
    return permisos.slice(0, 4).join(' · ') + (permisos.length > 4 ? ` +${permisos.length - 4}` : '');
  }

  guardarRoles(): void {
    if (!this.rolesUsuario) return;
    this.guardandoRoles = true;
    this.service.asignarRoles(this.rolesUsuario.id, [...this.seleccionados]).subscribe({
      next: () => {
        this.guardandoRoles = false;
        this.modalRoles = false;
        this.toast.success('Roles actualizados.');
        this.cargar();
      },
      error: () => (this.guardandoRoles = false),
    });
  }
}
