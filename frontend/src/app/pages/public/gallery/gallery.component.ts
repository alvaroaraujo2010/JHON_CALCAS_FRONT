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

const PLACEHOLDERS: PlaceholderProduct[] = [
  { id: -1, title: 'Protector Deportivo', brand: 'YAMAHA', model: 'MT-09', price: 89000, imageUrl: '', comingSoon: true },
  { id: -2, title: 'Calca Tanque Carbon', brand: 'HONDA', model: 'CRF', price: 65000, imageUrl: '', comingSoon: true },
  { id: -3, title: 'Kit Rines Racing', brand: 'SUZUKI', model: 'GSX', price: 120000, imageUrl: '', comingSoon: true },
  { id: -4, title: 'Emblema 3D', brand: 'KAWASAKI', model: 'Z900', price: 45000, imageUrl: '', comingSoon: true },
  { id: -5, title: 'Protector Full', brand: 'YAMAHA', model: 'R15', price: 95000, imageUrl: '', comingSoon: true },
  { id: -6, title: 'Calca Rines Premium', brand: 'HONDA', model: 'CBR', price: 78000, imageUrl: '', comingSoon: true },
];

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
        </nav>

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

          <div class="jc-gallery__grid">
            @for (p of displayItems(); track p.id) {
              <div class="jc-product-card">
                <div class="jc-product-card__image">
                  @if (p.comingSoon) {
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
                  } @else if (p.imageUrl) {
                    <img [src]="p.imageUrl" [alt]="p.title" loading="lazy" (click)="openLightbox($any(p))" />
                  } @else {
                    <div class="jc-product-card__placeholder">Sin imagen</div>
                  }
                  @if (p.model && !p.comingSoon) {
                    <span class="jc-product-card__model-badge">{{ p.menuModel || p.model }}</span>
                  }
                  @if (p.color && !p.comingSoon) {
                    <span class="jc-product-card__color-badge">{{ p.color }}</span>
                  }
                </div>
                <div class="jc-product-card__info">
                  <span class="jc-product-card__brand">{{ p.brand }}</span>
                  <h3 class="jc-product-card__title">{{ p.title }}</h3>
                  <span class="jc-product-card__price">\${{ p.price.toLocaleString('es-CO') }}</span>
                  @if (!p.comingSoon) {
                    <button class="jc-product-card__add" (click)="addToCart($any(p))">
                      Agregar al carrito
                    </button>
                  } @else {
                    <button class="jc-product-card__add jc-product-card__add--soon" disabled>Próximamente</button>
                  }
                </div>
              </div>
            }
          </div>
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
    .jc-breadcrumb a { color: #64748b; text-decoration: none; &:hover { color: #de0404; } }
    .jc-breadcrumb span { color: #0f172a; font-weight: 600; }
    .jc-breadcrumb-sep { color: #cbd5e1; pointer-events: none; }
    .jc-gallery { background: #f8fafc; padding: 2.5rem 0 5rem; min-height: 60vh; }
    .jc-gallery__brands { display: flex; flex-wrap: wrap; gap: 0.5rem; justify-content: center; margin-bottom: 2rem; }
    .jc-chip { padding: 0.5rem 1.25rem; border: 1px solid #cbd5e1; border-radius: 999px; background: #fff; cursor: pointer; font-size: 0.85rem; font-weight: 500; transition: all 0.2s; color: #475569; }
    .jc-chip:hover { border-color: #94a3b8; color: #0f172a; }
    .jc-chip--active { background: #0f172a; color: #fff; border-color: #0f172a; }
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
    .jc-product-card__model-badge { position: absolute; top: 0.6rem; left: 0.6rem; background: rgba(15,23,42,0.85); color: #fff; padding: 0.2rem 0.6rem; border-radius: 6px; font-size: 0.7rem; font-weight: 600; }
    .jc-product-card__color-badge { position: absolute; top: 0.6rem; right: 0.6rem; background: rgba(222,4,4,0.9); color: #fff; padding: 0.2rem 0.6rem; border-radius: 6px; font-size: 0.7rem; font-weight: 600; }
    .jc-product-card__info { padding: 0.9rem 1rem 1rem; display: flex; flex-direction: column; gap: 0.15rem; flex: 1; }
    .jc-product-card__brand { font-size: 0.7rem; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.06em; font-weight: 600; }
    .jc-product-card__title { font-size: 0.95rem; margin: 0.15rem 0; font-weight: 600; color: #0f172a; line-height: 1.3; }
    .jc-product-card__price { font-size: 1.15rem; font-weight: 700; margin: 0.35rem 0 0.6rem; color: #0f172a; }
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

  readonly products = signal<CatalogProduct[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly selectedBrand = signal('');
  readonly lightboxProduct = signal<CatalogProduct | null>(null);
  readonly brands = signal<string[]>([]);
  readonly title = signal('Catálogo');
  readonly brandFilter = signal('');
  readonly modelFilter = signal('');
  readonly productLine = signal('');
  readonly filteredProducts = signal<CatalogProduct[]>([]);

  readonly displayItems = computed(() => {
    const real = this.filteredProducts();
    if (real.length > 0) return real;
    return PLACEHOLDERS;
  });

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
        (a, b) => a.slug === b.slug && a.brand === b.brand && a.model === b.model,
      ),
      switchMap(({ slug, brand, model }) => {
        this.productLine.set(slug);
        this.brandFilter.set(brand);
        this.modelFilter.set(model);
        this.selectedBrand.set(brand);
        this.lightboxProduct.set(null);

        if (slug) {
          this.title.set(this.productLineLabel());
        }

        this.loading.set(true);
        this.error.set('');

        const params = new URLSearchParams();
        if (slug) params.set('productLine', slug);
        if (brand) params.set('brand', brand);
        if (model) params.set('model', model);
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

  addToCart(product: CatalogProduct) {
    this.cart.add(product);
    this.toast.success('Producto agregado al carrito');
  }

  openLightbox(product: CatalogProduct) {
    this.lightboxProduct.set(product);
  }
  closeLightbox() {
    this.lightboxProduct.set(null);
  }
}
