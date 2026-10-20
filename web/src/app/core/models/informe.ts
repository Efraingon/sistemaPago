// ---------------------------------------------------------------------------
// MOTOR GENÉRICO DE INFORMES (Centro de Reportes)
// Contrato de GET /api/informes (catálogo) y GET /api/informes/{id} (resultado).
// ---------------------------------------------------------------------------

export interface InformeCatalogo {
  familias: InformeFamilia[];
}

export interface InformeFamilia {
  codigo: string;
  nombre: string;
  descripcion?: string;
  informes: InformeDef[];
}

export interface InformeDef {
  id: string;
  nombre: string;
  descripcion?: string;
  /** Archivo .FRX original de Visual FoxPro (trazabilidad). */
  origenVfp?: string;
  /** Ruta de la página propia cuando el informe tiene pantalla dedicada. */
  rutaPantalla?: string;
  parametros: InformeParametro[];
}

export interface InformeParametro {
  clave: string;
  etiqueta: string;
  /** fecha | texto | cliente | banco | select */
  tipo: string;
  requerido?: boolean;
  defecto?: string;
  opciones?: { valor: string; etiqueta: string }[];
}

export interface Informe {
  id: string;
  titulo: string;
  subtitulo?: string;
  periodo?: string;
  generado: string;
  empresa: InformeEmpresa;
  secciones: InformeSeccion[];
  notas: { texto: string }[];
}

export interface InformeEmpresa {
  nombre: string;
  rif?: string;
  direccion?: string;
  telefono?: string;
}

export interface InformeSeccion {
  nombre?: string;
  columnas: InformeColumna[];
  filas: Record<string, unknown>[];
  totales?: Record<string, unknown> | null;
}

export interface InformeColumna {
  clave: string;
  etiqueta: string;
  /** texto | numero | moneda | fecha */
  tipo: string;
  totalizable?: boolean;
}
