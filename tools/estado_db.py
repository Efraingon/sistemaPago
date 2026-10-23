# -*- coding: utf-8 -*-
"""Muestra el estado actual de la base remota: tablas, filas y columnas clave."""
import os
import sys

import psycopg2
from dotenv import load_dotenv

load_dotenv()

conn = psycopg2.connect(
    host=os.getenv("SUPABASE_HOST"),
    port=os.getenv("SUPABASE_PORT", "5432"),
    dbname=os.getenv("SUPABASE_DB", "postgres"),
    user=os.getenv("SUPABASE_USER"),
    password=os.getenv("SUPABASE_PASSWORD"),
    connect_timeout=30,
)
conn.autocommit = True
cur = conn.cursor()

print("=" * 60)
print("TABLAS EN SCHEMA monrys")
print("=" * 60)
cur.execute("""
    SELECT tablename FROM pg_tables
    WHERE schemaname = 'monrys' ORDER BY tablename
""")
tablas = [r[0] for r in cur.fetchall()]
for t in tablas:
    cur.execute('SELECT count(*) FROM monrys."%s"' % t)
    print("  %-24s %8d filas" % (t, cur.fetchone()[0]))

print()
print("=" * 60)
print("COLUMNAS DE TABLAS GRANDES")
print("=" * 60)
for t in ["compra", "venta", "operacion_recepcion", "operacion_pago",
          "caja", "compensacion", "transferencia", "bitacora"]:
    cur.execute("""
        SELECT column_name FROM information_schema.columns
        WHERE table_schema='monrys' AND table_name=%s
        ORDER BY ordinal_position
    """, (t,))
    cols = [r[0] for r in cur.fetchall()]
    print("  %-22s (%d): %s" % (t, len(cols), ",".join(cols)))

cur.close()
conn.close()
