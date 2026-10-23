# -*- coding: utf-8 -*-
"""
Migracion por lotes a Supabase (Monrys Capital).

Resuelve el problema de los archivos ETL grandes (>1 MB):

  * NO se hardcodean columnas: la lista de columnas se lee del propio
    archivo (cada INSERT ya la trae explicita y es la correcta).
  * NO se generan archivos intermedios: el cuerpo VALUES se parte en
    memoria en lotes de N filas y se ejecuta un INSERT por lote.
  * Cada lote se valida antes de enviarse (numero de valores == columnas),
    de modo que un error de parseo se detecta aqui y no en PostgreSQL.
  * Idempotente: antes de cargar una tabla se hace TRUNCATE ... RESTART
    IDENTITY (no hay FKs que apunten a estas tablas), y los INSERT llevan
    ON CONFLICT DO NOTHING.
  * Al final compara filas cargadas vs. filas declaradas en el archivo.

Uso:
    python tools/migrar_lotes.py                    # todo, 900 filas/lote
    python tools/migrar_lotes.py --lote 900
    python tools/migrar_lotes.py --tabla bitacora
    python tools/migrar_lotes.py --dry-run          # solo analiza
    python tools/migrar_lotes.py --sin-truncar      # no borra antes
    python tools/migrar_lotes.py --finales          # + 998, 999 y 02_seed
    python tools/migrar_lotes.py --verificar        # solo conteos
"""

import argparse
import os
import re
import sys
import time
from pathlib import Path

import psycopg2
from dotenv import dotenv_values

BASE = Path(__file__).resolve().parent.parent
DIR_MIG = BASE / "database" / "migracion"

# Archivos ETL con un unico INSERT masivo, en orden de carga.
ARCHIVOS = [
    "014_compra.sql",
    "015_venta.sql",
    "016a_operacion_recepcion.sql",
    "016b_operacion_recepcion.sql",
    "017a_operacion_pago.sql",
    "017b_operacion_pago.sql",
    "018_caja.sql",
    "019_compensacion.sql",
    "021_transferencia.sql",
    "022_bitacora.sql",
]

FINALES = ["998_final_datafix.sql", "999_final_counters.sql", "../02_seed.sql"]

TABLA_DE = {
    "014_compra.sql": "compra",
    "015_venta.sql": "venta",
    "016a_operacion_recepcion.sql": "operacion_recepcion",
    "016b_operacion_recepcion.sql": "operacion_recepcion",
    "017a_operacion_pago.sql": "operacion_pago",
    "017b_operacion_pago.sql": "operacion_pago",
    "018_caja.sql": "caja",
    "019_compensacion.sql": "compensacion",
    "021_transferencia.sql": "transferencia",
    "022_bitacora.sql": "bitacora",
}


# --------------------------------------------------------------------------
# Lectura / parseo del SQL
# --------------------------------------------------------------------------
def leer_texto(ruta):
    raw = ruta.read_bytes()
    for enc in ("utf-8-sig", "cp1252"):
        try:
            return raw.decode(enc)
        except UnicodeDecodeError:
            continue
    return raw.decode("utf-8", errors="replace")


def _fin_de_llave(texto, inicio):
    """Dado el indice de un '(', devuelve el indice de su ')' de cierre."""
    profundidad = 0
    en_comillas = False
    i = inicio
    n = len(texto)
    while i < n:
        c = texto[i]
        if en_comillas:
            if c == "'":
                if i + 1 < n and texto[i + 1] == "'":   # comilla escapada ''
                    i += 2
                    continue
                en_comillas = False
        else:
            if c == "'":
                en_comillas = True
            elif c == "(":
                profundidad += 1
            elif c == ")":
                profundidad -= 1
                if profundidad == 0:
                    return i
        i += 1
    raise ValueError("Parentesis sin cerrar")


def dividir_sentencias(texto):
    """Parte el archivo en sentencias SQL por ';' de nivel superior."""
    sentencias = []
    inicio = 0
    i = 0
    n = len(texto)
    en_comillas = False
    profundidad = 0
    while i < n:
        c = texto[i]
        if en_comillas:
            if c == "'":
                if i + 1 < n and texto[i + 1] == "'":      # comilla escapada ''
                    i += 2
                    continue
                en_comillas = False
        else:
            if c == "'":
                en_comillas = True
            elif c == "(":
                profundidad += 1
            elif c == ")":
                profundidad -= 1
            elif c == ";" and profundidad == 0:
                sentencias.append(texto[inicio:i + 1])
                inicio = i + 1
        i += 1
    resto = texto[inicio:].strip()
    if resto:
        sentencias.append(resto)
    return [s.strip() for s in sentencias if s.strip()]


def parsear_insert(sentencia):
    """Devuelve (tabla, columnas, tuplas) de una sentencia INSERT ... VALUES."""
    m = re.search(r"INSERT\s+INTO\s+([\w\".]+)\s*\(", sentencia, re.I)
    if not m:
        raise ValueError("No se encontro 'INSERT INTO tabla (...)'")
    tabla = m.group(1).split(".")[-1].strip('"')

    ini_cols = m.end() - 1
    fin_cols = _fin_de_llave(sentencia, ini_cols)
    columnas = [c.strip().strip('"') for c in sentencia[ini_cols + 1:fin_cols].split(",")]

    mv = re.compile(r"\bVALUES\b", re.I).search(sentencia, fin_cols + 1)
    if not mv:
        raise ValueError("No se encontro VALUES")

    fin_cuerpo = sentencia.rfind(";")
    if fin_cuerpo < mv.end():
        fin_cuerpo = len(sentencia)
    cuerpo = sentencia[mv.end():fin_cuerpo]

    tuplas = partir_tuplas(cuerpo, len(columnas))
    return tabla, columnas, tuplas


def filas_declaradas(texto):
    mc = re.match(r"\s*--\s*\S+:\s*(\d+)\s+filas", texto)
    return int(mc.group(1)) if mc else None


def partir_tuplas(cuerpo, n_columnas):
    """Parte '(...),(...)' en tuplas y valida el numero de valores de cada una."""
    tuplas = []
    i, n = 0, len(cuerpo)
    numero = 0
    while True:
        while i < n and cuerpo[i] in " \t\r\n,;":
            i += 1
        if i >= n:
            break
        if cuerpo[i] != "(":
            raise ValueError("Se esperaba '(' en posicion %d: %r" % (i, cuerpo[i:i + 60]))
        inicio = i
        profundidad = 0
        en_comillas = False
        comas = 0
        while i < n:
            c = cuerpo[i]
            if en_comillas:
                if c == "'":
                    if i + 1 < n and cuerpo[i + 1] == "'":
                        i += 2
                        continue
                    en_comillas = False
            else:
                if c == "'":
                    en_comillas = True
                elif c == "(":
                    profundidad += 1
                elif c == ")":
                    profundidad -= 1
                    if profundidad == 0:
                        break
                elif c == "," and profundidad == 1:
                    comas += 1
            i += 1
        if i >= n:
            raise ValueError("Tupla #%d sin cerrar" % (numero + 1))
        tuplas.append(cuerpo[inicio:i + 1])
        numero += 1
        if comas + 1 != n_columnas:
            raise ValueError(
                "Tupla #%d tiene %d valores y el INSERT declara %d columnas: %s"
                % (numero, comas + 1, n_columnas, cuerpo[inicio:inicio + 160])
            )
        i += 1
    return tuplas


# --------------------------------------------------------------------------
# Base de datos
# --------------------------------------------------------------------------
def conectar():
    cfg = dotenv_values(BASE / ".env")
    faltan = [k for k in ("SUPABASE_HOST", "SUPABASE_USER", "SUPABASE_PASSWORD") if not cfg.get(k)]
    if faltan:
        sys.exit("ERROR: faltan variables en .env: " + ", ".join(faltan))
    conn = psycopg2.connect(
        host=cfg["SUPABASE_HOST"].strip(),
        port=int(cfg.get("SUPABASE_PORT", "5432")),
        dbname=cfg.get("SUPABASE_DB", "postgres").strip(),
        user=cfg["SUPABASE_USER"].strip(),
        password=cfg["SUPABASE_PASSWORD"],
        connect_timeout=30,
        application_name="monrys-migracion",
    )
    conn.autocommit = True
    cur = conn.cursor()
    cur.execute("SET search_path TO monrys, public;")
    cur.execute("SET statement_timeout = '120s';")
    cur.close()
    return conn


def contar(cur, tabla):
    cur.execute('SELECT count(*) FROM monrys."%s"' % tabla)
    return cur.fetchone()[0]


def truncar(cur, tabla):
    cur.execute('TRUNCATE TABLE monrys."%s" RESTART IDENTITY' % tabla)


def insertar_lote(cur, tabla, columnas, tuplas):
    sql = 'INSERT INTO monrys."%s" (%s) VALUES %s ON CONFLICT DO NOTHING' % (
        tabla,
        ", ".join('"%s"' % c for c in columnas),
        ", ".join(tuplas),
    )
    cur.execute(sql)


# --------------------------------------------------------------------------
# Carga
# --------------------------------------------------------------------------
def cargar_archivo(cur, ruta, tabla_esperada, tam_lote, truncar_previo, dry_run):
    texto = leer_texto(ruta)
    declaradas = filas_declaradas(texto)

    # El ETL ya viene partido en bloques (~500 filas); cada bloque se funde
    # con el siguiente y se re-corta en lotes de `tam_lote` filas.
    bloques = []
    otras = []
    for sentencia in dividir_sentencias(texto):
        if re.search(r"\bINSERT\s+INTO\b", sentencia, re.I):
            tabla, columnas, tuplas = parsear_insert(sentencia)
            if tabla != tabla_esperada:
                raise ValueError(
                    "el archivo inserta en '%s' y se esperaba '%s'" % (tabla, tabla_esperada))
            if bloques and bloques[-1][0] == columnas:
                bloques[-1][1].extend(tuplas)
            else:
                bloques.append((columnas, list(tuplas)))
        else:
            otras.append(sentencia)

    total = sum(len(t) for _, t in bloques)
    aviso = ""
    if declaradas is not None and declaradas != total:
        aviso = "  [AVISO] el encabezado declara %d filas" % declaradas

    print("  %-34s %-20s %6d filas%s" % (ruta.name, tabla_esperada, total, aviso))

    if dry_run:
        return tabla_esperada, total, total, []

    for sentencia in otras:                 # comentarios / SET search_path
        try:
            cur.execute(sentencia)
        except Exception as e:
            print("      [AVISO] sentencia no-INSERT ignorada: %s" % str(e).splitlines()[0][:120])

    if truncar_previo:
        truncar(cur, tabla_esperada)

    t0 = time.time()
    errores = []
    numero = 0
    pendientes = sum((len(t) + tam_lote - 1) // tam_lote for _, t in bloques)
    for columnas, tuplas in bloques:
        for i in range(0, len(tuplas), tam_lote):
            numero += 1
            try:
                insertar_lote(cur, tabla_esperada, columnas, tuplas[i:i + tam_lote])
            except Exception as e:
                errores.append((numero, i, str(e).splitlines()[0][:300]))
                print("      [LOTE %d] ERROR: %s" % (numero, errores[-1][2]))
                if len(errores) >= 5:
                    print("      ... demasiados errores, se corta este archivo")
                    break
            if numero % 10 == 0 or numero == pendientes:
                print("      %d/%d lotes (%.0f%%) %.1fs"
                      % (numero, pendientes, 100.0 * numero / pendientes, time.time() - t0))
        if len(errores) >= 5:
            break

    cargadas = contar(cur, tabla_esperada)
    print("      -> en BD: %d filas (%.1fs)" % (cargadas, time.time() - t0))
    return tabla_esperada, total, cargadas, errores


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--lote", type=int, default=900, help="filas por INSERT (default 900)")
    ap.add_argument("--tabla", default="", help="solo estas tablas, separadas por coma")
    ap.add_argument("--sin-truncar", action="store_true", help="no borra la tabla antes de cargar")
    ap.add_argument("--dry-run", action="store_true", help="solo parsea y cuenta, no escribe")
    ap.add_argument("--finales", action="store_true", help="ejecuta 998, 999 y 02_seed al final")
    ap.add_argument("--solo-finales", action="store_true", help="no recarga datos, solo scripts finales")
    ap.add_argument("--verificar", action="store_true", help="solo muestra conteos actuales")
    args = ap.parse_args()

    filtro = {t.strip() for t in args.tabla.split(",") if t.strip()}

    conn = conectar()
    cur = conn.cursor()
    cur.execute("SELECT version()")
    print("Conectado: %s" % cur.fetchone()[0][:60])
    print("Lote: %d filas\n" % args.lote)

    if args.verificar:
        for t in sorted(set(TABLA_DE.values())):
            print("  %-22s %8d" % (t, contar(cur, t)))
        cur.close(); conn.close()
        return 0

    truncadas = set()
    resumen = []
    fallos = 0

    for nombre in ARCHIVOS:
        if args.solo_finales:
            break
        tabla = TABLA_DE[nombre]
        if filtro and tabla not in filtro:
            continue
        ruta = DIR_MIG / nombre
        if not ruta.exists():
            print("  [SKIP] %s no existe" % nombre)
            continue

        truncar_previo = (not args.sin_truncar) and (tabla not in truncadas)
        try:
            t, esperadas, cargadas, errores = cargar_archivo(
                cur, ruta, tabla, args.lote, truncar_previo, args.dry_run)
        except Exception as e:
            print("  [ERROR] %s: %s" % (nombre, e))
            fallos += 1
            resumen.append((tabla, 0, 0, "parseo: %s" % e))
            continue

        truncadas.add(tabla)
        if args.dry_run:
            continue
        if errores:
            fallos += 1
            resumen.append((tabla, esperadas, cargadas, "%d lotes fallidos" % len(errores)))
        elif cargadas < esperadas:
            fallos += 1
            resumen.append((tabla, esperadas, cargadas, "faltan %d filas" % (esperadas - cargadas)))
        else:
            resumen.append((tabla, esperadas, cargadas, "OK"))

    print("\n" + "=" * 72)
    print("  RESUMEN")
    print("=" * 72)
    print("  %-22s %10s %10s  %s" % ("tabla", "esperadas", "en BD", "estado"))
    for tabla, esp, car, estado in resumen:
        print("  %-22s %10d %10d  %s" % (tabla, esp, car, estado))

    if args.finales and not args.dry_run:
        print("\nAplicando scripts finales...")
        cur.execute("SET search_path TO monrys, public;")
        for rel in FINALES:
            ruta = (DIR_MIG / rel).resolve()
            if not ruta.exists():
                print("  [SKIP] %s" % ruta.name)
                continue
            try:
                cur.execute(leer_texto(ruta))
                print("  [OK]   %s" % ruta.name)
            except Exception as e:
                print("  [ERROR] %s: %s" % (ruta.name, str(e).splitlines()[0][:200]))
                fallos += 1

    cur.close()
    conn.close()
    print()
    if fallos:
        print("Terminado con %d problema(s)." % fallos)
        return 1
    print("Migracion completada sin errores.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
