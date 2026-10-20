/** Modelos tipados del contrato de la API (.NET 9 — Monrys Capital).
 *  Los campos marcados como opcionales pueden variar según el backend:
 *  el acceso está centralizado en core/services/* para ajustarlos en un solo lugar. */

// ---------- Auth / Usuarios ----------
export interface Usuario {
  id: number;
  nombre: string;
  tipo?: string;
  roles?: string[];
  usuario?: string;
  activo?: boolean;
  clave?: string;
}

export interface LoginResponse {
  token: string;
  expira: string;
  usuario: Usuario;
}

export interface Permiso {
  id?: number;
  nombre?: string;
  clave?: string;
  descripcion?: string;
}

export interface Rol {
  id: number;
  nombre: string;
  permisos?: (string | Permiso)[];
}

// ---------- Clientes ----------
export type TipoPersona = 'N' | 'J';

export interface Cliente {
  /** Id del cliente, formato VFP "0000000001". */
  id: string;
  persona: TipoPersona | string;
  /** Nombre completo computado por la API (listado/detalle). */
  nombre?: string;
  razonSocial?: string;
  nombres?: string;
  apellidos?: string;
  rif?: string;
  cedula?: string;
  direccion?: string;
  telefonos?: string;
  email?: string;
  observaciones?: string;
  activo: boolean;
}

export interface ClientePage {
  items: Cliente[];
  total: number;
}

export interface CuentaCliente {
  id: number;
  clienteId?: string;
  bancoId: string;
  banco?: string;
  numero: string;
  tipo?: string;
  monedaId?: string;
  moneda?: string;
  descripcion?: string;
  saldo?: number;
  activo?: boolean;
}

// ---------- Monedas / Tasas ----------
export interface Moneda {
  /** Id de la moneda, formato VFP "0001". */
  id: string;
  descripcion: string;
  principal?: boolean;
  nacional?: boolean;
  simbolo?: string;
  activa?: boolean;
}

export interface Tasa {
  id?: number;
  fecha?: string;
  tasaActiva: number;
  tasaCompra: number;
  tasaVenta: number;
}

export interface TasaCalendarioItem {
  id?: number;
  fecha: string;
  nombreDia?: string;
  /** Campo real del calendario histórico (API: CalendTasaDto.tasa). */
  tasa?: number;
  tasaCompra?: number;
  tasaVenta?: number;
}

// ---------- Bancos ----------
export interface Banco {
  /** Id del banco, formato VFP "00001". */
  id: string;
  nombre: string;
  /** 1 = nacional, 2 = internacional (VFP BANCOS.NACIONAL). */
  nacional?: number;
  activo?: boolean;
  saldo?: number;
  cuentas?: CuentaBanco[];
}

export interface CuentaBanco {
  /** Id numérico de la cuenta (serial). */
  id: number;
  bancoId?: string;
  /** Número de cuenta tal como está en el banco (VFP CTAS.NUMECTAS). */
  numeroCuenta: string;
  tipo?: number | null;
  cuentaContable?: string | null;
  debitos?: number;
  creditos?: number;
  saldo?: number;
  activa?: boolean;
}

// ---------- Operaciones (compras / ventas) ----------
export type TipoOperacion = 'compra' | 'venta';
export type EstadoOperacion = 'pendiente' | 'completada' | 'anulada' | string;

export interface Operacion {
  id: number;
  numero?: string;
  tipo?: TipoOperacion | string;
  /** Id del cliente, formato VFP "0000000001". */
  clienteId: string;
  cliente?: string;
  fecha: string;
  tasa: number;
  cantidad: number;
  montoBs?: number;
  monto?: number;
  saldo?: number;
  estado?: EstadoOperacion;
  observaciones?: string;
}

export interface OperacionPage {
  items: Operacion[];
  total: number;
  pagina?: number;
  tamano?: number;
}

export interface Cuota {
  id: number;
  fecha: string;
  cantidad?: number;
  monto?: number;
  montoBs?: number;
  tipo?: string;
  deCaja?: boolean;
  bancoId?: string;
  banco?: string;
  bancoDebitoId?: string;
  bancoCreditoId?: string;
  bancoDebito?: string;
  bancoCredito?: string;
  tipoPago?: string;
  estado?: string;
  descripcion?: string;
}

export interface OperacionDetalle extends Operacion {
  cuotas?: Cuota[];
  recepciones?: Cuota[];
  pagos?: Cuota[];
}

export interface NuevaOperacion {
  /** Id del cliente, formato VFP "0000000001". */
  clienteId: string;
  fecha: string;
  tasa: number;
  cantidad: number;
  observaciones?: string;
}

// ---------- Caja ----------
export type OrigenMov = 'IN' | 'EG' | string;

export interface CajaMovimiento {
  id: number;
  ejercicio?: number | string;
  origen: OrigenMov;
  fecha: string;
  monto: number;
  descripcion?: string;
  monedaId?: string;
  moneda?: string;
  /** Equivalente en Bs (usa la tasa propia del movimiento). */
  montoBs?: number;
  tasa?: number | null;
  recibo?: string;
  /** Estado crudo de la API: ACTIVO | ANULADO (la UI normaliza a `anulado`). */
  estado?: string;
  cierreFecha?: string | null;
  clienteId?: string;
  cliente?: string;
  /** Id de la codificación (tabla VFP), formato "00001". */
  codiIngresoId?: string;
  codiEgresoId?: string;
  tipo?: string;
  anulado?: boolean;
  usuario?: string;
}

export interface CajaMovimientoPage {
  items: CajaMovimiento[];
  total: number;
}

export interface CodificacionOpcion {
  id: string;
  descripcion: string;
  cuentaContable?: string;
}

export interface Codificaciones {
  ingresos: CodificacionOpcion[];
  egresos: CodificacionOpcion[];
}

export interface NuevoMovimientoCaja {
  origen: OrigenMov;
  fecha: string;
  monto: number;
  descripcion?: string;
  /** Id de la moneda, formato VFP "0001". */
  monedaId: string;
  clienteId?: string | null;
  /** Ids de tabla (string) según CodificacionesDto. */
  codiIngresoId?: string | null;
  codiEgresoId?: string | null;
}

export interface ArqueoDenominacion {
  id: number;
  moneda: string;
  valor: number;
  cantidad: number;
  subtotal: number;
}

export interface Arqueo {
  fecha?: string;
  denominaciones: ArqueoDenominacion[];
  totalesPorMoneda: TotalMoneda[];
}

export interface TotalMoneda {
  moneda?: string;
  descripcion?: string;
  monto?: number;
  total?: number;
  /** Desglose del cuadre (monto nativo de la moneda + equivalente Bs). */
  nacional?: boolean;
  saldoAnterior?: number;
  entradas?: number;
  salidas?: number;
  saldoFinal?: number;
  saldoFinalBs?: number;
}

export interface CierreEstado {
  cerrado: boolean;
  fecha: string;
  total: number;
}

// ---------- Transferencias ----------
export interface Transferencia {
  id: number;
  /** Catálogo VFP: CO, VE, CU, DP, RT, T1..T6. */
  origen?: string;
  /** CR (crédito, entra) o DB (débito, sale) — VFP CTASTRAN.TIPOTRAN. */
  tipoTran?: string;
  fecha: string;
  numeroTran?: string;
  cuentaTran?: string;
  beneficiario?: string;
  monto: number;
  /** Equivalente en bolívares (NULL si la transferencia es en moneda nacional). */
  montoBs?: number | null;
  tasa?: number | null;
  bancoId?: string;
  banco?: string;
  monedaId?: string;
  moneda?: string;
  clienteId?: string | null;
  cliente?: string;
  motivo?: string;
  estado?: string;
  anulado?: boolean;
}

export interface NuevaTransferencia {
  /** Catálogo VFP: DP, RT, T1..T4 (CO/VE/CU se generan solos desde operaciones). */
  origen: string;
  /** CR (crédito, entra) o DB (débito, sale). */
  tipoTran: string;
  fecha: string;
  numeroTran?: string;
  cuentaTran?: string;
  beneficiario: string;
  monto: number;
  bancoId: string;
  monedaId: string;
  clienteId?: string | null;
  motivo?: string;
  tasa?: number;
  usd?: number;
  montoBs?: number;
}

// ---------- Custodios ----------
export interface Custodio {
  /** Id del cliente, formato VFP "0000000001". */
  clienteId: string;
  cliente: string;
  /** Id de la moneda, formato VFP "0001". */
  monedaId: string;
  moneda: string;
  disponible: number;
  disponibleBs: number;
}

/** Movimiento de custodia normalizado en la UI (abonos − retiros). */
export interface CustodioMovimiento {
  id: string | number;
  fecha: string;
  descripcion?: string;
  monedaId?: string;
  /** Abonos − retiros (+entrada / −salida). */
  monto: number;
  tipo: 'entrada' | 'salida';
  /** Saldo disponible DESPUÉS del movimiento (recalculado en la UI). */
  saldo?: number;
}

export interface MovimientoCustodio {
  clienteId: string;
  monedaId: string;
  monto: number;
  descripcion?: string;
}

// ---------- Reportes ----------
export interface ConsolidadoItem {
  moneda: string;
  descripcion?: string;
  total: number;
  totalBs?: number;
  tasa?: number;
}

export interface EstadoGeneralItem {
  cliente: string;
  clienteId?: string;
  comprasBs: number;
  ventasBs: number;
  custodio: number;
  custodioBs: number;
  consolidado: number;
}

export interface CuadreCaja {
  entradas: number;
  salidas: number;
  saldo: number;
  porMoneda: TotalMoneda[];
}

export interface ReporteCaja {
  movimientos: CajaMovimiento[];
  agrupadoPorOrigen: { origen?: string; total?: number; monto?: number }[];
}

export interface EstadoCuentaFiltro {
  desde?: string;
  hasta?: string;
}

export interface EstadoCuentaLinea {
  fecha: string;
  tipo?: string;
  origen?: string;
  descripcion?: string;
  referencia?: string;
  monto: number;
  monedaId?: string;
  moneda?: string;
  /** ENTRADA = valor que entra a la casa · SALIDA = valor que sale. */
  sentido?: string;
  /** Monto convertido a Bs (tasa propia del movimiento). */
  montoBs?: number;
  /** Tasa Bs usada para derivar montoBs; null si la moneda es nacional o el
   *  movimiento ya traía su monto_bs registrado. */
  tasa?: number;
  /** Saldo corrido en Bs hasta esta línea. */
  acumuladoBs?: number;
}

export interface EstadoCuenta {
  lineas: EstadoCuentaLinea[];
  cliente?: string;
  desde?: string;
  hasta?: string;
  tasa?: number;
  totalEntradasBs?: number;
  totalSalidasBs?: number;
  saldoBs?: number;
}

export type TipoComprobante = 'recibo' | 'compra' | 'venta' | 'ingreso' | 'entrega' | 'recepcion';

/** Respuesta flexible de /api/reportes/comprobante/{tipo}/{id} */
export interface Comprobante {
  titulo?: string;
  numero?: string | number;
  fecha?: string;
  cliente?: string;
  rif?: string;
  empresa?: string;
  direccion?: string;
  telefono?: string;
  tasa?: number;
  total?: number;
  lineas?: { descripcion?: string; cantidad?: number; monto?: number; valor?: number }[];
  detalles?: { descripcion?: string; cantidad?: number; monto?: number; valor?: number }[];
  [clave: string]: unknown;
}

// ---------- Configuración / Auditoría ----------
export interface Configuracion {
  id?: number;
  nombreEmpresa?: string;
  rif?: string;
  direccion?: string;
  telefono?: string;
  email?: string;
  mensajePie?: string;
  [clave: string]: unknown;
}

export interface AuditoriaItem {
  id?: number;
  tabla?: string;
  registroId?: number;
  accion?: string;
  fecha?: string;
  usuario?: string;
  datos?: string;
  detalle?: string;
}

// ---------- Utilidades ----------
export interface Page<T> {
  items: T[];
  total: number;
}
