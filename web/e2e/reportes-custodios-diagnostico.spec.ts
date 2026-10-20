import { test, expect } from '@playwright/test';

/**
 * Diagnóstico del reporte "Custodios — resumen por cliente"
 * Verifica qué se muestra al navegar al visor del informe.
 */

test.describe.configure({ mode: 'serial' });

test.describe('Diagnóstico: informe custodios-todos', () => {
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

  test('debe navegar al visor y mostrar el estado actual', async ({ page }) => {
    await login(page);

    // Ir directamente al visor del informe
    await page.goto(`${BASE_URL}/reportes/informe/custodios-todos`);
    await page.waitForURL('**/reportes/informe/custodios-todos', { timeout: 10000 });

    // Esperar un poco para que Angular renderice
    await page.waitForTimeout(3000);

    // Ver qué elementos están presentes
    const cuerpo = page.locator('body');
    const html = await cuerpo.innerHTML();

    // Buscar indicadores clave
    const tienePapel = await page.locator('.papel').count();
    const tieneFormulario = await page.locator('.params-bar').count();
    const tieneError = await page.locator('.empty-state').count();
    const tieneCargando = await page.locator('.loading-panel').count();
    const btnEjecutar = await page.locator('button:has-text("Ejecutar informe")').count();
    const btnImprimir = await page.locator('button:has-text("Imprimir")').count();

    console.log('=== DIAGNÓSTICO custodios-todos ===');
    console.log('tiene .papel:', tienePapel);
    console.log('tiene .params-bar:', tieneFormulario);
    console.log('tiene .empty-state:', tieneError);
    console.log('tiene .loading-panel:', tieneCargando);
    console.log('botón "Ejecutar informe":', btnEjecutar);
    console.log('botón "Imprimir":', btnImprimir);

    // Guardar HTML para inspección
    await page.screenshot({ path: 'test-results/custodios-diagnostico.png', fullPage: true });

    // Al menos una de estas condiciones debe ser verdadera
    expect(tienePapel + tieneFormulario + tieneError + tieneCargando).toBeGreaterThan(0);
  });
});
