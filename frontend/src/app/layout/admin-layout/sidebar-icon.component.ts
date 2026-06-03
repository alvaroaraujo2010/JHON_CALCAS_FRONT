import { Component, input } from '@angular/core';

@Component({
  selector: 'app-sidebar-icon',
  standalone: true,
  host: {
    class: 'sidebar-icon',
    '[class.sidebar-icon--lg]': 'size() === "lg"'
  },
  template: `
    @switch (name()) {
      @case ('dashboard') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="3" width="7" height="9" rx="1"/><rect x="14" y="3" width="7" height="5" rx="1"/><rect x="14" y="12" width="7" height="9" rx="1"/><rect x="3" y="16" width="7" height="5" rx="1"/></svg>
      }
      @case ('productos') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><path d="M3.27 6.96 12 12.01l8.73-5.05M12 22.08V12"/></svg>
      }
      @case ('categorias') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
      }
      @case ('inventario') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 9h18v10a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V9z"/><path d="M3 9V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v4"/><path d="M12 12v6M8 15h8"/></svg>
      }
      @case ('proveedores') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M1 3h15v13H1zM16 8h4l3 3v5h-7V8z"/><circle cx="5.5" cy="18.5" r="2.5"/><circle cx="18.5" cy="18.5" r="2.5"/></svg>
      }
      @case ('clientes') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>
      }
      @case ('compras') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/><path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"/></svg>
      }
      @case ('ventas') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><path d="M14 2v6h6M16 13H8M16 17H8M10 9H8"/></svg>
      }
      @case ('ventas-compras') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 7h10v10H7z"/><path d="M17 3h4v4M3 17h4v4M7 7 3 3M17 17l4 4M17 7l4-4M7 17 3 21"/></svg>
      }
      @case ('contabilidad') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>
      }
      @case ('usuarios') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
      }
      @case ('empresa') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 21h18M9 8h1M9 12h1M9 16h1M14 8h1M14 12h1M14 16h1M5 21V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16"/></svg>
      }
      @case ('nomina') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="2" y="4" width="20" height="16" rx="2"/><path d="M2 9h20M7 14h2M11 14h6"/></svg>
      }
      @case ('empleados') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="9" cy="8" r="4"/><path d="M2 21v-2a4 4 0 0 1 4-4h6a4 4 0 0 1 4 4v2"/><circle cx="17" cy="7" r="3"/><path d="M22 20v-1.5a3 3 0 0 0-2-2.83"/></svg>
      }
      @case ('deducciones') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M8 12h8M12 8v8"/></svg>
      }
      @case ('seguridad') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 2 4 5v7c0 5 3.5 9 8 10 4.5-1 8-5 8-10V5l-8-3z"/><path d="m9 12 2 2 4-4"/></svg>
      }
      @case ('pagos') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20M6 15h4"/></svg>
      }
      @case ('parametros') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
      }
      @case ('provisiones') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 3v18h18"/><path d="M7 14l4-4 4 4 6-6"/></svg>
      }
      @case ('liquidacion') {
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/></svg>
      }
      @default {
        <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="10"/></svg>
      }
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      flex-shrink: 0;
    }
    :host(.sidebar-icon--lg) {
      width: 56px;
      height: 56px;
    }
    :host(.sidebar-icon--lg) svg {
      width: 28px;
      height: 28px;
    }
    svg {
      width: 18px;
      height: 18px;
      stroke: currentColor;
      fill: none;
      stroke-width: 2;
      stroke-linecap: round;
      stroke-linejoin: round;
    }
  `
})
export class SidebarIconComponent {
  name = input.required<string>();
  size = input<'sm' | 'lg'>('sm');
}
