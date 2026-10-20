import { test } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.describe('Diagnóstico HTML: informe custodios-todos', () => {
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

  test('mostrar HTML del visor de informe', async ({ page }) => {
    await login(page);

    await page.goto(`${BASE_URL}/reportes/informe/custodios-todos`);
    await page.waitForURL('**/reportes/informe/custodios-todos', { timeout: 10000 });
    await page.waitForTimeout(3000);

    const html = await page.locator('body').innerHTML();
    console.log('=== HTML DEL CUERPO ===');
    console.log(html.substring(0, 3000));
    console.log('=== FIN HTML ===');

    // Tomar screenshot
    await page.screenshot({ path: 'test-results/custodios-html.png', fullPage: true });
  });
});
