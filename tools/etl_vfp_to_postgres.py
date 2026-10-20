#!/usr/bin/env python3
"""ETL: migra los DBF de C:\\Mundodolar\\datos\\ (VFP6, cp1252) a scripts SQL
para el esquema PostgreSQL de Monrys Capital (ver database/01_schema.sql).

Uso:
  python tools/etl_vfp_to_postgres.py [--datos C:\\Mundodolar\\datos] [--out database/migracion]

Produce: un archivo .sql por tabla (INSERTs en lotes de 500 filas), en orden de FKs,
más un 998_final_datafix.sql (deriva transferencia.banco_id de la cuenta) y un
999_final_counters.sql que deja los correlativos en el máximo migrado.
Solo stdlib (sin dependencias)."""
import sys, os, struct, argparse

# ---------------- DBF reader (cp1252) ----------------
def dec(b):
    if b is None: return None
    if isinstance(b, str): return b
    for enc in ("cp1252", "latin-1"):
        try: return b.decode(enc)
        except Exception: pass
    return b.decode("latin-1", "replace")

def read_dbf(path):
    with open(path, "rb") as f:
        data = f.read()
    nrec = struct.unpack("<I", data[4:8])[0]
    hlen = struct.unpack("<H", data[8:10])[0]
    rlen = struct.unpack("<H", data[10:12])[0]
    fields, off = [], 32
    while off < hlen - 1 and data[off] != 0x0D:
        fd = data[off:off+32]
        fields.append({"name": dec(fd[0:11]).rstrip("\x00").strip(), "type": chr(fd[11]),
                       "len": fd[16], "dec": fd[17]})
        off += 32
    return {"nrec": nrec, "hlen": hlen, "rlen": rlen, "fields": fields, "raw": data}

def read_fpt(path):
    """Extrae memos de un .FPT/.SCT/.FRT (offset = bloque * tamaño_bloque)."""
    if not os.path.exists(path): return []
    with open(path, "rb") as f:
        data = f.read()
    if len(data) < 8: return []
    blksz = struct.unpack(">H", data[6:8])[0] or 64
    memos, pos = [], 8
    while pos + 8 <= len(data):
        mtype = struct.unpack(">I", data[pos:pos+4])[0]
        mlen = struct.unpack(">I", data[pos+4:pos+8])[0]
        if mlen == 0 or pos + 8 + mlen > len(data):
            pos += 8; memos.append(""); continue
        blob = data[pos+8:pos+8+mlen]
        memos.append(dec(blob) if mtype == 2 else "")
        pos += 8 + mlen
    return memos

def rows(path):
    h = read_dbf(path)
    names = [f["name"] for f in h["fields"]]
    for i in range(h["nrec"]):
        start = h["hlen"] + i * h["rlen"]
        rec = h["raw"][start:start+h["rlen"]]
        if not rec or rec[0:1] == b"\x1a": break
        if rec[0:1] == b"*": continue       # borrado lógico VFP: omitir
        row, pos = {}, 1
        for f in h["fields"]:
            chunk = rec[pos:pos+f["len"]]; pos += f["len"]
            t = f["type"]
            if t == "M":
                try: row[f["name"]] = int(dec(chunk).strip() or 0)
                except Exception: row[f["name"]] = 0
            elif t == "I" and len(chunk) == 4: row[f["name"]] = struct.unpack("<i", chunk)[0]
            elif t == "B" and len(chunk) == 8: row[f["name"]] = struct.unpack("<d", chunk)[0]
            elif t == "L": row[f["name"]] = chunk[:1] in (b"T", b"Y", b"1")
            elif t == "T" and len(chunk) == 8:
                # VFP DATETIME: JDN (int32 LE, número juliano) + milisegundos (int32 LE)
                jdn, ms = struct.unpack("<ii", chunk)
                if jdn == 0 and ms == 0:
                    row[f["name"]] = ""
                else:
                    # JDN → fecha gregoriana (algoritmo Fliegel-Van Flandern)
                    l = jdn + 68569
                    n = (4 * l) // 146097
                    l = l - (146097 * n + 3) // 4
                    i = (4000 * (l + 1)) // 1461001
                    l = l - (1461 * i) // 4 + 31
                    j = (80 * l) // 2447
                    d = l - (2447 * j) // 80
                    l = j // 11
                    m = j + 2 - 12 * l
                    y = 100 * (n - 49) + i + l
                    row[f["name"]] = f"{y:04d}-{m:02d}-{d:02d} {ms//3600000:02d}:{(ms//60000)%60:02d}:{(ms//1000)%60:02d}"
            else: row[f["name"]] = dec(chunk)
        yield row

# ---------------- helpers de conversión ----------------
def q(v):
    """Valor → literal SQL."""
    if v is None: return "NULL"
    if isinstance(v, bool): return "TRUE" if v else "FALSE"
    if isinstance(v, (int, float)): return str(v)
    s = str(v).strip()
    if s == "": return "NULL"
    return "'" + s.replace("'", "''") + "'"

def qd(v):
    """Fecha VFP 'YYYYMMDD' → 'YYYY-MM-DD' | NULL."""
    s = (v or "").strip()
    if len(s) != 8 or not s.isdigit() or s.startswith("0000"): return "NULL"
    return f"'{s[:4]}-{s[4:6]}-{s[6:8]}'"

def qn(v, default=0):
    """Numérico VFP (puede venir vacío) → número | default."""
    s = (v or "").strip() if isinstance(v, str) else v
    if s in ("", None): return str(default)
    try: return str(float(s))
    except Exception: return str(default)

def qp(v, pad):
    """CHAR id con relleno de ceros → literal conservando formato."""
    s = (v or "").strip()
    return q(s) if s else "NULL"

def vs(v):
    """Valor VFP crudo → string recortado."""
    return (v or "").strip() if isinstance(v, str) else ("" if v is None else str(v))

def qej(v):
    """Ejercicio (año) VFP → literal CHAR(4) NOT NULL; vacío → '0000'."""
    e = vs(v)
    return q(e) if e and e.isdigit() else q("0000")

def qfd(v, ejercicio):
    """Fecha VFP; si viene vacía usa 01-01 del ejercicio (columna NOT NULL)."""
    s = (v or "").strip()
    if len(s) == 8 and s.isdigit() and not s.startswith("0000"):
        return qd(s)
    e = vs(ejercicio)
    return q(f"{e}-01-01" if e and e.isdigit() else "1900-01-01")

def opid(ejercicio, secuencia, quoted=True):
    """ID canónico de operación (compra/venta): ejercicio(4) + secuencia(12 dígitos).

    VFP guarda dos formas: la corta ('0000000005', en COMPRAS/COMPRECIB) y la
    compuesta ('20220000000001', en CAJA/CTASTRAN). El prefijo almacenado manda."""
    e, s = vs(ejercicio), vs(secuencia)
    if len(s) >= 14:
        e, s = s[:4], s[4:]
    if not e or not e.isdigit():
        e = "0000"
    out = e + s.zfill(12)
    return q(out) if quoted else out

def opref(ejercicio, secuencia):
    """Referencia a operación desde caja/transferencia: NULL si no hay valor."""
    if not vs(secuencia):
        return "NULL"
    return opid(ejercicio, secuencia)

class Writer:
    def __init__(self, table, out): self.table, self.file, self.buf, self.n = table, out, [], 0
    def add(self, cols, vals):
        if self.n % 500 == 0:
            if self.n: self.buf.append(";")
            self.buf.append(f"INSERT INTO {self.table} ({cols}) VALUES")
        else:
            self.buf.append(",")
        self.buf.append("(" + vals + ")")
        self.n += 1
    def flush(self):
        if self.n:
            self.buf.append(";\n")
            with open(os.path.join(ARGS.out, self.file + ".sql"), "w", encoding="utf-8") as f:
                f.write("-- " + self.file + f": {self.n} filas\nSET search_path TO monrys, public;\n" + "".join(self.buf))
        return self.n

def run(nombre, fn, out, table):
    w = Writer(table, out)
    try:
        n = fn(w)
    except FileNotFoundError:
        print(f"  [skip] {nombre}: no existe"); return 0
    total = w.flush()
    print(f"  [ok]   {nombre} -> {out}.sql -> tabla '{table}' ({total} filas)")
    return total

# ---------------- tablas ----------------
def main():
    D = ARGS.datos
    totales = {}

    def tipomony(w):
        for r in rows(os.path.join(D, "TIPOMONY.DBF")):
            w.add("id,descripcion,monto_dolar,principal,valor_nacional,nacional,activa",
                  ",".join([qp(r["ID_MONY"],4), q(r["DESCRIP"]), qn(r["MONTODOLAR"]), q(r["PRINCIPAL"]),
                            qn(r["VALONACIO"]), q(r["NACIONAL"]), "TRUE"]))
    totales["tipo_moneda"] = run("TIPOMONY", tipomony, "001_tipo_moneda", "tipo_moneda")

    def qn2(v, d=0):
        s = (v or "").strip() if isinstance(v, str) else v
        if s in ("", None): return str(d)
        try: return str(float(s))
        except Exception: return str(d)
    def qn2n(v):
        """Numérico anulable: vacío → NULL. Mejor que 0 porque la API distingue
        'sin calcular' (NULL) de un cero real al aplicar la tasa de conversión."""
        s = (v or "").strip() if isinstance(v, str) else v
        if s in ("", None): return "NULL"
        try: return str(float(s))
        except Exception: return "NULL"
    def configuracion2(w):
        # DOLARMAIN.ID_MONY2 = '0003' pero TIPOMONY solo tiene 0001/0002: referencia
        # colgante heredada del VFP (rompería la FK de moneda_nac_id en compras/ventas).
        monedas = list(rows(os.path.join(D, "TIPOMONY.DBF")))
        ids = {qp(m["ID_MONY"], 4) for m in monedas}

        def moneda_ref(v):
            ref = qp(v, 4)
            if ref in ids: return ref
            for m in monedas:
                if m.get("NACIONAL"): return qp(m["ID_MONY"], 4)
            return ref

        for r in rows(os.path.join(D, "dolarmain.DBF")):
            w.add("id,nro_cliente,nombre_empresa,direccion,telefono,rif,iva,moneda_principal_id,moneda_nacional_id,tasa_activa,tasa_compra,tasa_venta,version",
                  ",".join(["1", qn2(r["NROCLIENTE"]), q(r["NOMBCLIE"]), q(r["DIRECLIE"]), q(r["TELECLIE"]),
                            q(r["RIFCLIE"]), qn2(r["IVA"]), moneda_ref(r["ID_MONY"]), moneda_ref(r["ID_MONY2"]),
                            qn2(r["TASACTIVA"]), qn2(r["TASACOMP"]), qn2(r["TASAVENTA"]), q(r["UVERSION"])]))
    totales["configuracion"] = run("DOLARMAIN", configuracion2, "002_configuracion", "configuracion")

    def calend(w):
        for r in rows(os.path.join(D, "CALENDTASA.DBF")):
            w.add("fecha,nombre_dia,tasa,tasa_compra,tasa_venta",
                  ",".join([qd(r["FECHA"]), q(r["NOMBDIA"]), qn2(r["TASA"]), qn2(r["TASACOMP"]), qn2(r["TASAVENTA"])]))
    totales["calend_tasa"] = run("CALENDTASA", calend, "003_calend_tasa", "calend_tasa")

    def maestro(w):
        for r in rows(os.path.join(D, "MAESTRO.DBF")):
            w.add("cuenta,descripcion", qp(r["CTACONTAB"],10) + "," + q(r["DESCRIPCIO"]))
    totales["plan_contable"] = run("MAESTRO", maestro, "004_plan_contable", "plan_contable")

    def coding(w):
        for r in rows(os.path.join(D, "CODIINGRESO.DBF")):
            w.add("id,descripcion,cuenta_contable",
                  ",".join([qp(r["ID_CODI"],5), q(r["DESCRIP"]), qp(r["CTACONTAB"],10)]))
    totales["codificacion_ingreso"] = run("CODIINGRESO", coding, "005_codificacion_ingreso", "codificacion_ingreso")

    def codegr(w):
        for r in rows(os.path.join(D, "CODIEGRESO.DBF")):
            w.add("id,descripcion,cuenta_contable",
                  ",".join([qp(r["ID_CODE"],5), q(r["DESCRIP"]), qp(r["CTACONTAB"],10)]))
    totales["codificacion_egreso"] = run("CODIEGRESO", codegr, "006_codificacion_egreso", "codificacion_egreso")

    def bancos(w):
        for r in rows(os.path.join(D, "bancos.DBF")):
            w.add("id,nombre,direccion,telefonos,fax,correo,contacto,nacional,saldo,cuenta_contable,activo",
                  ",".join([qp(r["ID_BANCO"],5), q(r["BANCO"]), q(r["DIRECCION"]), q(r["TELEFONOS"]),
                            q(r["FAX"]), q(r["CORREO"]), q(r["CONTACTO"]),
                            str(int(float(r["NACIONAL"] or 1))),
                            qn2(r["SALDO"]), qp(r["CTACONTAB"],10), "TRUE"]))
    totales["banco"] = run("bancos", bancos, "007_banco", "banco")

    def clientes(w):
        for r in rows(os.path.join(D, "CLIENTES.DBF")):
            w.add("id,persona,razon_social,nombres,apellidos,rif,cedula,direccion,telefonos,email,observaciones,cuenta_contable,activo",
                  ",".join([qp(r["ID_CLIE"],10), str(int(float(r["PERSONA"] or 1))),
                            q(r["RAZONSOCIA"]), q(r["NOMBRES"]), q(r["APELLIDOS"]), q(r["RIF"]),
                            q(r["CEDULA"]), q(r["DIRECCION"]), q(r["TELEFONOS"]), q(r["EMAIL"]),
                            q(r["OBSERVA"]), qp(r["CTACONTAB"],10), "TRUE"]))
    totales["cliente"] = run("CLIENTES", clientes, "008_cliente", "cliente")

    def centros(w):
        # VFP permite varios PPAL='X' por cliente (67 casos en el origen): solo el
        # primero se conserva como principal para respetar ux_centro_ppal.
        principales = set()
        for r in rows(os.path.join(D, "CENTROSCLIE.DBF")):
            cid = vs(r["ID_CLIE"])
            ppal = (r["PPAL"] == "X") and (cid not in principales)
            if ppal:
                principales.add(cid)
            w.add("cliente_id,descripcion,principal",
                  ",".join([qp(r["ID_CLIE"],10), q(r["DESCRIP"]), q(ppal)]))
    totales["centro_costo"] = run("CENTROSCLIE", centros, "009_centro_costo", "centro_costo")

    def ctas(w):
        for r in rows(os.path.join(D, "ctas.DBF")):
            w.add("banco_id,numero_cuenta,tipo,debitos,creditos,saldo,cuenta_contable,activa",
                  ",".join([qp(r["ID_BANCO"],5), qp(r["NUMECTAS"],20), qn2(r["TIPO"]), qn2(r["DEBITOS"]),
                            qn2(r["CREDITOS"]), qn2(r["SALDO"]), qp(r["CTACONTAB"],10), "TRUE"]))
    totales["cuenta_banco"] = run("ctas", ctas, "010_cuenta_banco", "cuenta_banco")

    def ctasclie(w):
        for r in rows(os.path.join(D, "ctasclie.DBF")):
            w.add("banco_id,cliente_id,numero_cuenta,cuenta_contable,activa",
                  ",".join([qp(r["ID_BANCO"],5), qp(r["ID_CLIE"],10), qp(r["NUMECTAS"],20),
                            qp(r["CTACONTAB"],10), "TRUE"]))
    totales["cuenta_cliente"] = run("ctasclie", ctasclie, "011_cuenta_cliente", "cuenta_cliente")

    def cono(w, dbf, moneda):
        for r in rows(os.path.join(D, dbf)):
            w.add("id,moneda_id,tipo,valor,activa",
                  ",".join([qp(r["ID_CONO"],4), q(moneda), q(r["TIPO"]), qn2(r["VALOR"]), "TRUE"]))
    totales["denominacion"] = (run("conodol", lambda w: cono(w, "conodol.DBF", "0001"), "012a_denominacion_usd", "denominacion")
                               + run("cononac", lambda w: cono(w, "cononac.DBF", "0002"), "012b_denominacion_bs", "denominacion"))

    def usuarios(w):
        # NO migrar claves en texto plano: placeholder + cuenta inactiva (reset en primer login)
        for r in rows(os.path.join(D, "usuarios.DBF")):
            w.add("id,nombre,clave_hash,tipo,historico_finanzas,ruta_pantalla,activo,bloqueado",
                  ",".join([qp(r["ID_USUARIO"],10), q(r["NOMBREUSU"]),
                            q("PBKDF2|100000|bWlncmF0ZQ==|bWlncmF0ZQ=="),  # placeholder inválido → forzar reset
                            str(int(float(r["TIPO"] or 2))), q(r["FINANZAS"]), q(r["RUTAPANT"]),
                            "FALSE", q(r["BLOQUEO"])]))  # inactivos hasta reset de clave
    totales["usuario"] = run("USUARIOS", usuarios, "013_usuario", "usuario")

    def compras(w):
        for r in rows(os.path.join(D, "COMPRAS.DBF")):
            if not vs(r["ID_COMPRA"]):
                continue  # 1 fila en blanco del origen (sin id, cliente ni montos)
            w.add("id,ejercicio,cliente_id,fecha,moneda_divisa_id,moneda_nac_id,mto_x_divisa,mto_x_nac,tasa,cantidad,monto_bs,pagado_bs,saldo_bs,recibido_divisa,por_recibir,observaciones,centro_costo,estado",
                  ",".join([opid(r["EJERCC"], r["ID_COMPRA"]), qej(r["EJERCC"]), qp(r["ID_CLIE"],10), qfd(r["FECHA"], r["EJERCC"]),
                            qp(r["ID_MONY1"],4), qp(r["ID_MONY2"],4), qn2(r["MTOXD1"]), qn2(r["MTOXD2"]),
                            qn2(r["VALONACIO"]), qn2(r["CANTIDAD"]), qn2(r["MONTO_BS"]), qn2(r["PAGADO_BS"]),
                            qn2(r["SALDO_BS"]), qn2(r["RECIBIDODV"]), qn2(r["PORECIB"]),
                            q(r["OBSERVA"]), q(r["CENTROCOST"]), q("ACTIVO")]))
    totales["compra"] = run("COMPRAS", compras, "014_compra", "compra")

    def ventas(w):
        for r in rows(os.path.join(D, "VENTAS.DBF")):
            w.add("id,ejercicio,cliente_id,fecha,moneda_divisa_id,moneda_nac_id,mto_x_divisa,mto_x_nac,tasa,cantidad,monto_bs,pagado_bs,saldo_bs,recibido_divisa,por_recibir,observaciones,centro_costo,estado",
                  ",".join([opid(r["EJERCC"], r["ID_VENTA"]), qej(r["EJERCC"]), qp(r["ID_CLIE"],10), qfd(r["FECHA"], r["EJERCC"]),
                            qp(r["ID_MONY1"],4), qp(r["ID_MONY2"],4), qn2(r["MTOXD1"]), qn2(r["MTOXD2"]),
                            qn2(r["VALONACIO"]), qn2(r["CANTIDAD"]), qn2(r["MONTO_BS"]), qn2(r["PAGADO_BS"]),
                            qn2(r["SALDO_BS"]), qn2(r["RECIBIDODV"]), qn2(r["PORECIB"]),
                            q(r["OBSERVA"]), q(r["CENTROCOST"]), q("ACTIVO")]))
    totales["venta"] = run("VENTAS", ventas, "015_venta", "venta")

    def recep(w, dbf, tipo, campo):
        for r in rows(os.path.join(D, dbf)):
            w.add("tipo_op,operacion_id,nroper,ejercicio,fecha,cantidad,por_recibir,recibo,recibo2,banco_credito_id,banco_debito_id,cta_credito,cta_debito,tipo_pago,compensado,de_caja,de_banco",
                  ",".join([q(tipo), opid(r["EJERCC"], r[campo]), qn2(r["NROPER"]), qej(r["EJERCC"]), qfd(r["FECHA"], r["EJERCC"]),
                            qn2(r["CANTIDAD"]), qn2(r["PORECIB"]), q(r["RECIBO"]), q(r["RECIBO2"]),
                            qp(r["BANCOCREDI"],5), qp(r["BANCODEBE"],5), q(r["CTACREDI"]), q(r["CTADEBE"]),
                            q(r["TIPOPAGO"]), q(r["COMPENSAC"]), q(r["DECAJA"]), q(r["DEBANCO"])]))
    totales["operacion_recepcion"] = (run("COMPRECIB", lambda w: recep(w, "COMPRECIB.DBF", "C", "ID_COMPRA"), "016a_operacion_recepcion", "operacion_recepcion")
                                      + run("VENRECIB", lambda w: recep(w, "VENRECIB.DBF", "V", "ID_VENTA"), "016b_operacion_recepcion", "operacion_recepcion"))

    def pagos(w, dbf, tipo, campo):
        for r in rows(os.path.join(D, dbf)):
            w.add("tipo_op,operacion_id,nroper,ejercicio,fecha,monto,banco_debito_id,cta_debito,banco_credito_id,cta_credito,recibo,tipo_pago,saldo",
                  ",".join([q(tipo), opid(r["EJERCC"], r[campo]), qn2(r["NROPER"]), qej(r["EJERCC"]), qfd(r["FECHA"], r["EJERCC"]),
                            qn2(r["MONTO"]), qp(r["BANCODEBE"],5), q(r["CTADEBE"]), qp(r["BANCOCREDI"],5),
                            q(r["CTACREDI"]), q(r["RECIBO"]), q(r["TIPOPAGO"]), qn2(r["SALDO"])]))
    totales["operacion_pago"] = (run("COMPAGAR", lambda w: pagos(w, "COMPAGAR.DBF", "C", "ID_COMPRA"), "017a_operacion_pago", "operacion_pago")
                                 + run("VENPAGAR", lambda w: pagos(w, "VENPAGAR.DBF", "V", "ID_VENTA"), "017b_operacion_pago", "operacion_pago"))

    def caja(w):
        for r in rows(os.path.join(D, "CAJA.DBF")):
            anulado = bool(r["ANULADO"])
            w.add("id,ejercicio,origen,fecha,recibo,monto,descripcion,observaciones,moneda_id,cliente_id,compra_id,venta_id,banco_id,cuenta_tran,numero_tran,tasa,usd,monto_bs,codi_ingreso_id,codi_egreso_id,centro_costo,estado,anulado_en,anulado_motivo",
                  ",".join([qp(r["ID_CAJA"],10), qej(r["EJERCC"]), qp(r["ORIGEN"],2), qfd(r["FECHA"], r["EJERCC"]),
                            qp(r["RECIBO"],20), qn2(r["MONTO"]), q(r["DESCRIP"]), q(r["OBSERVA"]),
                            qp(r["ID_MONY"],4), qp(r["ID_CLIE"],10), opref(r["EJERCC"], r["ID_COMPRA"]), opref(r["EJERCC"], r["ID_VENTA"]),
                            qp(r["ID_BANCO"],5), q(r["CTASTRAN"]), q(r["NUMETRAN"]), qn2n(r["TASA"]),
                            qn2n(r["USD"]), qn2n(r["MTOBS"]), qp(r["ID_CODI"],5), qp(r["ID_CODE"],5),
                            q(r["CENTROCOST"]), q("ANULADO") if anulado else q("ACTIVO"),
                            qd(r["FECHANUL"]), q("Anulado en origen (VFP)") if anulado else "NULL"]))
    totales["caja"] = run("CAJA", caja, "018_caja", "caja")

    def compensac(w):
        for r in rows(os.path.join(D, "compensac.DBF")):
            w.add("id,ejercicio,fecha,descripcion,cliente_id,moneda_id,abonos,retiros,recibo_tran,cta_tran,centro_costo,observaciones",
                  ",".join([qp(r["ID_TRANS"],10), qej(r["EJERCC"]), qfd(r["FECHA"], r["EJERCC"]), q(r["DESCRIP"]),
                            qp(r["ID_CLIE"],10), qp(r["ID_MONY"],4), qn2(r["ABONOS"]), qn2(r["RETIROS"]),
                            q(r["RECITRAN"]), q(r["CTASTRAN"]), q(r["CENTROCOST"]), q(r["OBSERVA"])]))
    totales["compensacion"] = run("COMPENSAC", compensac, "019_compensacion", "compensacion")

    def custodio(w):
        for r in rows(os.path.join(D, "CUSTODIO.DBF")):
            w.add("cliente_id,moneda_id,disponible,disponible_bs",
                  ",".join([qp(r["ID_CLIE"],10), qp(r["ID_MONY"],4), qn2(r["DISPONIBLE"]), qn2(r["DISPONIBB"])]))
    totales["custodio"] = run("CUSTODIO", custodio, "020_custodio", "custodio")

    def tran(w):
        for r in rows(os.path.join(D, "CTASTRAN.DBF")):
            anulado = bool(r["ANULADO"])
            w.add("origen,tipo_tran,fecha,numero_tran,cuenta_tran,beneficiario,monto,cliente_id,motivo,compra_id,venta_id,moneda_id,banco_id,cuenta_clie,de_caja,tasa,usd,monto_bs,codi_ingreso_id,codi_egreso_id,estado,anulado_en,anulado_motivo",
                  ",".join([qp(r["ORIGTRAN"],2), q(r["TIPOTRAN"]), qd(r["FECHTRAN"]), q(r["NUMETRAN"]),
                            q(r["CTASTRAN"]), q(r["BENETRAN"]), qn2(r["MONTTRAN"]), qp(r["ID_CLIE"],10),
                            q(r["MOTITRAN"]), opref(r.get("EJERCC"), r["ID_COMPRA"]), opref(r.get("EJERCC"), r["ID_VENTA"]), qp(r["ID_MONY"],4),
                            qp(r["ID_BANCO"],5), q(r["CTASCLIE"]), q(r["DECAJA"]), qn2n(r["TASA"]),
                            qn2n(r["USD"]), qn2n(r["MTOBS"]), qp(r["ID_CODI"],5), qp(r["ID_CODE"],5),
                            q("ANULADO") if anulado else q("ACTIVO"),
                            qd(r["FECHANUL"]), q("Anulado en origen (VFP)") if anulado else "NULL"]))
    totales["transferencia"] = run("CTASTRAN", tran, "021_transferencia", "transferencia")

    def bitacora(w):
        for r in rows(os.path.join(D, "bitacora.DBF")):
            # FECHA es DATETIME VFP → rows() ya la convierte a 'YYYY-MM-DD HH:MM:SS'
            f = (r["FECHA"] or "").strip()
            w.add("fecha,usuario_id,tipo,accion,observaciones,equipo",
                  ",".join([q(f) if f else "NULL", qp(r["ID_USUARIO"],10), str(int(float(r["TIPO"] or 0))),
                            q(r["ACCION"]), q(r["OBSERVA"]), q(r["EQUIPO"])]))
    totales["bitacora"] = run("bitacora", bitacora, "022_bitacora", "bitacora")

    # --- Correlativos: semillas EXACTAS de los contadores que usa la API --------
    # CorrelativoService pide: "COMPRA"/"VENTA"/"CAJA_ID"/"COMPENSACION" por año
    # (ejercicio = año de la fecha del movimiento) y "CAJA_RECIBO"/"CLIENTE" con
    # EjercicioGlobal = "GLOBAL". Si una semilla falta, el contador arranca en 1 y
    # choca por PK/UNIQUE contra los datos migrados (p.ej. cliente '0000000001').
    def _num(v):
        s = vs(v)
        return int(s) if s.isdigit() else None

    def _ej(v):
        e = vs(v)
        return e if e.isdigit() else "0000"

    por_ejercicio = {}   # tabla -> ejercicio -> maximo
    global_max = {}      # tabla -> maximo (contadores globales)

    def _sembrar(tabla, ejercicio, valor):
        if valor is None:
            return
        mapa = por_ejercicio.setdefault(tabla, {})
        if valor > mapa.get(ejercicio, 0):
            mapa[ejercicio] = valor

    # compras/ventas: el id canónico es ejercicio(4) + secuencia(12)
    for tabla, archivo, campo in (("COMPRA", "COMPRAS.DBF", "ID_COMPRA"),
                                  ("VENTA", "VENTAS.DBF", "ID_VENTA")):
        for r in rows(os.path.join(D, archivo)):
            if not vs(r[campo]):
                continue
            idop = opid(r["EJERCC"], r[campo], quoted=False)
            _sembrar(tabla, idop[:4], int(idop[4:]))

    # caja: id por año (PK compuesta) + recibo GLOBAL (UNIQUE)
    for r in rows(os.path.join(D, "CAJA.DBF")):
        _sembrar("CAJA_ID", _ej(r["EJERCC"]), _num(r["ID_CAJA"]))
        rec = _num(r["RECIBO"])
        if rec is not None and rec > global_max.get("CAJA_RECIBO", 0):
            global_max["CAJA_RECIBO"] = rec

    # compensaciones: id por año
    for r in rows(os.path.join(D, "compensac.DBF")):
        _sembrar("COMPENSACION", _ej(r["EJERCC"]), _num(r["ID_TRANS"]))

    # clientes: id global de 10 digitos
    for r in rows(os.path.join(D, "CLIENTES.DBF")):
        c = _num(r["ID_CLIE"])
        if c is not None and c > global_max.get("CLIENTE", 0):
            global_max["CLIENTE"] = c

    filas = [(t, e, v) for t, mapa in por_ejercicio.items() for e, v in sorted(mapa.items()) if v > 0]
    filas += [("CAJA_RECIBO", "GLOBAL", global_max.get("CAJA_RECIBO", 0)),
              ("CLIENTE", "GLOBAL", global_max.get("CLIENTE", 0))]
    filas.sort()
    with open(os.path.join(ARGS.out, "999_final_counters.sql"), "w", encoding="utf-8") as f:
        f.write("SET search_path TO monrys, public;\n")
        valores = ",".join(f"('{t}','{e}',{v})" for t, e, v in filas)
        if valores:
            f.write(f"INSERT INTO correlativo(tabla,ejercicio,ultimo) VALUES {valores}\n")
            f.write("ON CONFLICT (tabla,ejercicio) DO UPDATE SET ultimo = GREATEST(correlativo.ultimo, EXCLUDED.ultimo);\n")
        f.write("-- Semillas generadas por el ETL; coinciden con las claves que pide la\n")
        f.write("-- API (CorrelativoService): COMPRA/VENTA/CAJA_ID/COMPENSACION por anyo y\n")
        f.write("-- CAJA_RECIBO/CLIENTE con ejercicio 'GLOBAL'.\n")
    print(f"  [ok]   correlativos -> 999_final_counters.sql ({len(filas)} semillas)")

    # --- Datafix: banco en transferencias/caja --------------------------------
    # VFP no guardaba el banco en CTASTRAN: el banco se deducia de la cuenta
    # bancaria. Se deriva de cuenta_banco(numero_cuenta) para que los filtros y
    # extractos por banco encuentren las filas (~23.900 transferencias).
    with open(os.path.join(ARGS.out, "998_final_datafix.sql"), "w", encoding="utf-8") as f:
        f.write("SET search_path TO monrys, public;\n")
        f.write("-- 998_final_datafix: VFP deducia el banco de la cuenta bancaria\n")
        f.write("-- (CTASTRAN no traia banco_id). Se deriva de cuenta_banco para que los\n")
        f.write("-- filtros/extractos por banco funcionen. Idempotente.\n")
        f.write("UPDATE transferencia t SET banco_id = cb.banco_id\n")
        f.write("FROM cuenta_banco cb\n")
        f.write("WHERE t.cuenta_tran = cb.numero_cuenta\n")
        f.write("  AND (t.banco_id IS NULL OR t.banco_id = '');\n")
        f.write("UPDATE caja c SET banco_id = cb.banco_id\n")
        f.write("FROM cuenta_banco cb\n")
        f.write("WHERE c.cuenta_tran = cb.numero_cuenta\n")
        f.write("  AND (c.banco_id IS NULL OR c.banco_id = '');\n")
    print("  [ok]   datafix banco_id -> 998_final_datafix.sql")

    print("\n== Resumen ETL ==")
    for k, v in totales.items():
        if v: print(f"  {k:<22} {v:>7} filas")

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--datos", default=r"C:\Mundodolar\datos")
    ap.add_argument("--out", default=r"C:\Negocio\sistemaPago\database\migracion")
    ARGS = ap.parse_args()
    os.makedirs(ARGS.out, exist_ok=True)
    print(f"ETL VFP -> PostgreSQL\n  origen: {ARGS.datos}\n  destino: {ARGS.out}\n")
    main()
