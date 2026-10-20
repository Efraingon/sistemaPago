import { Pipe, PipeTransform } from '@angular/core';

/** Fecha → dd/MM/yyyy (acepta ISO, 'yyyy-MM-dd', Date o timestamp). */
@Pipe({ name: 'fecha', standalone: true })
export class FechaPipe implements PipeTransform {
  transform(valor: string | Date | number | null | undefined): string {
    if (valor === null || valor === undefined || valor === '') return '—';
    const partes = typeof valor === 'string' ? /^(\d{4})-(\d{2})-(\d{2})/.exec(valor) : null;
    let fecha: Date;
    if (partes) {
      fecha = new Date(Number(partes[1]), Number(partes[2]) - 1, Number(partes[3]));
    } else {
      fecha = valor instanceof Date ? valor : new Date(valor);
    }
    if (isNaN(fecha.getTime())) return String(valor);
    const d = String(fecha.getDate()).padStart(2, '0');
    const m = String(fecha.getMonth() + 1).padStart(2, '0');
    return `${d}/${m}/${fecha.getFullYear()}`;
  }
}

/** Número → 1.234,56 (es-VE, separador de miles con punto). */
@Pipe({ name: 'monto', standalone: true })
export class MontoPipe implements PipeTransform {
  transform(valor: number | string | null | undefined, decimales = 2): string {
    if (valor === null || valor === undefined || valor === '') return '—';
    const n = typeof valor === 'string' ? Number(valor.replace(/\s/g, '').replace(',', '.')) : valor;
    if (n === null || n === undefined || isNaN(n)) return String(valor);
    return n.toLocaleString('es-VE', { minimumFractionDigits: decimales, maximumFractionDigits: decimales });
  }
}
