/** Utilidades de exportación e impresión de informes. */

type Celda = string | number | null | undefined;

/** Descarga un CSV con BOM UTF-8 y separador «;» (abre directo en Excel es-VE). */
export function exportarCsv(nombre: string, encabezados: string[], filas: Celda[][]): void {
  const esc = (v: Celda): string => `"${String(v ?? '').replace(/"/g, '""')}"`;
  const csv = '\uFEFF' + [encabezados, ...filas].map((f) => f.map(esc).join(';')).join('\r\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${nombre}.csv`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

/** Abre el diálogo de impresión del navegador (o guardar como PDF). */
export function imprimir(): void {
  window.print();
}

/** Fecha ISO o 'yyyy-MM-dd' → dd/MM/yyyy (para exportar). */
export function fechaTexto(valor: unknown): string {
  if (valor === null || valor === undefined || valor === '') return '';
  const s = String(valor);
  const p = /^(\d{4})-(\d{2})-(\d{2})/.exec(s);
  return p ? `${p[3]}/${p[2]}/${p[1]}` : s;
}

/** Número → 1.234,56 (es-VE) para exportar. */
export function numeroTexto(valor: unknown, decimales = 2): string {
  const n = typeof valor === 'number' ? valor : Number(valor);
  if (valor === null || valor === undefined || valor === '' || isNaN(n)) return String(valor ?? '');
  return n.toLocaleString('es-VE', { minimumFractionDigits: decimales, maximumFractionDigits: decimales });
}
