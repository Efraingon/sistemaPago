import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { tap } from 'rxjs/operators';
import { Cliente } from '../../core/models';
import { ClienteService } from '../../core/services/cliente.service';

/** Autocomplete de clientes (carga perezosa + filtro local).
 *  Uso: <app-cliente-autocomplete [cliente]="sel" (clienteChange)="sel = $event" /> */
@Component({
  selector: 'app-cliente-autocomplete',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="autocomplete">
      <input
        class="input"
        type="text"
        role="combobox"
        [attr.aria-expanded]="abierto"
        aria-autocomplete="list"
        [class.is-invalid]="invalido"
        [placeholder]="placeholder"
        [(ngModel)]="texto"
        (ngModelChange)="alEscribir($event)"
        (focus)="abrir()"
        (blur)="cerrarLuego()"
        [disabled]="disabled"
        autocomplete="off"
      />
      @if (abierto && filtrados.length) {
        <ul class="autocomplete-list" role="listbox">
          @for (c of filtrados; track c.id) {
            <li (mousedown)="seleccionar(c)" role="option">
              <span>{{ nombreDe(c) }}</span>
              <span class="rif">{{ c.rif || c.cedula || '—' }}</span>
            </li>
          }
        </ul>
      }
    </div>
  `,
})
export class ClienteAutocompleteComponent {
  @Input() disabled = false;
  @Input() invalido = false;
  @Input() placeholder = 'Buscar cliente por nombre o RIF…';

  @Input()
  set cliente(valor: Cliente | null | undefined) {
    this._cliente = valor ?? null;
    this.texto = this._cliente ? this.nombreDe(this._cliente) : '';
  }
  get cliente(): Cliente | null {
    return this._cliente;
  }

  @Output() clienteChange = new EventEmitter<Cliente | null>();

  _cliente: Cliente | null = null;
  texto = '';
  abierto = false;
  filtrados: Cliente[] = [];

  private cargados: Cliente[] = [];
  private timer: ReturnType<typeof setTimeout> | null = null;
  private readonly service = inject(ClienteService);

  nombreDe(c: Cliente): string {
    if (c.razonSocial) return c.razonSocial;
    const persona = `${c.nombres ?? ''} ${c.apellidos ?? ''}`.trim();
    return persona || `Cliente #${c.id}`;
  }

  abrir(): void {
    this.abierto = true;
    if (!this.cargados.length) this.cargar('');
  }

  cerrarLuego(): void {
    setTimeout(() => {
      this.abierto = false;
      if (!this._cliente) this.texto = '';
    }, 160);
  }

  alEscribir(valor: string): void {
    this._cliente = null;
    this.clienteChange.emit(null);
    this.abierto = true;
    if (this.timer) clearTimeout(this.timer);
    this.timer = setTimeout(() => this.cargar(valor), 250);
  }

  seleccionar(c: Cliente): void {
    this._cliente = c;
    this.texto = this.nombreDe(c);
    this.abierto = false;
    this.clienteChange.emit(c);
  }

  private cargar(buscar: string): void {
    this.service.listar({ pagina: 1, tamano: 20, buscar }).pipe(
      tap((page) => {
        this.cargados = page?.items ?? [];
        this.filtrados = this.cargados;
      })
    ).subscribe({ error: () => (this.filtrados = []) });
  }
}
