import { Component, DestroyRef, OnInit, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { catchError, distinctUntilChanged, filter, map, of, startWith, switchMap } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CartService } from '../../../core/services/cart.service';
import { CatalogProduct } from '../../../core/models';
import { ToastService } from '../../../core/services/toast.service';
import { resolveGalleryFilters } from '../../../core/jhon-calcas-routing';

interface PlaceholderProduct {
  id: number;
  title: string;
  brand: string;
  model: string;
  menuModel?: string;
  color?: string;
  price: number;
  imageUrl: string;
  comingSoon: boolean;
}

interface ProductGroup {
  key: string;
  brand: string;
  menuModel: string;
  model: string;
  designRef?: string;
  baseTitle: string;
  variants: CatalogProduct[];
}

const PLACEHOLDERS: PlaceholderProduct[] = [
  { id: -1, title: 'Protector Deportivo', brand: 'YAMAHA', model: 'MT-09', price: 89000, imageUrl: '', comingSoon: true },
  { id: -2, title: 'Calca Tanque Carbon', brand: 'HONDA', model: 'CRF', price: 65000, imageUrl: '', comingSoon: true },
  { id: -3, title: 'Kit Rines Racing', brand: 'SUZUKI', model: 'GSX', price: 120000, imageUrl: '', comingSoon: true },
  { id: -4, title: 'Emblema 3D', brand: 'KAWASAKI', model: 'Z900', price: 45000, imageUrl: '', comingSoon: true },
  { id: -5, title: 'Protector Full', brand: 'YAMAHA', model: 'R15', price: 95000, imageUrl: '', comingSoon: true },
  { id: -6, title: 'Calca Rines Premium', brand: 'HONDA', model: 'CBR', price: 78000, imageUrl: '', comingSoon: true },
];

const COLOR_SWATCHES: Record<string, string> = {
  azul: '#2563eb',
  celeste: '#38bdf8',
  aguamarina: '#2dd4bf',
  dorado: '#d4a017',
  oro: '#d4a017',
  gris: '#94a3b8',
  plata: '#cbd5e1',
  plateado: '#cbd5e1',
  rojo: '#de0404',
  verde: '#16a34a',
  militar: '#4d7c0f',
  'verde militar': '#4d7c0f',
  lila: '#a78bfa',
  morado: '#7c3aed',
  violeta: '#7c3aed',
  naranja: '#f97316',
  amarillo: '#eab308',
  blanco: '#f8fafc',
  negro: '#0f172a',
  rosado: '#fb7185',
  rosa: '#fb7185',
  fucsia: '#e11d48',
  cafe: '#92400e',
  café: '#92400e',
  marron: '#92400e',
  marrón: '#92400e',
  beige: '#d6bfa3',
  turquesa: '#14b8a6',
  carbono: '#334155',
};

function stripColorFromTitle(title: string, color?: string | null): string {
  if (!title) return '';
  let base = title.trim();
  if (color) {
    const re = new RegExp(`\\s*${escapeRegExp(color)}\\s*$`, 'i');
    base = base.replace(re, '').trim();
  }
  return base || title;
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function groupKey(p: CatalogProduct): string {
  return [p.productLine || '', p.brand || '', p.menuModel || '', p.model || '', p.designRef || ''].join('|');
}

function groupProducts(products: CatalogProduct[]): ProductGroup[] {
  const map = new Map<string, CatalogProduct[]>();
  for (const p of products) {
    const key = groupKey(p);
    const list = map.get(key) || [];
    list.push(p);
    map.set(key, list);
  }

  return [...map.entries()].map(([key, variants]) => {
    const sorted = [...variants].sort((a, b) =>
      (a.color || a.title).localeCompare(b.color || b.title, 'es')
    );
    const first = sorted[0];
    return {
      key,
      brand: first.brand,
      menuModel: first.menuModel || first.model || '',
      model: first.model || '',
      designRef: first.designRef,
      baseTitle: stripColorFromTitle(first.title, first.color),
      variants: sorted,
    };
  });
}

@Component({
  selector: 'app-gallery',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="jc-gallery">
      <div class="page-width">
        <nav class="jc-breadcrumb">
          <a routerLink="/">Inicio</a>
          @if (productLine()) {
            <span class="jc-breadcrumb-sep">/</span>
            <a routerLink="/galeria">{{ productLine() }}</a>
          }
          @if (brandFilter()) {
            <span class="jc-breadcrumb-sep">/</span>
            <span>{{ brandFilter() }}</span>
          }
          @if (modelFilter()) {
            <span class="jc-breadcrumb-sep">/</span>
            <span>{{ modelFilter() }}</span>
          }
          @if (searchTerm()) {
            <span class="jc-breadcrumb-sep">/</span>
            <span>Buscar: {{ searchTerm() }}</span>
          }
        </nav>

        @if (title()) {
          <h1 class="jc-gallery__title">{{ title() }}</h1>
        }

        @if (loading()) {
          <div class="jc-gallery__loading">Cargando productos...</div>
        } @else if (error()) {
          <div class="jc-gallery__error">{{ error() }}</div>
        } @else {
          @if (brands().length > 1 && !selectedBrand()) {
            <div class="jc-gallery__brands">
              @for (b of brands(); track b) {
                <button class="jc-chip" [class.jc-chip--active]="selectedBrand() === b" (click)="filterBrand(b)">{{ b }}</button>
              }
            </div>
          }

          @if (productGroups().length > 0) {
            <div class="jc-gallery__grid">
              @for (group of productGroups(); track group.key) {
                @let selected = selectedVariant(group);
                <div class="jc-product-card">
                  <div class="jc-product-card__image">
                    @if (selected.imageUrl) {
                      <img [src]="selected.imageUrl" [alt]="selected.title" loading="lazy" (click)="openLightbox(selected)" />
                    } @else {
                      <div class="jc-product-card__placeholder">Sin imagen</div>
                    }
                    @if (group.menuModel) {
                      <span class="jc-product-card__model-badge">{{ group.menuModel }}</span>
                    }
                    @if (selected.color) {
                      <span class="jc-product-card__color-badge">{{ selected.color }}</span>
                    }
                  </div>
                  <div class="jc-product-card__info">
                    <span class="jc-product-card__brand">{{ group.brand }}</span>
                    <h3 class="jc-product-card__title">{{ displayTitle(group, selected) }}</h3>
                    <span class="jc-product-card__price">\${{ selected.price.toLocaleString('es-CO') }}</span>

                    @if (group.variants.length > 1) {
                      <div class="jc-color-palette" role="listbox" [attr.aria-label]="'Colores de ' + group.baseTitle">
                        @for (variant of group.variants; track variant.id) {
                          <button
                            type="button"
                            class="jc-color-swatch"
                            role="option"
                            [class.jc-color-swatch--active]="variant.id === selected.id"
                            [class.jc-color-swatch--light]="isLightColor(variant.color)"
                            [style.background]="swatchColor(variant.color)"
                            [attr.aria-label]="variant.color || variant.title"
                            [attr.title]="variant.color || variant.title"
                            [attr.aria-selected]="variant.id === selected.id"
                            (click)="selectColor(group.key, variant.id)"
                          ></button>
                        }
                      </div>
                    }

                    <button class="jc-product-card__add" (click)="addToCart(selected)">
                      Agregar al carrito
                    </button>
                  </div>
                </div>
              }
            </div>
          } @else {
            <div class="jc-gallery__grid">
              @for (p of placeholders; track p.id) {
                <div class="jc-product-card">
                  <div class="jc-product-card__image">
                    <div class="jc-product-card__soon">
                      <svg viewBox="0 0 400 260" fill="none">
                        <rect width="400" height="260" fill="#f1f5f9"/>
                        <circle cx="200" cy="110" r="48" fill="#e2e8f0"/>
                        <path d="M186 98h28v28h-28z" fill="#94a3b8"/>
                        <circle cx="214" cy="106" r="4" fill="#f1f5f9"/>
                        <path d="M190 130l10-12 14 16 20-24 24 28" stroke="#94a3b8" stroke-width="3" stroke-linecap="round"/>
                        <rect x="120" y="170" width="160" height="6" rx="3" fill="#e2e8f0"/>
                        <rect x="150" y="186" width="100" height="6" rx="3" fill="#e2e8f0"/>
                        <rect x="140" y="202" width="120" height="6" rx="3" fill="#e2e8f0"/>
                      </svg>
                      <span class="jc-product-card__soon-label">Próximamente</span>
                    </div>
                  </div>
                  <div class="jc-product-card__info">
                    <span class="jc-product-card__brand">{{ p.brand }}</span>
                    <h3 class="jc-product-card__title">{{ p.title }}</h3>
                    <span class="jc-product-card__price">\${{ p.price.toLocaleString('es-CO') }}</span>
                    <button class="jc-product-card__add jc-product-card__add--soon" disabled>Próximamente</button>
                  </div>
                </div>
              }
            </div>
          }
        }
      </div>
    </div>

    @if (lightboxProduct()) {
      <div class="jc-lightbox" (click)="closeLightbox()">
        <img [src]="lightboxProduct()!.imageUrl" [alt]="lightboxProduct()!.title" class="jc-lightbox__img" />
        <button class="jc-lightbox__close" (click)="closeLightbox()">&times;</button>
      </div>
    }
  `,
  styles: [`
    .jc-breadcrumb { display: flex; align-items: center; gap: 0.5rem; font-size: 1rem; color: #64748b; margin-bottom: 1.5rem; flex-wrap: wrap; text-transform: uppercase; letter-spacing: 0.04em; }
    .jc-gallery__title { margin: 0 0 1.5rem; font-size: 1.5rem; font-weight: 700; color: #0f172a; text-align: center; text-transform: uppercase; letter-spacing: 0.04em; }
    .jc-breadcrumb a { color: #64748b; text-decoration: none; &:hover { color: #de0404; } }
    .jc-breadcrumb span { color: #0f172a; font-weight: 600; }
    .jc-breadcrumb-sep { color: #cbd5e1; pointer-events: none; }
    .jc-gallery { background: var(--jc-gallery-bg, #f8fafc); padding: 2.5rem 0 5rem; min-height: 60vh; }
    .jc-gallery__brands { display: flex; flex-wrap: wrap; gap: 0.65rem; justify-content: center; margin-bottom: 2rem; }
    .jc-chip {
      padding: 0.7rem 1.45rem;
      border: 2px solid #de0404;
      border-radius: 999px;
      background: #fff;
      cursor: pointer;
      font-size: 0.95rem;
      font-weight: 700;
      letter-spacing: 0.03em;
      text-transform: uppercase;
      transition: all 0.2s;
      color: #de0404;
    }
    .jc-chip:hover { background: rgba(222, 4, 4, 0.08); color: #c40303; border-color: #c40303; }
    .jc-chip--active { background: #de0404; color: #fff; border-color: #de0404; }
    .jc-gallery__grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 1.5rem; }
    .jc-product-card { border-radius: 12px; overflow: hidden; background: #fff; box-shadow: 0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04); transition: box-shadow 0.25s, transform 0.2s; display: flex; flex-direction: column; }
    .jc-product-card:hover { box-shadow: 0 10px 25px rgba(0,0,0,0.08), 0 4px 10px rgba(0,0,0,0.04); transform: translateY(-2px); }
    .jc-product-card__image { aspect-ratio: 3/2; overflow: hidden; cursor: pointer; position: relative; background: #f8fafc; }
    .jc-product-card__image img { width: 100%; height: 100%; object-fit: cover; transition: transform 0.4s; display: block; }
    .jc-product-card__image img:hover { transform: scale(1.06); }
    .jc-product-card__soon { position: relative; width: 100%; height: 100%; }
    .jc-product-card__soon svg { width: 100%; height: 100%; display: block; }
    .jc-product-card__soon-label { position: absolute; bottom: 0.75rem; left: 50%; transform: translateX(-50%); background: #f1f5f9; color: #64748b; padding: 0.3rem 1rem; border-radius: 999px; font-size: 0.75rem; font-weight: 600; }
    .jc-product-card__placeholder { width: 100%; height: 100%; display: flex; align-items: center; justify-content: center; background: #f1f5f9; color: #94a3b8; font-size: 0.85rem; }
    .jc-product-card__model-badge {
      position: absolute;
      top: 0.7rem;
      left: 0.7rem;
      background: rgba(15, 23, 42, 0.9);
      color: #fff;
      padding: 0.35rem 0.75rem;
      border-radius: 8px;
      font-size: 0.8rem;
      font-weight: 700;
      letter-spacing: 0.03em;
    }
    .jc-product-card__color-badge {
      position: absolute;
      top: 0.7rem;
      right: 0.7rem;
      background: #de0404;
      color: #fff;
      padding: 0.35rem 0.8rem;
      border-radius: 8px;
      font-size: 0.8rem;
      font-weight: 700;
      letter-spacing: 0.03em;
      box-shadow: 0 4px 10px rgba(222, 4, 4, 0.35);
    }
    .jc-product-card__info { padding: 0.9rem 1rem 1rem; display: flex; flex-direction: column; gap: 0.15rem; flex: 1; }
    .jc-product-card__brand { font-size: 0.7rem; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.06em; font-weight: 600; }
    .jc-product-card__title { font-size: 0.95rem; margin: 0.15rem 0; font-weight: 600; color: #0f172a; line-height: 1.3; }
    .jc-product-card__price { font-size: 1.15rem; font-weight: 700; margin: 0.35rem 0 0.45rem; color: #0f172a; }
    .jc-color-palette {
      display: flex;
      flex-wrap: wrap;
      gap: 0.45rem;
      margin: 0.15rem 0 0.75rem;
    }
    .jc-color-swatch {
      width: 1.55rem;
      height: 1.55rem;
      border-radius: 999px;
      border: 2px solid rgba(15, 23, 42, 0.15);
      cursor: pointer;
      padding: 0;
      box-shadow: inset 0 0 0 1px rgba(255,255,255,0.25);
      transition: transform 0.15s ease, box-shadow 0.15s ease;
    }
    .jc-color-swatch:hover { transform: scale(1.08); }
    .jc-color-swatch--active {
      box-shadow: 0 0 0 2px #fff, 0 0 0 4px #de0404;
      transform: scale(1.08);
    }
    .jc-color-swatch--light { border-color: #cbd5e1; }
    .jc-product-card__add { width: 100%; padding: 0.6rem 1rem; border: none; border-radius: 8px; background: #0f172a; color: #fff; font-size: 0.85rem; font-weight: 600; cursor: pointer; transition: background 0.2s; margin-top: auto; }
    .jc-product-card__add:hover { background: #1e293b; }
    .jc-product-card__add--soon { background: #e2e8f0; color: #94a3b8; cursor: not-allowed; }
    .jc-product-card__add--soon:hover { background: #e2e8f0; }
    .jc-gallery__loading, .jc-gallery__error { text-align: center; padding: 4rem 0; color: #64748b; }
    .jc-gallery__error { color: #ef5350; }
    .jc-lightbox { position: fixed; inset: 0; background: rgba(0,0,0,0.95); display: flex; align-items: center; justify-content: center; z-index: 9999; cursor: pointer; }
    .jc-lightbox__img { max-width: 90vw; max-height: 90vh; object-fit: contain; }
    .jc-lightbox__close { position: absolute; top: 1rem; right: 1.5rem; color: #fff; font-size: 2.5rem; background: none; border: none; cursor: pointer; }
  `]
})
export class GalleryComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private api = inject(ApiService);
  private cart = inject(CartService);
  private toast = inject(ToastService);

  readonly placeholders = PLACEHOLDERS;
  readonly products = signal<CatalogProduct[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly selectedBrand = signal('');
  readonly lightboxProduct = signal<CatalogProduct | null>(null);
  readonly brands = signal<string[]>([]);
  readonly title = signal('Catálogo');
  readonly brandFilter = signal('');
  readonly modelFilter = signal('');
  readonly searchTerm = signal('');
  readonly productLine = signal('');
  readonly filteredProducts = signal<CatalogProduct[]>([]);
  /** groupKey -> selected catalog product id */
  readonly selectedColorIds = signal<Record<string, number>>({});

  readonly productGroups = computed(() => groupProducts(this.filteredProducts()));

  readonly productLineLabel = computed(() => {
    const map: Record<string, string> = {
      'protectores-tanque': 'Protectores de Tanque',
      'calcas-motos': 'Calcas Motos',
      'calcas-rines': 'Calcas Rines',
      'emblemas': 'Emblemas'
    };
    return map[this.productLine()] || this.productLine();
  });

  ngOnInit() {
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      startWith(null),
      map(() => resolveGalleryFilters(this.route)),
      distinctUntilChanged(
        (a, b) => a.slug === b.slug && a.brand === b.brand && a.model === b.model && a.search === b.search,
      ),
      switchMap(({ slug, brand, model, search }) => {
        this.productLine.set(slug);
        this.brandFilter.set(brand);
        this.modelFilter.set(model);
        this.searchTerm.set(search);
        this.selectedBrand.set(brand);
        this.lightboxProduct.set(null);
        this.selectedColorIds.set({});

        if (slug) {
          this.title.set(this.productLineLabel());
        } else if (search) {
          this.title.set(`Resultados para "${search}"`);
        }

        this.loading.set(true);
        this.error.set('');

        const params = new URLSearchParams();
        if (slug) params.set('productLine', slug);
        if (brand) params.set('brand', brand);
        if (model) params.set('model', model);
        if (search) params.set('search', search);
        const qs = params.toString();

        return this.api.getPublic<CatalogProduct[]>(`catalog/products${qs ? '?' + qs : ''}`).pipe(
          map((products) => ({ products })),
          catchError(() => {
            this.error.set('No se pudieron cargar los productos.');
            return of({ products: [] as CatalogProduct[] });
          }),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(({ products }) => {
      this.products.set(products);
      this.brands.set([...new Set(products.map((p) => p.brand))].sort());
      this.filteredProducts.set(products);
      this.loading.set(false);
    });
  }

  filterBrand(brand: string) {
    this.selectedBrand.set(brand);
    this.filteredProducts.set(brand ? this.products().filter(p => p.brand === brand) : this.products());
  }

  selectedVariant(group: ProductGroup): CatalogProduct {
    const selectedId = this.selectedColorIds()[group.key];
    return group.variants.find((v) => v.id === selectedId) || group.variants[0];
  }

  selectColor(groupKey: string, productId: number) {
    this.selectedColorIds.update((map) => ({ ...map, [groupKey]: productId }));
  }

  displayTitle(group: ProductGroup, selected: CatalogProduct): string {
    if (group.variants.length > 1) {
      return selected.color ? `${group.baseTitle} — ${selected.color}` : group.baseTitle;
    }
    return selected.title;
  }

  swatchColor(color?: string | null): string {
    if (!color) return '#64748b';
    const key = color.trim().toLowerCase();
    if (COLOR_SWATCHES[key]) return COLOR_SWATCHES[key];
    // fallback hash for catalog-like labels
    let hash = 0;
    for (let i = 0; i < key.length; i++) hash = (hash * 31 + key.charCodeAt(i)) >>> 0;
    const hue = hash % 360;
    return `hsl(${hue} 55% 45%)`;
  }

  isLightColor(color?: string | null): boolean {
    const key = (color || '').trim().toLowerCase();
    return ['blanco', 'beige', 'plata', 'plateado', 'celeste', 'amarillo'].includes(key);
  }

  addToCart(product: CatalogProduct) {
    this.cart.add(product);
    this.toast.success(product.color ? `Agregado: ${product.color}` : 'Producto agregado al carrito');
  }

  openLightbox(product: CatalogProduct) {
    this.lightboxProduct.set(product);
  }
  closeLightbox() {
    this.lightboxProduct.set(null);
  }
}
