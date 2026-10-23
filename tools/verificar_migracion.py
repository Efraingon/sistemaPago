# -*- coding: utf-8 -*-
"""Verificaciones de integridad despues de la migracion."""
import os

import psycopg2
from dotenv import dotenv_values

cfg = dotenv_values(os.path.join(os.path.dirname(__file__), "..", ".env"))
conn = psycopg2.connect(
    host=cfg["SUPABASE_HOST"].strip(), port=int(cfg.get("SUPABASE_PORT", "5432")),
    dbname=cfg.get("SUPABASE_DB", "postgres").strip(), user=cfg["SUPABASE_USER"].strip(),
    password=cfg["SUPABASE_PASSWORD"], connect_timeout=30,
)
conn.autocommit = True
cur = conn.cursor()


def uno(sql):
    cur.execute(sql)
    return cur.fetchone()


print("== Conteos vs. filas declaradas por el ETL ==")
esperado = {
    "compra": 8859, "venta": 2234, "caja": 9129, "compensacion": 8265,
    "transferencia": 23904, "bitacora": 54845,
    "operacion_recepcion": 8349 + 2566, "operacion_pago": 11521 + 3049,
    "custodio": 195, "cliente": 393, "cuenta_cliente": 498,
}
ok = True
for t, esp in esperado.items():
    n = uno('SELECT count(*) FROM monrys."%s"' % t)[0]
    estado = "OK" if n == esp else "DIFERENCIA"
    ok = ok and n == esp
    print("  %-22s %8d  esperado %8d  %s" % (t, n, esp, estado))

print("\n== Datafix 998 (banco_id derivado de cuenta_banco) ==")
for t in ("transferencia", "caja"):
    sin_banco, con_cta = uno(
        'SELECT count(*) FILTER (WHERE banco_id IS NULL), count(*) FILTER (WHERE cuenta_tran IS NOT NULL) FROM monrys."%s"' % t)
    print("  %-14s sin banco_id: %6d   con cuenta_tran: %6d" % (t, sin_banco, con_cta))

print("\n== Totales de dinero (control de integridad) ==")
for t, col in (("compra", "monto_bs"), ("venta", "monto_bs"), ("caja", "monto"),
               ("compensacion", "abonos"), ("transferencia", "monto")):
    s = uno('SELECT round(sum(%s), 2) FROM monrys."%s"' % (col, t))[0]
    print("  %-14s sum(%-9s) = %s" % (t, col, s))

print("\n== Correlativos (999) ==")
cur.execute("SELECT tabla, ejercicio, ultimo FROM monrys.correlativo ORDER BY 1, 2")
for fila in cur.fetchall():
    print("  %-14s %-7s %s" % fila)

print("\n== Usuarios y roles ==")
cur.execute("""SELECT u.id, u.nombre, u.tipo, u.activo, coalesce(r.nombre,'-')
               FROM monrys.usuario u
               LEFT JOIN monrys.usuario_rol ur ON ur.usuario_id = u.id
               LEFT JOIN monrys.rol r ON r.id = ur.rol_id ORDER BY u.id""")
for fila in cur.fetchall():
    print("  %-12s %-25s tipo=%s activo=%-5s rol=%s" % fila)

print("\n== Fechas extremas ==")
for t in ("compra", "venta", "caja", "transferencia", "bitacora"):
    a, b = uno('SELECT min(fecha), max(fecha) FROM monrys."%s"' % t)
    print("  %-14s %s -> %s" % (t, a, b))

cur.close()
conn.close()
print("\nIntegridad de conteos:", "OK" if ok else "REVISAR")
