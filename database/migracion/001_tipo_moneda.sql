-- 001_tipo_moneda: 2 filas
SET search_path TO monrys, public;
INSERT INTO tipo_moneda (id,descripcion,monto_dolar,principal,valor_nacional,nacional,activa) VALUES('0001','USD/BS',1.0,TRUE,1.0,FALSE,TRUE),('0002','BS',1.0,FALSE,1.0,TRUE,TRUE);
