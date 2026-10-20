import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, IconComponent],
  template: `
    <div class="landing">
      <!-- ============ NAV ============ -->
      <header class="nav">
        <div class="container nav-inner">
          <a class="brand" href="/" aria-label="Monrys Capital, inicio">
            <svg viewBox="0 0 32 32" width="30" height="30" aria-hidden="true">
              <rect width="32" height="32" rx="7" fill="#0F172A" stroke="rgba(245,158,11,.45)" />
              <path d="M7 23V9h4l5 8 5-8h4v14h-4v-8l-5 8-5-8v8z" fill="#F59E0B" />
            </svg>
            <span class="brand-name"><strong>Monrys</strong><em>Capital</em></span>
          </a>

          <nav class="nav-links" aria-label="Secciones">
            <a href="#soluciones">Soluciones</a>
            <a href="#confianza">Confianza</a>
            <a href="#proceso">Proceso</a>
          </nav>

          <a class="btn btn-primary btn-sm" routerLink="/login">
            <app-icon name="lock" [size]="15" />
            Iniciar sesión
          </a>
        </div>
      </header>

      <!-- ============ HERO ============ -->
      <section class="hero">
        <div class="hero-grid" aria-hidden="true"></div>
        <div class="hero-glow" aria-hidden="true"></div>

        <div class="container hero-inner">
          <div class="hero-text">
            <p class="eyebrow reveal">Casa de cambio · Venezuela</p>
            <h1 class="reveal">
              Casa de cambio con<br />
              <span class="gold">estándar bancario</span>
            </h1>
            <p class="lead reveal">
              Operamos divisas, pagos y custodia con la disciplina de una entidad financiera:
              trazabilidad completa, conciliación diaria y comprobantes verificables en cada
              transacción.
            </p>

            <div class="cta-row reveal">
              <a class="btn btn-primary btn-lg" routerLink="/login">Iniciar sesión</a>
              <a class="btn btn-secondary btn-lg" href="#soluciones">Ver soluciones</a>
            </div>

            <dl class="hero-stats reveal">
              <div>
                <dt>Disponibilidad de la mesa</dt>
                <dd class="mono">24 / 7</dd>
              </div>
              <div>
                <dt>Divisas operadas</dt>
                <dd class="mono">USD · Bs</dd>
              </div>
              <div>
                <dt>Conciliación de caja</dt>
                <dd class="mono">Diaria</dd>
              </div>
            </dl>
          </div>

          <!-- Panel de cotización (atmósfera de mesa de dinero) -->
          <aside class="hero-panel reveal" aria-label="Mesa de dinero">
            <div class="panel-head">
              <span class="dot" aria-hidden="true"></span>
              <span>Mesa de dinero</span>
              <span class="mono panel-hora">Bs / USD</span>
            </div>
            <div class="panel-row">
              <span>Compra</span>
              <strong class="mono">36,50</strong>
            </div>
            <div class="panel-row">
              <span>Venta</span>
              <strong class="mono gold">38,20</strong>
            </div>
            <div class="panel-hairline" aria-hidden="true"></div>
            <ul class="panel-list">
              <li><span>Transferencias nacionales</span><span class="mono">T+0</span></li>
              <li><span>Transferencias internacionales</span><span class="mono">T+1</span></li>
              <li><span>Custodia multimoneda</span><span class="mono">Bs · USD</span></li>
            </ul>
            <p class="panel-note">Tasas referenciales. La tasa vigente se publica en la sesión.</p>
          </aside>
        </div>
      </section>

      <!-- ============ PRUEBA / CREDIBILIDAD ============ -->
      <section class="proof" id="confianza">
        <div class="container">
          <div class="proof-grid">
            <article class="proof-item">
              <app-icon name="shield" [size]="22" />
              <h3>Registro SUDEBAN</h3>
              <p>Operación sujeta a las normativas de casas de cambio y prevención de lavado.</p>
            </article>
            <article class="proof-item">
              <app-icon name="lock" [size]="22" />
              <h3>Cumplimiento KYC / AML</h3>
              <p>Verificación de identidad, límites por operación y bitácora de auditoría.</p>
            </article>
            <article class="proof-item">
              <app-icon name="chart" [size]="22" />
              <h3>Conciliación bancaria</h3>
              <p>Cada movimiento de caja se concilia contra cuentas bancarias certificadas.</p>
            </article>
            <article class="proof-item">
              <app-icon name="file" [size]="22" />
              <h3>Comprobantes emitibles</h3>
              <p>Recibos y comprobantes de compra, venta y entrega listos para imprimir.</p>
            </article>
          </div>
        </div>
      </section>

      <!-- ============ SOLUCIONES ============ -->
      <section class="solutions" id="soluciones">
        <div class="container">
          <header class="section-head">
            <p class="eyebrow">Soluciones</p>
            <h2>Una mesa de dinero completa</h2>
            <p class="section-sub">
              Cuatro líneas de servicio operadas desde un solo sistema, con la misma fuente de
              verdad para caja, bancos y custodia.
            </p>
          </header>

          <div class="cards">
            <article class="sol-card">
              <span class="sol-ico"><app-icon name="swap" [size]="20" /></span>
              <h3>Cambio de divisas</h3>
              <p>Compra y venta de USD con tasa configurable, cuotas de recepción y pago por operación.</p>
              <ul>
                <li>Cotización con tasa activa</li>
                <li>Comprobante por transacción</li>
                <li>Historial por cliente</li>
              </ul>
            </article>

            <article class="sol-card">
              <span class="sol-ico"><app-icon name="transfer" [size]="20" /></span>
              <h3>Pagos y transferencias</h3>
              <p>Transferencias nacionales e internacionales con registro de banco, cuenta y beneficiario.</p>
              <ul>
                <li>Trazabilidad por número</li>
                <li>Conciliación contra caja</li>
                <li>Anulación con motivo</li>
              </ul>
            </article>

            <article class="sol-card">
              <span class="sol-ico"><app-icon name="shield" [size]="20" /></span>
              <h3>Custodia de activos</h3>
              <p>Saldos de custodia por cliente y moneda, con entradas y salidas documentadas.</p>
              <ul>
                <li>Disponible en Bs y USD</li>
                <li>Movimientos por cliente</li>
                <li>Recibir y entregar</li>
              </ul>
            </article>

            <article class="sol-card">
              <span class="sol-ico"><app-icon name="layers" [size]="20" /></span>
              <h3>Tesorería y caja</h3>
              <p>Caja con codificación de ingresos y egresos, arqueo por denominaciones y cierre diario.</p>
              <ul>
                <li>Arqueo por billetes</li>
                <li>Cuadre de caja</li>
                <li>Cierre con estado</li>
              </ul>
            </article>
          </div>
        </div>
      </section>

      <!-- ============ PROCESO ============ -->
      <section class="process" id="proceso">
        <div class="container">
          <header class="section-head">
            <p class="eyebrow">Proceso</p>
            <h2>Operación en tres pasos auditables</h2>
          </header>
          <ol class="steps">
            <li>
              <span class="step-n mono">01</span>
              <div>
                <h3>Registro del cliente</h3>
                <p>Persona natural o jurídica, con RIF o cédula y cuentas bancarias vinculadas.</p>
              </div>
            </li>
            <li>
              <span class="step-n mono">02</span>
              <div>
                <h3>Ejecución de la operación</h3>
                <p>Compra o venta con tasa del día, monto en Bs calculado y cuotas programadas.</p>
              </div>
            </li>
            <li>
              <span class="step-n mono">03</span>
              <div>
                <h3>Conciliación y comprobante</h3>
                <p>Recepciones y pagos en caja o banco, cierre del día y comprobante imprimible.</p>
              </div>
            </li>
          </ol>
        </div>
      </section>

      <!-- ============ CTA FINAL ============ -->
      <section class="cta-final">
        <div class="container cta-inner">
          <div>
            <p class="eyebrow">Acceso operativo</p>
            <h2>Entre a la sala de operaciones</h2>
            <p class="section-sub">Usuarios autorizados, roles por módulo y auditoría de cada cambio.</p>
          </div>
          <a class="btn btn-primary btn-lg" routerLink="/login">Iniciar sesión</a>
        </div>
      </section>

      <!-- ============ FOOTER ============ -->
      <footer class="footer">
        <div class="container footer-inner">
          <div class="footer-brand">
            <span class="brand-name"><strong>Monrys</strong><em>Capital</em></span>
            <p>Casa de cambio con estándar bancario. Divisas, pagos, custodia y tesorería.</p>
          </div>
          <div class="footer-meta">
            <p class="mono">Caracas · Venezuela</p>
            <p class="mono">ops&#64;monryscapital.com</p>
            <p class="mono">© {{ anio }} Monrys Capital — Todos los derechos reservados</p>
          </div>
        </div>
      </footer>
    </div>
  `,
  styles: [
    `
      .landing {
        min-height: 100vh;
        background:
          linear-gradient(180deg, #0f172a 0%, #0a0f1e 55%, #070b16 100%);
        overflow-x: hidden;
      }

      /* ---------- Nav ---------- */
      .nav {
        position: sticky;
        top: 0;
        z-index: 100;
        background: rgba(10, 15, 30, 0.82);
        backdrop-filter: blur(10px);
        border-bottom: 1px solid rgba(51, 65, 85, 0.7);
      }

      .nav-inner {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 24px;
        height: 68px;
      }

      .brand {
        display: flex;
        align-items: center;
        gap: 11px;
        cursor: pointer;
      }

      .brand-name {
        display: flex;
        flex-direction: column;
        line-height: 1.1;
      }

      .brand-name strong {
        font-size: 17px;
        font-weight: 700;
        color: #f8fafc;
        letter-spacing: -0.01em;
      }

      .brand-name em {
        font-style: normal;
        font-family: var(--font-mono);
        font-size: 10px;
        text-transform: uppercase;
        letter-spacing: 0.24em;
        color: #f59e0b;
      }

      .nav-links {
        display: flex;
        gap: 30px;
        margin-inline-start: auto;
        margin-inline-end: 8px;
      }

      .nav-links a {
        color: #94a3b8;
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
        padding: 6px 0;
        border-bottom: 1px solid transparent;
        transition: color 200ms ease, border-color 200ms ease;
      }

      .nav-links a:hover {
        color: #f8fafc;
        border-bottom-color: rgba(245, 158, 11, 0.6);
      }

      /* ---------- Hero ---------- */
      .hero {
        position: relative;
        padding: 96px 0 88px;
        border-bottom: 1px solid rgba(51, 65, 85, 0.6);
      }

      .hero-grid {
        position: absolute;
        inset: 0;
        background-image:
          linear-gradient(var(--grid-line) 1px, transparent 1px),
          linear-gradient(90deg, var(--grid-line) 1px, transparent 1px);
        background-size: 64px 64px;
        mask-image: radial-gradient(ellipse 90% 70% at 50% 30%, #000 30%, transparent 75%);
        pointer-events: none;
      }

      .hero-glow {
        position: absolute;
        top: -180px;
        right: -120px;
        width: 620px;
        height: 620px;
        background: radial-gradient(circle, rgba(245, 158, 11, 0.14), transparent 62%);
        pointer-events: none;
      }

      .hero-inner {
        position: relative;
        display: grid;
        grid-template-columns: minmax(0, 1.25fr) minmax(0, 0.85fr);
        gap: 64px;
        align-items: center;
      }

      .hero h1 {
        font-size: clamp(38px, 5.4vw, 62px);
        font-weight: 300;
        letter-spacing: -0.03em;
        line-height: 1.06;
        margin: 18px 0 22px;
        color: #f8fafc;
      }

      .hero h1 .gold {
        font-weight: 600;
        color: #f59e0b;
      }

      .lead {
        font-size: clamp(15px, 1.5vw, 17.5px);
        line-height: 1.65;
        color: #b6c2d4;
        max-width: 56ch;
        font-weight: 300;
      }

      .cta-row {
        display: flex;
        gap: 14px;
        flex-wrap: wrap;
        margin-top: 34px;
      }

      .hero-stats {
        display: flex;
        gap: 44px;
        margin: 52px 0 0;
        padding-top: 26px;
        border-top: 1px solid var(--hairline);
        flex-wrap: wrap;
      }

      .hero-stats dt {
        font-size: 11px;
        text-transform: uppercase;
        letter-spacing: 0.12em;
        color: #64748b;
        margin-bottom: 6px;
      }

      .hero-stats dd {
        margin: 0;
        font-size: 21px;
        font-weight: 600;
        color: #f8fafc;
      }

      /* Panel mesa de dinero */
      .hero-panel {
        background: rgba(34, 39, 53, 0.85);
        border: 1px solid var(--color-border);
        border-radius: 14px;
        padding: 24px;
        box-shadow: var(--shadow-xl);
        position: relative;
      }

      .hero-panel::before {
        content: '';
        position: absolute;
        inset-inline: 24px;
        top: 0;
        height: 2px;
        background: linear-gradient(90deg, transparent, #f59e0b, transparent);
        opacity: 0.8;
      }

      .panel-head {
        display: flex;
        align-items: center;
        gap: 9px;
        font-size: 11px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.14em;
        color: #94a3b8;
        padding-bottom: 16px;
        border-bottom: 1px solid var(--color-border);
      }

      .panel-head .dot {
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background: #22c55e;
        box-shadow: 0 0 0 3px rgba(34, 197, 94, 0.2);
      }

      .panel-hora {
        margin-inline-start: auto;
        color: #64748b;
        letter-spacing: 0.08em;
      }

      .panel-row {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        padding: 18px 0 6px;
      }

      .panel-row span {
        font-size: 13.5px;
        color: #94a3b8;
      }

      .panel-row strong {
        font-size: 34px;
        font-weight: 600;
        letter-spacing: -0.02em;
        color: #f8fafc;
      }

      .panel-row strong.gold {
        color: #f59e0b;
      }

      .panel-hairline {
        height: 1px;
        background: var(--hairline);
        margin: 14px 0;
      }

      .panel-list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 11px;
      }

      .panel-list li {
        display: flex;
        justify-content: space-between;
        gap: 12px;
        font-size: 13px;
        color: #b6c2d4;
      }

      .panel-list li span:last-child {
        color: #f59e0b;
        font-size: 12.5px;
      }

      .panel-note {
        margin-top: 18px;
        padding-top: 14px;
        border-top: 1px solid var(--color-border);
        font-size: 11.5px;
        color: #64748b;
        line-height: 1.5;
      }

      /* ---------- Prueba ---------- */
      .proof {
        padding: 64px 0;
        border-bottom: 1px solid rgba(51, 65, 85, 0.6);
        background: rgba(10, 15, 30, 0.6);
      }

      .proof-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
        gap: 28px;
      }

      .proof-item {
        padding-left: 18px;
        border-inline-start: 2px solid rgba(245, 158, 11, 0.5);
      }

      .proof-item app-icon {
        color: #f59e0b;
      }

      .proof-item h3 {
        font-size: 15.5px;
        margin: 12px 0 7px;
        color: #f8fafc;
      }

      .proof-item p {
        font-size: 13.5px;
        line-height: 1.6;
        color: #94a3b8;
        font-weight: 300;
      }

      /* ---------- Secciones ---------- */
      .solutions,
      .process {
        padding: 84px 0;
      }

      .process {
        padding-top: 0;
      }

      .section-head {
        max-width: 720px;
        margin-bottom: 48px;
      }

      .section-head h2 {
        font-size: clamp(26px, 3vw, 36px);
        font-weight: 300;
        letter-spacing: -0.02em;
        margin: 14px 0 14px;
        color: #f8fafc;
      }

      .section-sub {
        font-size: 15.5px;
        line-height: 1.65;
        color: #94a3b8;
        font-weight: 300;
      }

      .cards {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
        gap: 22px;
      }

      .sol-card {
        background: rgba(34, 39, 53, 0.7);
        border: 1px solid var(--color-border);
        border-radius: 12px;
        padding: 26px 24px;
        cursor: default;
        transition: border-color 220ms ease, transform 220ms ease, box-shadow 220ms ease;
      }

      .sol-card:hover {
        border-color: rgba(245, 158, 11, 0.5);
        transform: translateY(-3px);
        box-shadow: var(--shadow-lg);
      }

      .sol-ico {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 42px;
        height: 42px;
        border-radius: 10px;
        background: rgba(245, 158, 11, 0.12);
        border: 1px solid rgba(245, 158, 11, 0.35);
        color: #f59e0b;
        margin-bottom: 18px;
      }

      .sol-card h3 {
        font-size: 17px;
        font-weight: 600;
        margin-bottom: 9px;
        color: #f8fafc;
      }

      .sol-card > p {
        font-size: 13.5px;
        line-height: 1.6;
        color: #94a3b8;
        font-weight: 300;
        margin-bottom: 16px;
      }

      .sol-card ul {
        list-style: none;
        margin: 0;
        padding: 14px 0 0;
        border-top: 1px solid var(--color-border);
        display: flex;
        flex-direction: column;
        gap: 8px;
      }

      .sol-card li {
        font-size: 12.5px;
        color: #b6c2d4;
        display: flex;
        align-items: center;
        gap: 9px;
      }

      .sol-card li::before {
        content: '';
        width: 5px;
        height: 5px;
        border-radius: 50%;
        background: #f59e0b;
        flex: 0 0 auto;
      }

      /* ---------- Proceso ---------- */
      .steps {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
        gap: 30px;
        counter-reset: none;
      }

      .steps li {
        display: flex;
        gap: 18px;
        padding-top: 22px;
        border-top: 1px solid var(--color-border);
      }

      .step-n {
        font-size: 13px;
        font-weight: 600;
        color: #f59e0b;
        letter-spacing: 0.1em;
        flex: 0 0 auto;
      }

      .steps h3 {
        font-size: 16.5px;
        margin-bottom: 8px;
        color: #f8fafc;
      }

      .steps p {
        font-size: 13.5px;
        line-height: 1.6;
        color: #94a3b8;
        font-weight: 300;
      }

      /* ---------- CTA final ---------- */
      .cta-final {
        padding: 64px 0;
        background:
          linear-gradient(90deg, rgba(245, 158, 11, 0.07), transparent 55%),
          rgba(15, 23, 42, 0.7);
        border-block: 1px solid rgba(245, 158, 11, 0.25);
      }

      .cta-inner {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 32px;
        flex-wrap: wrap;
      }

      .cta-inner h2 {
        font-size: clamp(24px, 2.6vw, 32px);
        font-weight: 300;
        letter-spacing: -0.02em;
        margin: 12px 0 10px;
      }

      /* ---------- Footer ---------- */
      .footer {
        padding: 46px 0 40px;
        background: #070b16;
      }

      .footer-inner {
        display: flex;
        justify-content: space-between;
        gap: 36px;
        flex-wrap: wrap;
      }

      .footer-brand p {
        margin-top: 12px;
        font-size: 13px;
        color: #64748b;
        max-width: 44ch;
        line-height: 1.6;
        font-weight: 300;
      }

      .footer-meta {
        text-align: end;
      }

      .footer-meta p {
        font-size: 12.5px;
        color: #64748b;
        margin-bottom: 6px;
      }

      /* ---------- Reveal sutil ---------- */
      .reveal {
        animation: reveal 620ms cubic-bezier(0.22, 1, 0.36, 1) both;
      }

      .hero-text .reveal:nth-child(2) {
        animation-delay: 80ms;
      }
      .hero-text .reveal:nth-child(3) {
        animation-delay: 160ms;
      }
      .hero-text .reveal:nth-child(4) {
        animation-delay: 240ms;
      }
      .hero-text .reveal:nth-child(5) {
        animation-delay: 320ms;
      }
      .hero-panel.reveal {
        animation-delay: 220ms;
      }

      @keyframes reveal {
        from {
          opacity: 0;
          transform: translateY(14px);
        }
        to {
          opacity: 1;
          transform: none;
        }
      }

      /* ---------- Responsive ---------- */
      @media (max-width: 1024px) {
        .hero-inner {
          gap: 44px;
        }
        .hero {
          padding: 72px 0 64px;
        }
      }

      @media (max-width: 768px) {
        .nav-links {
          display: none;
        }
        .hero {
          padding: 56px 0 52px;
        }
        .hero-inner {
          grid-template-columns: 1fr;
          gap: 40px;
        }
        .hero-stats {
          gap: 26px;
        }
        .solutions,
        .process {
          padding: 60px 0;
        }
        .footer-meta {
          text-align: start;
        }
      }

      @media (max-width: 375px) {
        .cta-row .btn {
          width: 100%;
        }
        .hero-stats {
          flex-direction: column;
          gap: 18px;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .reveal {
          animation: none;
        }
        .sol-card:hover {
          transform: none;
        }
      }
    `,
  ],
})
export class LandingComponent {
  readonly anio = new Date().getFullYear();
}
