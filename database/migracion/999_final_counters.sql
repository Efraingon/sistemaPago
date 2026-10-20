SET search_path TO monrys, public;
INSERT INTO correlativo(tabla,ejercicio,ultimo) VALUES ('CAJA_ID','2022',1922),('CAJA_ID','2023',1933),('CAJA_ID','2024',2594),('CAJA_ID','2025',2682),('CAJA_RECIBO','GLOBAL',9131),('CLIENTE','GLOBAL',393),('COMPENSACION','2022',800),('COMPENSACION','2023',1328),('COMPENSACION','2024',2129),('COMPENSACION','2025',4008),('COMPRA','2022',1340),('COMPRA','2023',1528),('COMPRA','2024',2699),('COMPRA','2025',3500),('VENTA','2022',538),('VENTA','2023',562),('VENTA','2024',633),('VENTA','2025',549)
ON CONFLICT (tabla,ejercicio) DO UPDATE SET ultimo = GREATEST(correlativo.ultimo, EXCLUDED.ultimo);
-- Semillas generadas por el ETL; coinciden con las claves que pide la
-- API (CorrelativoService): COMPRA/VENTA/CAJA_ID/COMPENSACION por anyo y
-- CAJA_RECIBO/CLIENTE con ejercicio 'GLOBAL'.
