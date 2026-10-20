-- ============================================================================
-- MONRYS CAPITAL — Seed de datos base
-- Requiere: 01_schema.sql ejecutado
-- ============================================================================

SET search_path TO monrys, public;

-- Monedas (VFP: TIPOMONY) -----------------------------------------------------
-- NOTA: con ON CONFLICT DO NOTHING este seed es idempotente y puede ejecutarse
-- DESPUÉS de la migración (database/migracion/*.sql): lo migrado tiene prioridad.
INSERT INTO tipo_moneda (id, descripcion, monto_dolar, principal, valor_nacional, nacional) VALUES
  ('0001', 'USD/BS', 1.0000000, TRUE,  1.0000000, FALSE),  -- divisa principal (USD)
  ('0002', 'BS',     1.0000000, FALSE, 1.0000000, TRUE)    -- moneda nacional (Bs)
ON CONFLICT (id) DO NOTHING;

-- Configuración global (VFP: DOLARMAIN) ---------------------------------------
INSERT INTO configuracion (id, nro_cliente, nombre_empresa, direccion, telefono, rif,
                           iva, moneda_principal_id, moneda_nacional_id,
                           tasa_activa, tasa_compra, tasa_venta, version)
VALUES (1, 23, 'MONRYS CAPITAL LLC', 'Caracas Vzla.', '0212 00000000', '123456789',
        16.00, '0001', '0002', 220.0000000, 285.0000000, 300.0000000, '2.0.0')
ON CONFLICT (id) DO NOTHING;

-- Plan contable (VFP: MAESTRO) ------------------------------------------------
INSERT INTO plan_contable (cuenta, descripcion) VALUES
  ('4.1.01.002', 'OTROS INGRESOS Y COMISIONES POR OPERACIONES DE TESORERIA'),
  ('4.1.03.001', 'INGRESO POR CREDITOS VIGENTES'),
  ('4.1.03.002', 'INGRESO POR CREDITOS REESTRUCTURADOS'),
  ('4.1.03.003', 'INGRESO POR CREDITOS VENCIDOS'),
  ('4.1.03.004', 'INGRESO POR RECUPERACION DE CREDITOS CASTIGADOS'),
  ('6.1.01.002', 'GASTOS COMISIONES BMN'),
  ('6.1.01.003', 'GASTOS COMISIONES BME'),
  ('6.1.01.004', 'PERDIDA EN COMPRA DE DIVISAS NO RECUPERADAS'),
  ('6.1.01.005', 'PERDIDA SALDOS EN MN NO COBRADOS'),
  ('6.1.01.006', 'OTROS GASTOS Y COMISIONES FINANCIERAS'),
  ('6.1.01.007', 'INTERESES POR CAPTACIONES DE TESORERIA'),
  ('6.1.01.008', 'INTERESES POR PRESTAMOS BANCARIOS'),
  ('6.1.01.009', 'COMISIONES POR LIQUIDACION DE PRESTAMOS'),
  ('6.1.02.001', 'SUELDOS Y SALARIOS AL PERSONAL')
ON CONFLICT DO NOTHING;

-- Codificación de ingresos (VFP: CODIINGRESO) ---------------------------------
INSERT INTO codificacion_ingreso (id, descripcion, cuenta_contable) VALUES
  ('00001', 'OTROS INGRESOS FINANCIEROS',                      '4.1.01.002'),
  ('00002', 'INGRESOS POR CAMBIO ENTRE DIVISAS',               '4.1.01.002'),
  ('00003', 'INGRESOS POR TRANSACCION',                        '4.1.01.002'),
  ('00004', 'RESULTADO EN OPERACIONES DE TESORERIA',           '4.1.01.002'),
  ('00005', 'INGRESO POR CREDITOS VIGENTES',                   '4.1.03.001'),
  ('00006', 'INGRESO POR CREDITOS REESTRUCTURADOS',            '4.1.03.002'),
  ('00007', 'INGRESO POR RECUPERACION DE CREDITOS CASTIGADOS', '4.1.03.003'),
  ('00008', 'INGRESO POR RECUPERACION DE CREDITOS CASTIGADOS', '4.1.03.004')
ON CONFLICT DO NOTHING;

-- Codificación de egresos (VFP: CODIEGRESO) -----------------------------------
INSERT INTO codificacion_egreso (id, descripcion, cuenta_contable) VALUES
  ('00001', 'COMISIONES BANCARIAS',                               '6.1.01.002'),
  ('00002', 'SUELDOS Y SALARIOS',                                 '6.1.02.001'),
  ('00003', 'GASTOS OPERATIVOS',                                  '6.1.01.006'),
  ('00004', 'INTERESES Y COMISIONES DE PRESTAMOS',                '6.1.01.008'),
  ('00005', 'PERDIDAS POR COMPRAS DE DIVISAS NO RECUPERADAS',     '6.1.01.004'),
  ('00006', 'COMISIONES BANCARIAS MNE',                           '6.1.01.003')
ON CONFLICT DO NOTHING;

-- Denominaciones de billetes para arqueo (VFP: CONODOL/CONONAC) ---------------
INSERT INTO denominacion (id, moneda_id, tipo, valor) VALUES
  ('D001', '0001', 'BILLETE',   1.00), ('D002', '0001', 'BILLETE',   2.00),
  ('D005', '0001', 'BILLETE',   5.00), ('D010', '0001', 'BILLETE',  10.00),
  ('D020', '0001', 'BILLETE',  20.00), ('D050', '0001', 'BILLETE',  50.00),
  ('D100', '0001', 'BILLETE', 100.00),
  ('N010', '0002', 'BILLETE',  10.00), ('N020', '0002', 'BILLETE',  20.00),
  ('N050', '0002', 'BILLETE',  50.00), ('N100', '0002', 'BILLETE', 100.00),
  ('N200', '0002', 'BILLETE', 200.00), ('N500', '0002', 'BILLETE', 500.00)
ON CONFLICT DO NOTHING;

-- Bancos observados en datos de producción (VFP: BANCOS) ----------------------
INSERT INTO banco (id, nombre, nacional) VALUES
  ('00001', 'BANESCO',           1),
  ('00002', 'PROVINCIAL',        1),
  ('00003', 'EXTERIOR',          1),
  ('00005', 'BANCO PROVINCIAL',  1),
  ('00006', 'BANCO MERCANTIL',   1),
  ('00007', 'BANCO DE VENEZUELA',1),
  ('00008', 'WELLS FARGO',       2),
  ('00009', 'CITI BANK',         2)
ON CONFLICT DO NOTHING;

-- ----------------------------------------------------------------------------
-- SEGURIDAD: roles y permisos (reemplaza el string posicional de VFP)
-- ----------------------------------------------------------------------------
INSERT INTO rol (nombre, descripcion) VALUES
  ('ADMIN',      'Acceso total al sistema'),
  ('GERENTE',    'Consulta gerencial, reportes y cierre de caja'),
  ('CAJERO',     'Operación de punto de caja: cobros, pagos, recibos')
ON CONFLICT DO NOTHING;

INSERT INTO permiso (clave, descripcion) VALUES
  ('clientes.ver',        'Ver clientes'),
  ('clientes.crear',      'Crear clientes'),
  ('clientes.editar',     'Editar clientes'),
  ('operaciones.ver',     'Ver compras/ventas'),
  ('operaciones.crear',   'Crear compras/ventas'),
  ('operaciones.anular',  'Anular compras/ventas'),
  ('caja.ver',            'Ver movimientos de caja'),
  ('caja.mover',          'Registrar ingresos/egresos/cobros/pagos'),
  ('caja.cerrar',         'Realizar cierre de caja'),
  ('caja.anular',         'Anular movimientos de caja'),
  ('bancos.ver',          'Ver bancos y cuentas'),
  ('bancos.gestionar',    'Gestionar bancos y cuentas'),
  ('transferencias.ver',  'Ver transferencias'),
  ('transferencias.crear','Crear transferencias'),
  ('custodios.ver',       'Ver custodios'),
  ('custodios.mover',     'Entregar/recibir custodio'),
  ('tasas.gestionar',     'Modificar tasas de cambio'),
  ('reportes.gerenciales','Ver reportes gerenciales'),
  ('usuarios.gestionar',  'Gestionar usuarios y permisos'),
  ('config.gestionar',    'Modificar configuración del sistema')
ON CONFLICT DO NOTHING;

-- ADMIN: todos los permisos
INSERT INTO rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM rol r CROSS JOIN permiso p WHERE r.nombre = 'ADMIN'
ON CONFLICT DO NOTHING;

-- GERENTE: todo excepto gestión de usuarios/config
INSERT INTO rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM rol r CROSS JOIN permiso p
WHERE r.nombre = 'GERENTE'
  AND p.clave NOT IN ('usuarios.gestionar', 'config.gestionar')
ON CONFLICT DO NOTHING;

-- CAJERO: operación de punto de caja + consulta
INSERT INTO rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM rol r CROSS JOIN permiso p
WHERE r.nombre = 'CAJERO'
  AND p.clave IN ('clientes.ver','clientes.crear','operaciones.ver','operaciones.crear',
                  'caja.ver','caja.mover','bancos.ver','transferencias.ver','custodios.ver')
ON CONFLICT DO NOTHING;

-- ----------------------------------------------------------------------------
-- USUARIO ADMINISTRADOR INICIAL
-- clave: admin123  (hash PBKDF2-SHA256, 100k iteraciones — cambiar en primer acceso)
-- ----------------------------------------------------------------------------
INSERT INTO usuario (id, nombre, clave_hash, tipo, activo) VALUES
  ('ADMIN', 'Administrador',
   'PBKDF2|100000|myV3AhkBdYBiP8Y8G9iwgA==|QsArkqwlgep11ncbkVUmCEbJy0LEcuHVVHOusO4Bnfo=',
   1, TRUE)
ON CONFLICT (id) DO NOTHING;

INSERT INTO usuario_rol (usuario_id, rol_id)
SELECT 'ADMIN', id FROM rol WHERE nombre = 'ADMIN'
ON CONFLICT DO NOTHING;
