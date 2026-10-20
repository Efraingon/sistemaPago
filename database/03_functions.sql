-- ============================================================================
-- MONRYS CAPITAL — Funciones y triggers de integridad de negocio
-- Requiere: 01_schema.sql
-- (La lógica transaccional principal vive en la API .NET; aquí se protegen
--  los invariantes a nivel de base de datos.)
-- ============================================================================

SET search_path TO monrys, public;

-- ----------------------------------------------------------------------------
-- 1. CORRELATIVOS TRANSACCIONALES (reemplaza MAX()+1 sin lock de VFP)
--    Uso: SELECT monrys.fn_siguiente_correlativo('CAJA', '2026');
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_siguiente_correlativo(p_tabla VARCHAR, p_ejercicio CHAR(4))
RETURNS BIGINT AS $$
DECLARE
  v BIGINT;
BEGIN
  INSERT INTO correlativo (tabla, ejercicio, ultimo)
  VALUES (p_tabla, p_ejercicio, 1)
  ON CONFLICT (tabla, ejercicio) DO UPDATE SET ultimo = correlativo.ultimo + 1
  RETURNING ultimo INTO v;
  RETURN v;
END;
$$ LANGUAGE plpgsql;

-- ----------------------------------------------------------------------------
-- 2. FORMATO DE CORRELATIVOS LEGADOS (mantiene documentos impresos auditables)
--    ID_CAJA: 10 dígitos/año · RECIBO: 20 dígitos global · ID_TRANS: 10/año
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_formato_id(p_prefijo VARCHAR, p_nro BIGINT, p_digits INT)
RETURNS VARCHAR AS $$
  SELECT p_prefijo || LPAD(p_nro::TEXT, p_digits - LENGTH(p_prefijo), '0');
$$ LANGUAGE sql IMMUTABLE;

-- ----------------------------------------------------------------------------
-- 3. INVARIANTE DE CUSTODIO (regla de negocio clave del sistema VFP)
--    CUSTODIO.disponible = Σ COMPENSACION.abonos − Σ COMPENSACION.retiros
--    Se recalcula automáticamente tras cada inserción en compensacion.
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_recalcular_custodio(p_cliente VARCHAR, p_moneda VARCHAR)
RETURNS VOID AS $$
BEGIN
  INSERT INTO custodio (cliente_id, moneda_id, disponible, actualizado_en)
  SELECT p_cliente, p_moneda,
         COALESCE(SUM(abonos), 0) - COALESCE(SUM(retiros), 0),
         now()
  FROM compensacion
  WHERE cliente_id = p_cliente AND moneda_id = p_moneda
  GROUP BY cliente_id, moneda_id
  ON CONFLICT (cliente_id, moneda_id)
  DO UPDATE SET disponible = EXCLUDED.disponible,
                actualizado_en = now();
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION trg_compensacion_custodio()
RETURNS TRIGGER AS $$
BEGIN
  PERFORM fn_recalcular_custodio(NEW.cliente_id, NEW.moneda_id);
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_compensacion_custodio
AFTER INSERT ON compensacion
FOR EACH ROW EXECUTE FUNCTION trg_compensacion_custodio();

-- ----------------------------------------------------------------------------
-- 4. CIERRE DE CAJA (reconstruye la lógica que estaba compilada en monrroy.exe)
--    Cierra todos los movimientos de caja de una fecha dada.
--    El usuario/admin que cierra queda registrado en cierre_id.
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_cierre_caja(p_fecha DATE, p_usuario VARCHAR)
RETURNS INTEGER AS $$
DECLARE
  v_total INTEGER;
BEGIN
  UPDATE caja
  SET cierre_fecha = p_fecha,
      cierre_id    = p_usuario
  WHERE fecha = p_fecha
    AND cierre_fecha IS NULL
    AND estado = 'ACTIVO';
  GET DIAGNOSTICS v_total = ROW_COUNT;
  RETURN v_total;
END;
$$ LANGUAGE plpgsql;

-- ----------------------------------------------------------------------------
-- 5. MOVIMIENTO DE CAJA: helper que respeta el invariante de correlativos
--    (la API .NET llamará esto vía transacción; aquí como defensa de integridad)
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_insertar_caja(
  p_ejercicio  CHAR(4),
  p_origen     VARCHAR(3),   -- dominio: IN|EG|CO|VE|RE|EN|C1|C2|DE|DS|IB|EB|T5|T6 (CHECK en caja)
  p_fecha      DATE,
  p_monto      NUMERIC,
  p_descripcion VARCHAR,
  p_moneda     VARCHAR DEFAULT NULL,
  p_cliente    VARCHAR DEFAULT NULL,
  p_compra     VARCHAR DEFAULT NULL,
  p_venta      VARCHAR DEFAULT NULL,
  p_banco      VARCHAR DEFAULT NULL,
  p_tasa       NUMERIC DEFAULT NULL,
  p_usd        NUMERIC DEFAULT NULL,
  p_monto_bs   NUMERIC DEFAULT NULL
) RETURNS VARCHAR AS $$
DECLARE
  v_id    BIGINT;
  v_rec BIGINT;
BEGIN
  v_id    := fn_siguiente_correlativo('CAJA_ID',   p_ejercicio);
  v_rec := fn_siguiente_correlativo('CAJA_RECIBO', p_ejercicio);
  INSERT INTO caja (id, ejercicio, origen, fecha, recibo, monto, descripcion,
                    moneda_id, cliente_id, compra_id, venta_id, banco_id,
                    tasa, usd, monto_bs)
  VALUES (fn_formato_id('', v_id, 10), p_ejercicio, p_origen, p_fecha,
          fn_formato_id('', v_rec, 20), p_monto, p_descripcion,
          p_moneda, p_cliente, p_compra, p_venta, p_banco,
          p_tasa, p_usd, p_monto_bs);
  RETURN fn_formato_id('', v_id, 10);
END;
$$ LANGUAGE plpgsql;

-- ----------------------------------------------------------------------------
-- 6. BORRADO LÓGICO UNIFORME (VFP mezclaba DELETE físico y flags ANULADO)
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_anular_movimiento(
  p_tabla    VARCHAR,       -- 'caja' | 'transferencia' | 'compra' | 'venta'
  p_id       VARCHAR,
  p_ejercicio CHAR DEFAULT NULL,
  p_motivo   VARCHAR DEFAULT 'Sin motivo'
) RETURNS VOID AS $$
BEGIN
  IF p_tabla = 'caja' THEN
    UPDATE caja SET estado='ANULADO', anulado_en=now(), anulado_motivo=p_motivo
    WHERE ejercicio = p_ejercicio AND id = p_id AND estado='ACTIVO';
  ELSIF p_tabla = 'transferencia' THEN
    UPDATE transferencia SET estado='ANULADO', anulado_en=now(), anulado_motivo=p_motivo
    WHERE id = p_id AND estado='ACTIVO';
  ELSIF p_tabla = 'compra' THEN
    UPDATE compra SET estado='ANULADO', anulado_en=now(), anulado_motivo=p_motivo
    WHERE id = p_id AND estado='ACTIVO';
  ELSIF p_tabla = 'venta' THEN
    UPDATE venta SET estado='ANULADO', anulado_en=now(), anulado_motivo=p_motivo
    WHERE id = p_id AND estado='ACTIVO';
  ELSE
    RAISE EXCEPTION 'Tabla no soportada: %', p_tabla;
  END IF;
END;
$$ LANGUAGE plpgsql;
