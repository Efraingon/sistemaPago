/**
 * Produccion (build de Vercel).
 *
 * apiUrl = '' => las peticiones salen como '/api/...' contra el mismo origen
 * y Vercel las reenvia a Koyeb mediante el rewrite de vercel.json.
 * Asi no hay CORS ni hay que reconstruir el frontend si cambia la URL del API.
 */
export const environment = {
  production: true,
  apiUrl: '',
};
