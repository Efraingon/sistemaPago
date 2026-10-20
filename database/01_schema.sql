-- ============================================================================
-- MONRYS CAPITAL LLC — Sistema de Pagos y Casa de Cambio
-- Esquema PostgreSQL 18 (migrado desde Visual FoxPro 6)
-- Orden de carga completo:
--   01_schema.sql → 03_functions.sql → migracion\*.sql (datos reales) → 02_seed.sql
--   (el seed va al final: es idempotente y solo completa lo que VFP no trae)
-- ============================================================================

DROP SCHEMA IF EXISTS monrys CASCADE;
CREATE SCHEMA monrys;
SET search_path TO monrys, public;

-- ----------------------------------------------------------------------------
-- DOMINIOS (antes CREATE TYPE ... AS ENUM → ahora VARCHAR + CHECK)
-- ----------------------------------------------------------------------------
-- Motivo: Npgsql envía los valores como parámetros VARCHAR y PostgreSQL NO los
-- convierte implícitamente a enum nativo (error 42804 al insertar), mientras que
-- los literales del ETL sí entraban. Con VARCHAR + CHECK se conserva la validez
-- de los dominios y el modelo C# (propiedades string) encaja sin fricción.
--
-- ORIGEN de movimientos de caja (códigos VFP de 2 letras): ver CHECK en la tabla caja.
-- ORIGEN de transferencias bancarias: ver CHECK en la tabla transferencia.

-- ----------------------------------------------------------------------------
-- MAESTROS / CONFIGURACIÓN
-- ----------------------------------------------------------------------------
CREATE TABLE configuracion (            -- VFP: DOLARMAIN (fila única)
  id                      SMALLINT PRIMARY KEY DEFAULT 1 CHECK (id = 1),
  nro_cliente             INTEGER,
  nombre_empresa          VARCHAR(100) NOT NULL,
  direccion               VARCHAR(120),
  telefono                VARCHAR(50),
  fax                     VARCHAR(20),
  rif                     VARCHAR(20),
  logo                    BYTEA,
  iva                     NUMERIC(6,2) NOT NULL DEFAULT 0,
  impute_deba             VARCHAR(1),
  porc_deba               NUMERIC(6,2) DEFAULT 0,
  agente_retencion        BOOLEAN NOT NULL DEFAULT FALSE,
  serial_contable         VARCHAR(20),
  reteiva_se              NUMERIC(6,2),
  ruta_data               VARCHAR(80),
  moneda_principal_id     VARCHAR(4) NOT NULL,   -- FK a tipo_moneda (USD)
  moneda_nacional_id      VARCHAR(4) NOT NULL,   -- FK a tipo_moneda (Bs)
  tasa_activa             NUMERIC(14,7) NOT NULL DEFAULT 0,
  tasa_compra             NUMERIC(14,7) NOT NULL DEFAULT 0,
  tasa_venta              NUMERIC(14,7) NOT NULL DEFAULT 0,
  version                 VARCHAR(10),
  actualizado_en          TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE tipo_moneda (              -- VFP: TIPOMONY
  id                      VARCHAR(4) PRIMARY KEY,          -- '0001','0002'
  descripcion             VARCHAR(10) NOT NULL,            -- 'USD/BS','BS'
  monto_dolar             NUMERIC(14,7) NOT NULL DEFAULT 1,
  principal               BOOLEAN NOT NULL DEFAULT FALSE,  -- divisa dura (USD)
  valor_nacional          NUMERIC(14,7) NOT NULL DEFAULT 1,
  nacional                BOOLEAN NOT NULL DEFAULT FALSE,  -- moneda local (Bs)
  activa                  BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE calend_tasa (              -- VFP: CALENDTASA
  fecha                   DATE PRIMARY KEY,
  nombre_dia              VARCHAR(10),
  tasa                    NUMERIC(14,8),
  tasa_compra             NUMERIC(14,8),
  tasa_venta              NUMERIC(14,8),
  creado_en               TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE cliente (                  -- VFP: CLIENTES
  id                      VARCHAR(10) PRIMARY KEY,         -- ID_CLIE legacy
  persona                 SMALLINT NOT NULL,               -- 1=natural, 2=jurídica
  razon_social            VARCHAR(200),
  nombres                 VARCHAR(50),
  apellidos               VARCHAR(50),
  rif                     VARCHAR(15),
  cedula                  VARCHAR(15),
  direccion               VARCHAR(200),
  telefonos               VARCHAR(100),
  email                   VARCHAR(100),
  observaciones           VARCHAR(200),
  cuenta_contable         VARCHAR(10),
  activo                  BOOLEAN NOT NULL DEFAULT TRUE,
  creado_en               TIMESTAMP NOT NULL DEFAULT now(),
  actualizado_en          TIMESTAMP NOT NULL DEFAULT now(),
  CONSTRAINT ck_persona CHECK (persona IN (1,2))
);
CREATE INDEX ix_cliente_nombre ON cliente (razon_social, apellidos, nombres);
CREATE INDEX ix_cliente_cedula ON cliente (cedula);

CREATE TABLE centro_costo (             -- VFP: CENTROSCLIE
  id                      BIGSERIAL PRIMARY KEY,
  cliente_id              VARCHAR(10) NOT NULL REFERENCES cliente(id),
  descripcion             VARCHAR(80) NOT NULL,
  principal               BOOLEAN NOT NULL DEFAULT FALSE   -- VFP: PPAL='X'
);
CREATE UNIQUE INDEX ux_centro_ppal ON centro_costo (cliente_id) WHERE principal;

CREATE TABLE banco (                    -- VFP: BANCOS
  id                      VARCHAR(5) PRIMARY KEY,
  nombre                  VARCHAR(100) NOT NULL,
  direccion               VARCHAR(250),
  telefonos               VARCHAR(50),
  fax                     VARCHAR(50),
  correo                  VARCHAR(50),
  contacto                VARCHAR(50),
  nacional                SMALLINT NOT NULL DEFAULT 1,      -- 1=nacional, 2=internacional
  saldo                   NUMERIC(14,2) NOT NULL DEFAULT 0, -- cacheado (recalcular)
  cuenta_contable         VARCHAR(10),
  activo                  BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE INDEX ix_banco_nacional ON banco (nacional);

CREATE TABLE cuenta_banco (             -- VFP: CTAS / CTASBANCO (cuentas de la casa)
  id                      BIGSERIAL PRIMARY KEY,
  banco_id                VARCHAR(5) NOT NULL REFERENCES banco(id),
  numero_cuenta           VARCHAR(20) NOT NULL,
  tipo                    NUMERIC(1,0),
  debitos                 NUMERIC(14,2) NOT NULL DEFAULT 0, -- cacheados
  creditos                NUMERIC(14,2) NOT NULL DEFAULT 0,
  saldo                   NUMERIC(14,2) NOT NULL DEFAULT 0,
  cuenta_contable         VARCHAR(10),
  activa                  BOOLEAN NOT NULL DEFAULT TRUE,
  UNIQUE (banco_id, numero_cuenta)
);

CREATE TABLE cuenta_cliente (           -- VFP: CTASCLIE (cuentas bancarias de clientes)
  id                      BIGSERIAL PRIMARY KEY,
  banco_id                VARCHAR(5) NOT NULL REFERENCES banco(id),
  cliente_id              VARCHAR(10) NOT NULL REFERENCES cliente(id),
  numero_cuenta           VARCHAR(20) NOT NULL,
  cuenta_contable         VARCHAR(10),
  activa                  BOOLEAN NOT NULL DEFAULT TRUE,
  UNIQUE (banco_id, cliente_id, numero_cuenta)
);

CREATE TABLE plan_contable (            -- VFP: MAESTRO
  cuenta                  VARCHAR(10) PRIMARY KEY,
  descripcion             VARCHAR(100) NOT NULL
);

CREATE TABLE codificacion_ingreso (     -- VFP: CODIINGRESO
  id                      VARCHAR(5) PRIMARY KEY,
  descripcion             VARCHAR(50) NOT NULL,
  cuenta_contable         VARCHAR(10) REFERENCES plan_contable(cuenta)
);

CREATE TABLE codificacion_egreso (      -- VFP: CODIEGRESO
  id                      VARCHAR(5) PRIMARY KEY,
  descripcion             VARCHAR(50) NOT NULL,
  cuenta_contable         VARCHAR(10) REFERENCES plan_contable(cuenta)
);

CREATE TABLE denominacion (             -- VFP: CONODOL / CONONAC (billetes; para arqueo)
  id                      VARCHAR(4) PRIMARY KEY,
  moneda_id               VARCHAR(4) NOT NULL REFERENCES tipo_moneda(id),
  tipo                    VARCHAR(10) NOT NULL DEFAULT 'BILLETE',
  valor                   NUMERIC(14,2) NOT NULL,
  activa                  BOOLEAN NOT NULL DEFAULT TRUE
);

-- ----------------------------------------------------------------------------
-- SEGURIDAD
-- ----------------------------------------------------------------------------
CREATE TABLE usuario (                  -- VFP: USUARIOS (mejoras: hash + estado)
  id                      VARCHAR(10) PRIMARY KEY,
  nombre                  VARCHAR(50) NOT NULL,
  clave_hash              VARCHAR(200) NOT NULL,   -- PBKDF2 (salt+hash); nunca texto plano
  tipo                    SMALLINT NOT NULL DEFAULT 2,  -- 1=Administrador
  historico_finanzas      VARCHAR(4),
  ruta_pantalla           VARCHAR(100),
  activo                  BOOLEAN NOT NULL DEFAULT TRUE,
  bloqueado               BOOLEAN NOT NULL DEFAULT FALSE,
  ultimo_acceso           TIMESTAMP,
  creado_en               TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE rol (                      -- NUEVO (reemplaza permisos posicionales VFP)
  id                      SERIAL PRIMARY KEY,
  nombre                  VARCHAR(50) NOT NULL UNIQUE,
  descripcion             VARCHAR(200)
);

CREATE TABLE permiso (                  -- NUEVO (reemplaza USUARIOS.ACCESO string)
  id                      SERIAL PRIMARY KEY,
  clave                   VARCHAR(60) NOT NULL UNIQUE,  -- ej. 'clientes.crear'
  descripcion             VARCHAR(100) NOT NULL
);

CREATE TABLE rol_permiso (
  rol_id                  INTEGER NOT NULL REFERENCES rol(id) ON DELETE CASCADE,
  permiso_id              INTEGER NOT NULL REFERENCES permiso(id) ON DELETE CASCADE,
  PRIMARY KEY (rol_id, permiso_id)
);

CREATE TABLE usuario_rol (
  usuario_id              VARCHAR(10) NOT NULL REFERENCES usuario(id) ON DELETE CASCADE,
  rol_id                  INTEGER NOT NULL REFERENCES rol(id) ON DELETE CASCADE,
  PRIMARY KEY (usuario_id, rol_id)
);

CREATE TABLE bitacora (                 -- VFP: BITACORA (auditoría)
  id                      BIGSERIAL PRIMARY KEY,
  fecha                   TIMESTAMP NOT NULL DEFAULT now(),
  usuario_id              VARCHAR(10),
  tipo                    INTEGER,
  accion                  VARCHAR(200) NOT NULL,
  observaciones           VARCHAR(200),
  equipo                  VARCHAR(50),
  tabla                   VARCHAR(60),
  registro_id             VARCHAR(30),
  datos_previos           JSONB
);
CREATE INDEX ix_bitacora_fecha ON bitacora (fecha DESC);
CREATE INDEX ix_bitacora_usuario ON bitacora (usuario_id, fecha DESC);

CREATE TABLE log_error (                -- VFP: VER_ERROR
  id                      BIGSERIAL PRIMARY KEY,
  fecha                   TIMESTAMP NOT NULL DEFAULT now(),
  usuario_id              VARCHAR(10),
  nombre_usuario          VARCHAR(50),
  mensaje                 TEXT,
  programa                VARCHAR(100),
  linea                   INTEGER,
  equipo                  VARCHAR(50),
  version                 VARCHAR(10)
);

-- ----------------------------------------------------------------------------
-- OPERACIONES DE COMPRA/VENTA DE DIVISAS
-- ----------------------------------------------------------------------------
CREATE TABLE compra (                   -- VFP: COMPRAS
  id                      VARCHAR(16) PRIMARY KEY,   -- '2026000000000001' = ejercicio + 12 dígitos
  ejercicio               CHAR(4) NOT NULL,
  cliente_id              VARCHAR(10) NOT NULL REFERENCES cliente(id),
  fecha                   DATE NOT NULL,
  moneda_divisa_id        VARCHAR(4) NOT NULL REFERENCES tipo_moneda(id),
  moneda_nac_id           VARCHAR(4) NOT NULL REFERENCES tipo_moneda(id),
  mto_x_divisa            NUMERIC(14,2),   -- VFP: MTOXD1
  mto_x_nac               NUMERIC(14,2),   -- VFP: MTOXD2
  tasa                    NUMERIC(14,7) NOT NULL,    -- VFP: VALONACIO
  cantidad                NUMERIC(14,2) NOT NULL,    -- cantidad de divisa
  monto_bs                NUMERIC(14,2) NOT NULL,    -- ROUND(cantidad*tasa,2)
  pagado_bs               NUMERIC(14,2) NOT NULL DEFAULT 0,
  saldo_bs                NUMERIC(14,2) NOT NULL DEFAULT 0,
  recibido_divisa         NUMERIC(14,2) NOT NULL DEFAULT 0,  -- RECIBIDODV
  por_recibir             NUMERIC(14,2) NOT NULL DEFAULT 0,  -- PORECIB
  observaciones           VARCHAR(80),
  centro_costo            VARCHAR(80),
  estado                  VARCHAR(9) NOT NULL DEFAULT 'ACTIVO'
                            CHECK (estado IN ('ACTIVO','ANULADO')),
  anulado_en              TIMESTAMP,
  anulado_motivo          VARCHAR(200),
  creado_en               TIMESTAMP NOT NULL DEFAULT now(),
  actualizado_en          TIMESTAMP NOT NULL DEFAULT now()
);
CREATE INDEX ix_compra_cliente ON compra (cliente_id, fecha DESC);
CREATE INDEX ix_compra_fecha ON compra (fecha DESC);

CREATE TABLE venta (                    -- VFP: VENTAS (espejo de compra)
  id                      VARCHAR(16) PRIMARY KEY,
  ejercicio               CHAR(4) NOT NULL,
  cliente_id              VARCHAR(10) NOT NULL REFERENCES cliente(id),
  fecha                   DATE NOT NULL,
  moneda_divisa_id        VARCHAR(4) NOT NULL REFERENCES tipo_moneda(id),
  moneda_nac_id           VARCHAR(4) NOT NULL REFERENCES tipo_moneda(id),
  mto_x_divisa            NUMERIC(14,2),
  mto_x_nac               NUMERIC(14,2),
  tasa                    NUMERIC(14,7) NOT NULL,
  cantidad                NUMERIC(14,2) NOT NULL,
  monto_bs                NUMERIC(14,2) NOT NULL,
  pagado_bs               NUMERIC(14,2) NOT NULL DEFAULT 0,
  saldo_bs                NUMERIC(14,2) NOT NULL DEFAULT 0,
  recibido_divisa         NUMERIC(14,2) NOT NULL DEFAULT 0,  -- RECIBIDODV
  por_recibir             NUMERIC(14,2) NOT NULL DEFAULT 0,  -- PORECIB
  observaciones           VARCHAR(80),
  centro_costo            VARCHAR(80),
  estado                  VARCHAR(9) NOT NULL DEFAULT 'ACTIVO'
                            CHECK (estado IN ('ACTIVO','ANULADO')),
  anulado_en              TIMESTAMP,
  anulado_motivo          VARCHAR(200),
  creado_en               TIMESTAMP NOT NULL DEFAULT now(),
  actualizado_en          TIMESTAMP NOT NULL DEFAULT now()
);
CREATE INDEX ix_venta_cliente ON venta (cliente_id, fecha DESC);
CREATE INDEX ix_venta_fecha ON venta (fecha DESC);

-- Cuotas: recepción de divisa en compras (VFP: COMPRECIB) / cobro en ventas (VENRECIB)
CREATE TABLE operacion_recepcion (
  id                      BIGSERIAL PRIMARY KEY,
  tipo_op                 CHAR(1) NOT NULL CHECK (tipo_op IN ('C','V')),  -- Compra/Venta
  operacion_id            VARCHAR(16) NOT NULL,
  nroper                  INTEGER NOT NULL,           -- correlativo dentro de la operación
  ejercicio               CHAR(4) NOT NULL,
  fecha                   DATE NOT NULL,
  cantidad                NUMERIC(14,2) NOT NULL,     -- divisa recibida/cobrada
  por_recibir             NUMERIC(14,2),              -- PORECIB
  recibo                  VARCHAR(20),
  recibo2                 VARCHAR(20),
  banco_credito_id        VARCHAR(5) REFERENCES banco(id),
  banco_debito_id         VARCHAR(5) REFERENCES banco(id),
  cta_credito             VARCHAR(20),
  cta_debito              VARCHAR(20),
  tipo_pago               VARCHAR(15),
  compensado              BOOLEAN NOT NULL DEFAULT FALSE,  -- COMPENSAC
  de_caja                 BOOLEAN NOT NULL DEFAULT FALSE,  -- DECAJA
  de_banco                BOOLEAN NOT NULL DEFAULT FALSE,  -- DEBANCO
  anulado_en              TIMESTAMP,
  creado_en               TIMESTAMP NOT NULL DEFAULT now()
  -- NOTA: en VFP (tipo_op, operacion_id, nroper) NO es único: el origen real tiene
  -- ~5.600 repeticiones legítimas en recepciones. Se indexa sin restricción.
);
CREATE INDEX ix_oprec_op ON operacion_recepcion (tipo_op, operacion_id);
CREATE INDEX ix_oprec_nroper ON operacion_recepcion (tipo_op, operacion_id, nroper);

-- Cuotas: pago de compras (VFP: COMPAGAR) / pago de ventas (VENPAGAR)
CREATE TABLE operacion_pago (
  id                      BIGSERIAL PRIMARY KEY,
  tipo_op                 CHAR(1) NOT NULL CHECK (tipo_op IN ('C','V')),
  operacion_id            VARCHAR(16) NOT NULL,
  nroper                  INTEGER NOT NULL,
  ejercicio               CHAR(4) NOT NULL,
  fecha                   DATE NOT NULL,
  monto                   NUMERIC(14,2) NOT NULL,     -- monto en Bs
  banco_debito_id         VARCHAR(5) REFERENCES banco(id),
  cta_debito              VARCHAR(20),
  banco_credito_id        VARCHAR(5) REFERENCES banco(id),
  cta_credito             VARCHAR(20),
  recibo                  VARCHAR(20),
  tipo_pago               VARCHAR(15),
  saldo                   NUMERIC(14,2),
  anulado_en              TIMESTAMP,
  creado_en               TIMESTAMP NOT NULL DEFAULT now()
  -- NOTA: ídem recepciones — (tipo_op, operacion_id, nroper) se repite ~7.800 veces
  -- en el origen VFP; aquí solo se indexa para consultar pagos por operación.
);
CREATE INDEX ix_oppag_op ON operacion_pago (tipo_op, operacion_id);
CREATE INDEX ix_oppag_nroper ON operacion_pago (tipo_op, operacion_id, nroper);

-- ----------------------------------------------------------------------------
-- CAJA, CUSTODIA Y TESORERÍA
-- ----------------------------------------------------------------------------
CREATE TABLE caja (                     -- VFP: CAJA (libro de caja)
  id                      VARCHAR(10) NOT NULL,       -- correlativo 10 díg por año
  ejercicio               CHAR(4) NOT NULL,
  origen                  VARCHAR(3) NOT NULL
                            CHECK (origen IN ('IN','EG','CO','VE','RE','EN','C1','C2',
                                              'DE','DS','IB','EB','T5','T6')),
  fecha                   DATE NOT NULL,
  recibo                  VARCHAR(20) NOT NULL,       -- correlativo global 20 díg
  monto                   NUMERIC(14,2) NOT NULL,
  descripcion             VARCHAR(100),
  observaciones           VARCHAR(200),
  moneda_id               VARCHAR(4) REFERENCES tipo_moneda(id),
  cliente_id              VARCHAR(10) REFERENCES cliente(id),
  compra_id               VARCHAR(16),
  venta_id                VARCHAR(16),
  banco_id                VARCHAR(5) REFERENCES banco(id),
  cuenta_tran             VARCHAR(20),
  numero_tran             VARCHAR(20),
  tasa                    NUMERIC(14,7),
  usd                     NUMERIC(14,2),
  monto_bs                NUMERIC(14,2),
  codi_ingreso_id         VARCHAR(5) REFERENCES codificacion_ingreso(id),
  codi_egreso_id          VARCHAR(5) REFERENCES codificacion_egreso(id),
  centro_costo            VARCHAR(80),
  cierre_fecha            DATE,           -- VFP: FECHACIER
  cierre_id               VARCHAR(15),    -- VFP: ID_CIERR
  estado                  VARCHAR(9) NOT NULL DEFAULT 'ACTIVO'
                            CHECK (estado IN ('ACTIVO','ANULADO')),
  anulado_en              TIMESTAMP,
  anulado_motivo          VARCHAR(200),
  creado_en               TIMESTAMP NOT NULL DEFAULT now(),
  PRIMARY KEY (ejercicio, id),
  UNIQUE (recibo)
);
CREATE INDEX ix_caja_fecha ON caja (fecha DESC);
CREATE INDEX ix_caja_cliente ON caja (cliente_id, fecha DESC);
CREATE INDEX ix_caja_origen ON caja (origen, fecha DESC);
CREATE INDEX ix_caja_cierre ON caja (cierre_fecha) WHERE cierre_fecha IS NOT NULL;
CREATE UNIQUE INDEX ux_caja_id_anual ON caja (ejercicio, id);

CREATE TABLE compensacion (             -- VFP: COMPENSAC (custodia de clientes)
  id                      VARCHAR(10) NOT NULL,       -- correlativo 10 díg por año
  ejercicio               CHAR(4) NOT NULL,
  fecha                   DATE NOT NULL,
  descripcion             VARCHAR(50),
  cliente_id              VARCHAR(10) NOT NULL REFERENCES cliente(id),
  moneda_id               VARCHAR(4) NOT NULL REFERENCES tipo_moneda(id),
  abonos                  NUMERIC(14,2) NOT NULL DEFAULT 0,  -- entra a custodia
  retiros                 NUMERIC(14,2) NOT NULL DEFAULT 0,  -- sale de custodia
  recibo_tran             VARCHAR(20),
  cta_tran                VARCHAR(20),
  centro_costo            VARCHAR(80),
  observaciones           VARCHAR(30),
  reverso_de              VARCHAR(20),   -- anulación por fila reverso (legado)
  creado_en               TIMESTAMP NOT NULL DEFAULT now(),
  PRIMARY KEY (ejercicio, id)
);
CREATE INDEX ix_comp_clie ON compensacion (cliente_id, moneda_id, fecha);

CREATE TABLE custodio (                 -- VFP: CUSTODIO (saldos cacheados)
  cliente_id              VARCHAR(10) NOT NULL REFERENCES cliente(id),
  moneda_id               VARCHAR(4) NOT NULL REFERENCES tipo_moneda(id),
  disponible              NUMERIC(14,2) NOT NULL DEFAULT 0,   -- divisa
  disponible_bs           NUMERIC(14,2) NOT NULL DEFAULT 0,  -- VFP: DISPONIBB
  actualizado_en          TIMESTAMP NOT NULL DEFAULT now(),
  PRIMARY KEY (cliente_id, moneda_id)
);

CREATE TABLE transferencia (            -- VFP: CTASTRAN (libro de transferencias)
  id                      BIGSERIAL PRIMARY KEY,
  origen                  VARCHAR(2) NOT NULL
                            CHECK (origen IN ('CO','VE','CU','DP','RT','T1','T2','T3','T4','T5','T6')),
  tipo_tran               CHAR(2),
  fecha                   DATE NOT NULL,
  numero_tran             VARCHAR(20),
  cuenta_tran             VARCHAR(20),
  beneficiario            VARCHAR(100),
  monto                   NUMERIC(14,2) NOT NULL,
  cliente_id              VARCHAR(10) REFERENCES cliente(id),
  motivo                  VARCHAR(100),
  compra_id               VARCHAR(16),
  venta_id                VARCHAR(16),
  moneda_id               VARCHAR(4) REFERENCES tipo_moneda(id),
  banco_id                VARCHAR(5) REFERENCES banco(id),
  cuenta_clie             VARCHAR(20),
  de_caja                 BOOLEAN NOT NULL DEFAULT FALSE,
  tasa                    NUMERIC(14,7),
  usd                     NUMERIC(14,2),
  monto_bs                NUMERIC(14,2),
  codi_ingreso_id         VARCHAR(5) REFERENCES codificacion_ingreso(id),
  codi_egreso_id          VARCHAR(5) REFERENCES codificacion_egreso(id),
  estado                  VARCHAR(9) NOT NULL DEFAULT 'ACTIVO'
                            CHECK (estado IN ('ACTIVO','ANULADO')),
  anulado_en              TIMESTAMP,
  anulado_motivo          VARCHAR(200),
  creado_en               TIMESTAMP NOT NULL DEFAULT now()
);
CREATE INDEX ix_tran_fecha ON transferencia (fecha DESC);
CREATE INDEX ix_tran_banco ON transferencia (banco_id, fecha DESC);
CREATE INDEX ix_tran_cliente ON transferencia (cliente_id, fecha DESC);

-- ----------------------------------------------------------------------------
-- PARÁMETROS DE CORRELATIVOS (numeración por año, reemplaza MAX()+1 de VFP)
-- ----------------------------------------------------------------------------
CREATE TABLE correlativo (
  tabla                   VARCHAR(30) NOT NULL,
  ejercicio               VARCHAR(6) NOT NULL,        -- año '2026' o 'GLOBAL'
  ultimo                  BIGINT NOT NULL DEFAULT 0,
  PRIMARY KEY (tabla, ejercicio)
);

-- ============================================================================
-- FIN DEL ESQUEMA
-- ============================================================================
