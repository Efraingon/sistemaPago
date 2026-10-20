import { test, expect } from '@playwright/test';

/**
 * Prueba E2E del reporte "Custodios — resumen por cliente"
 *
 * Requisitos previos:
 * - Backend ejecutándose en http://localhost:5100 (modo demo con SQLite)
 * - Frontend ejecutándose en http://localhost:4200
 *
 * Ejecutar con: npx playwright test e2e/reportes-custodios.spec.ts
 */

test.describe.configure({ mode: 'serial' });

test.describe('Centro de Reportes — Custodios resumen por cliente', () => {
  const BASE_URL = 'http://localhost:4200';
  const USUARIO = 'ADMIN';
  const CLAVE = 'admin123';

  async function login(page: any) {
    await page.goto(`${BASE_URL}/login`);
    await page.fill('#usuario', USUARIO);
    await page.fill('#clave', CLAVE);
    await page.click('button[type="submit"]');
    await page.waitForURL('**/dashboard', { timeout: 10000 }).catch(() => {});
  }

  test('debe mostrar datos del reporte custodios-todos', async ({ page }) => {
    // 1. Login
    await login(page);

    // 2. Ir directamente al visor del informe custodios-todos
    await page.goto(`${BASE_URL}/reportes/informe/custodios-todos`);
    await page.waitForURL('**/reportes/informe/custodios-todos', { timeout: 10000 });

    // 3. El reporte tiene parámetros opcionales (desde/hasta), ejecutar sin ellos
    const btnEjecutar = page.locator('button:has-text("Ejecutar informe")');
    await expect(btnEjecutar).toBeVisible({ timeout: 10000 });
    await btnEjecutar.click();

    // 4. Esperar a que cargue el documento imprimible (.papel)
    await page.waitForSelector('.papel', { timeout: 20000 });

    // 5. Verificar que hay filas de datos en la tabla
    const filas = page.locator('.doc-table tbody tr');
    await expect(filas.first()).toBeVisible({ timeout: 10000 });

    // 6. Verificar encabezado de empresa
    const encabezado = page.locator('.papel-empresa strong');
    await expect(encabezado).toContainText('MONRYS CAPITAL');

    // 7. Verificar que hay datos (al menos una fila con contenido)
    const primeraFila = filas.first();
    const textoFila = await primeraFila.textContent();
    expect(textoFila?.trim().length).toBeGreaterThan(0);
  });

  test('debe tener botón de imprimir en el reporte custodios-todos', async ({ page }) => {
    // 1. Login
    await login(page);

    // 2. Ir directamente al visor del informe
    await page.goto(`${BASE_URL}/reportes/informe/custodios-todos`);
    await page.waitForURL('**/reportes/informe/custodios-todos', { timeout: 10000 });

    // 3. Ejecutar el reporte
    const btnEjecutar = page.locator('button:has-text("Ejecutar informe")');
    await expect(btnEjecutar).toBeVisible({ timeout: 10000 });
    await btnEjecutar.click();

    await page.waitForSelector('.papel', { timeout: 20000 });

    // 4. Verificar que el botón Imprimir está presente
    const btnImprimir = page.locator('button:has-text("Imprimir")');
    await expect(btnImprimir).toBeVisible();

    // 5. Verificar que el documento tiene filas de datos
    const filas = page.locator('.doc-table tbody tr');
    await expect(filas.first()).toBeVisible({ timeout: 10000 });
  });
});
