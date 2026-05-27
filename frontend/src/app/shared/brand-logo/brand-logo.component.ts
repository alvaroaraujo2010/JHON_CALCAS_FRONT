import { Component, computed, input } from '@angular/core';
import { brandLogoSrc, BrandLogoVariant } from '../../core/brand-assets';

export type BrandLogoSize = 'icon' | 'sidebar' | 'nav' | 'md' | 'lg' | 'xl' | 'full';

@Component({
  selector: 'app-brand-logo',
  standalone: true,
  template: `
    <img
      [src]="src()"
      [attr.alt]="alt()"
      [attr.width]="dims()?.width"
      [attr.height]="dims()?.height"
      [class]="'brand-logo brand-logo--' + size()"
      loading="lazy"
      decoding="async"
    />
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      line-height: 0;
    }

    :host(.brand-logo-host--full) {
      display: block;
      width: 100%;
    }

    .brand-logo {
      display: block;
      width: auto;
      object-fit: contain;
    }

    .brand-logo--icon {
      height: 40px;
      width: 40px;
    }

    .brand-logo--sidebar {
      height: 56px;
      width: 56px;
      flex-shrink: 0;
    }

    .brand-logo--nav {
      height: 44px;
      max-width: min(240px, 52vw);
    }

    .brand-logo--md {
      height: 52px;
      max-width: 280px;
    }

    .brand-logo--lg {
      height: 72px;
      max-width: 360px;
    }

    .brand-logo--xl {
      height: 125px;
      max-width: min(720px, 86vw);
    }

    .brand-logo--full {
      width: 100%;
      height: auto;
      max-width: 100%;
    }
  `,
  host: {
    '[class.brand-logo-host--full]': 'size() === "full"'
  }
})
export class BrandLogoComponent {
  variant = input<BrandLogoVariant>('color');
  size = input<BrandLogoSize>('nav');
  alt = input('ContaNexo');

  src = computed(() => brandLogoSrc(this.variant()));

  dims = computed(() => {
    const map: Record<BrandLogoSize, { width: number; height: number } | null> = {
      icon: { width: 40, height: 40 },
      sidebar: { width: 56, height: 56 },
      nav: { width: 240, height: 44 },
      md: { width: 280, height: 52 },
      lg: { width: 360, height: 72 },
      xl: { width: 720, height: 125 },
      full: null
    };
    return map[this.size()];
  });
}
