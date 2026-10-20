import { test } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.describe('Diagnóstico HTML completo: informe custodios-todos', () => {
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

  test('mostrar HTML completo y buscar errores', async ({ page }) => {
    await login(page);

    await page.goto(`${BASE_URL}/reportes/informe/custodios-todos`);
    await page.waitForURL('**/reportes/informe/custodios-todos', { timeout: 10000 });
    await page.waitForTimeout(5000);

    const html = await page.locator('body').innerHTML();
    console.log('=== HTML COMPLETO (primeros 5000 chars) ===');
    console.log(html.substring(0, 5000));
    console.log('=== LONGITUD TOTAL:', html.length, '===');

    // Buscar mensajes de error
    const tieneError = await page.locator('.empty-state, .alert, .msg-error, [role="alert"]').count();
    console.log('elementos de error:', tieneError);

    // Buscar cargando
    const tieneCargando = await page.locator('.loading-panel, .spinner').count();
    console.log('elementos de carga:', tieneCargando);

    // Buscar cualquier texto visible
    const textoVisible = await page.locator('body').innerText();
    console.log('=== TEXTO VISIBLE ===');
    console.log(textoVisible.substring(0, 2000));
    console.log('=== FIN TEXTO ===');

    await page.screenshot({ path: 'test-results/custodios-completo.png', fullPage: true });
  });
});
