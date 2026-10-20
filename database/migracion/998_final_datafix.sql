SET search_path TO monrys, public;
-- 998_final_datafix: VFP deducia el banco de la cuenta bancaria
-- (CTASTRAN no traia banco_id). Se deriva de cuenta_banco para que los
-- filtros/extractos por banco funcionen. Idempotente.
UPDATE transferencia t SET banco_id = cb.banco_id
FROM cuenta_banco cb
WHERE t.cuenta_tran = cb.numero_cuenta
  AND (t.banco_id IS NULL OR t.banco_id = '');
UPDATE caja c SET banco_id = cb.banco_id
FROM cuenta_banco cb
WHERE c.cuenta_tran = cb.numero_cuenta
  AND (c.banco_id IS NULL OR c.banco_id = '');
