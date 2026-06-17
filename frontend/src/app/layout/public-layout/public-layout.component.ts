import { Component, OnDestroy, OnInit } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { JhonCalcasHeaderComponent } from '../../shared/jhon-calcas-header/jhon-calcas-header.component';
import { JHON_CALCAS_IMAGES, JHON_CALCAS_WHATSAPP_URL } from '../../core/jhon-calcas-assets';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, JhonCalcasHeaderComponent],
  template: `
    <div class="jc-site">
      <app-jhon-calcas-header />
      <router-outlet />
      <footer class="jc-footer">
        <div class="page-width jc-footer__inner">
          <img [src]="images.logo" alt="Jhon Calcas" class="jc-footer__logo" width="90" height="62" />
          <p>Jhon Calcas — Calcomanías para motocicletas</p>
          <a routerLink="/login" class="jc-footer__admin">Acceso administrativo</a>
        </div>
      </footer>
    </div>
    <a
      [href]="whatsappUrl"
      class="jc-whatsapp"
      target="_blank"
      rel="noopener noreferrer"
      aria-label="Contactar por WhatsApp"
    >
      <img [src]="images.whatsapp" alt="" width="60" height="60" />
    </a>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: #121212; }
    .jc-site {
      --jc-red: #dd1d1d; --jc-red-dark: #c40303; --jc-text: #121212;
      --jc-muted: rgba(18,18,18,0.75); --jc-page: #121212;
      --jc-on-dark: #f3f3f3; --jc-card: #ffffff;
      --jc-page-width: 120rem;
      font-family: 'Montserrat', system-ui, sans-serif;
      color: var(--jc-on-dark); background: var(--jc-page);
    }
    .jc-site :host ::ng-deep router-outlet + * { background: var(--jc-page); min-height: 50vh; display: block; }
    .page-width { max-width: var(--jc-page-width); margin: 0 auto; padding-left: 1.5rem; padding-right: 1.5rem; }
    .jc-footer {
      background: #121212; color: rgba(255,255,255,0.78); padding: 2.5rem 0; text-align: center;
    }
    .jc-footer__inner { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; }
    .jc-footer__logo { width: auto; height: 3.5rem; margin-bottom: 0.5rem; }
    .jc-footer p { margin: 0; font-size: 0.9rem; }
    .jc-footer a { color: #fff; text-decoration: none; }
    .jc-footer a:hover { text-decoration: underline; }
    .jc-footer__admin { margin-top: 0.75rem; font-size: 0.85rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: rgba(255,255,255,0.65) !important; }
    .jc-footer__admin:hover { color: #fff !important; }
    .jc-whatsapp {
      position: fixed; right: 1.25rem; bottom: 1.25rem; z-index: 200; line-height: 0;
      filter: drop-shadow(0 4px 12px rgba(0,0,0,0.25));
    }
    .jc-whatsapp img { width: 60px; height: 60px; display: block; }
    @media (min-width: 750px) {
      .page-width { padding-left: 5rem; padding-right: 5rem; }
    }
  `]
})
export class PublicLayoutComponent implements OnInit, OnDestroy {
  readonly images = JHON_CALCAS_IMAGES;
  readonly whatsappUrl = JHON_CALCAS_WHATSAPP_URL;

  ngOnInit() {
    document.body.classList.add('jc-public-page');
  }
  ngOnDestroy() {
    document.body.classList.remove('jc-public-page');
  }
}
