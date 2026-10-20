-- 013_usuario: 3 filas
SET search_path TO monrys, public;
INSERT INTO usuario (id,nombre,clave_hash,tipo,historico_finanzas,ruta_pantalla,activo,bloqueado) VALUES('0000000001','SUPERVISOR','PBKDF2|100000|bWlncmF0ZQ==|bWlncmF0ZQ==',1,'2019','c:\Mundodolar\fondosys\BK31X.BMP',FALSE,FALSE),('0000000002','INFO-SYSTEMS','PBKDF2|100000|bWlncmF0ZQ==|bWlncmF0ZQ==',1,'2019','c:\Mundodolar\fondosys\BK31X.BMP',FALSE,FALSE),('0000000003','JOSE MONROY','PBKDF2|100000|bWlncmF0ZQ==|bWlncmF0ZQ==',1,'2024','c:\Mundodolar\fondosys\BK31X.BMP',FALSE,FALSE);
