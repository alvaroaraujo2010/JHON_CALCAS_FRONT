import { Component, computed, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { CatalogProduct, Product } from '../../../core/models';
import { ToastService } from '../../../core/services/toast.service';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-gallery-admin',
  standalone: true,
  imports: [ReactiveFormsModule, ModuleHeaderComponent],
  template: `
    <div class="module-page">
      <app-module-header
        title="Galería de productos"
        subtitle="Administra los productos del catálogo público"
        badge="Catálogo web"
      >
        <button class="btn" type="button" (click)="toggleForm()">
          {{ showForm() ? 'Cancelar' : 'Nuevo producto' }}
        </button>
      </app-module-header>

      @if (showForm()) {
        <form class="card-form" [formGroup]="form" (ngSubmit)="save()">
          <div class="card-form__head">
            <h3>{{ editing() ? 'Editar producto' : 'Nuevo producto' }}</h3>
            <p>Imágenes y datos visibles en el sitio web</p>
          </div>
          <div class="form-grid">
            <label>Línea de producto *
              <select formControlName="productLine">
                <option value="">Selecciona</option>
                <option value="protectores-tanque">Protectores de tanque</option>
                <option value="calcas-motos">Calcas motos</option>
                <option value="calcas-rines">Calcas rines</option>
                <option value="emblemas">Emblemas</option>
                <option value="otros">Otros</option>
              </select>
            </label>
            <label>Marca *<input type="text" formControlName="brand" /></label>
            <label>Modelo (carpeta)<input type="text" formControlName="model" placeholder="Ej: CB125F, NAVI REF 001" /></label>
            <label>Modelo menú *<input type="text" formControlName="menuModel" placeholder="Ej: CB, NAVI, FZ 25" /></label>
            <label>Referencia diseño<input type="text" formControlName="designRef" placeholder="REF 001, V1, V2" /></label>
            <label>Color<input type="text" formControlName="color" placeholder="Azul, Rojo, Dorado" /></label>
            <label>Título *<input type="text" formControlName="title" /></label>
            <label>Precio *<input type="number" step="0.01" formControlName="price" /></label>
            <label>Producto inventario
              <select formControlName="internalProductId">
                <option value="">Sin vínculo (solo web)</option>
                @for (p of inventoryProducts(); track p.id) {
                  <option [value]="p.id">{{ p.sku }} — {{ p.name }} (stock: {{ p.stock }})</option>
                }
              </select>
            </label>
            <label class="full">Descripción<textarea formControlName="description" rows="3"></textarea></label>
            <label class="full">Imagen
              <input type="file" (change)="onFileChange($event)" accept="image/*" />
              @if (previewUrl(); as url) {
                <img [src]="url" alt="Vista previa" class="gallery-preview" />
              }
            </label>
          </div>
          <p class="gallery-hint">El modelo menú debe coincidir con el menú del sitio. Vincule inventario para descontar stock al marcar el pedido como pagado.</p>
          <div class="form-actions">
            <button class="btn primary" type="submit" [disabled]="form.invalid || saving()">
              {{ saving() ? 'Guardando...' : (editing() ? 'Actualizar' : 'Guardar') }}
            </button>
          </div>
        </form>
      }

      <section class="data-card">
        <div class="data-card__head">
          <h2>Listado de productos</h2>
          <span>{{ filteredProducts().length }} registro(s)</span>
        </div>
        <input
          type="search"
          class="data-card__search"
          placeholder="Buscar por título, marca, modelo..."
          (input)="searchTerm.set($any($event.target).value)"
        />
        <div class="gallery-table-scroll">
          <table class="data">
            <thead>
              <tr>
                <th>Imagen</th><th>Línea</th><th>Marca</th><th>Menú</th><th>Color</th>
                <th>Título</th><th>Inventario</th><th>Precio</th><th>Estado</th><th></th>
              </tr>
            </thead>
            <tbody>
              @for (p of filteredProducts(); track p.id) {
                <tr>
                  <td>
                    @if (p.imageUrl) {
                      <img [src]="p.imageUrl" alt="" width="48" height="48" class="gallery-thumb" />
                    } @else {
                      <div class="gallery-thumb gallery-thumb--empty">Sin imagen</div>
                    }
                  </td>
                  <td>{{ p.productLine }}</td>
                  <td>{{ p.brand }}</td>
                  <td>{{ p.menuModel || p.model }}</td>
                  <td>{{ p.color || '—' }}</td>
                  <td><strong>{{ p.title }}</strong></td>
                  <td>
                    @if (p.internalProductSku) {
                      <span class="gallery-sku">{{ p.internalProductSku }}</span>
                    } @else {
                      —
                    }
                  </td>
                  <td>\${{ p.price.toLocaleString('es-CO') }}</td>
                  <td><span class="badge-ok">{{ p.isActive ? 'Activo' : 'Inactivo' }}</span></td>
                  <td>
                    <button class="btn sm" type="button" (click)="edit(p)">Editar</button>
                    <button class="btn sm danger" type="button" (click)="toggleActive(p)">
                      {{ p.isActive ? 'Desactivar' : 'Activar' }}
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="10">
                    <div class="empty-state">
                      <strong>Sin productos</strong>
                      <p>Cree el primer producto del catálogo público.</p>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .gallery-table-scroll {
      max-height: min(62vh, 720px);
      overflow: auto;
    }
    .gallery-table-scroll table.data th {
      position: sticky;
      top: 0;
      z-index: 1;
      box-shadow: 0 1px 0 #e2e8f0;
    }
    .gallery-thumb {
      width: 48px;
      height: 48px;
      object-fit: cover;
      border-radius: 6px;
      display: block;
    }
    .gallery-thumb--empty {
      display: grid;
      place-items: center;
      background: #f1f5f9;
      color: #94a3b8;
      font-size: 0.62rem;
      line-height: 1;
      text-align: center;
      border: 1px dashed #cbd5e1;
    }
    .gallery-preview {
      display: block;
      margin-top: 0.5rem;
      max-width: 120px;
      max-height: 120px;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
    }
    .gallery-hint {
      margin: -0.5rem 0 0;
      font-size: 0.85rem;
      color: #64748b;
    }
    .gallery-sku {
      font-size: 0.8rem;
      font-weight: 600;
      color: #475569;
    }
  `]
})
export class GalleryAdminComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private toast = inject(ToastService);

  readonly products = signal<CatalogProduct[]>([]);
  readonly inventoryProducts = signal<Product[]>([]);
  readonly searchTerm = signal('');
  readonly filteredProducts = computed(() => {
    const t = this.searchTerm().toLowerCase();
    if (!t) return this.products();
    return this.products().filter(p => p.title?.toLowerCase().includes(t) || p.brand?.toLowerCase().includes(t) || p.model?.toLowerCase().includes(t) || p.menuModel?.toLowerCase().includes(t) || p.productLine?.toLowerCase().includes(t));
  });
  readonly showForm = signal(false);
  readonly editing = signal<CatalogProduct | null>(null);
  readonly saving = signal(false);
  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl = signal<string>('');

  readonly form = this.fb.group({
    productLine: ['', Validators.required],
    brand: ['', Validators.required],
    model: [''],
    menuModel: ['', Validators.required],
    designRef: [''],
    color: [''],
    title: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    internalProductId: [''],
    description: ['']
  });

  ngOnInit() {
    this.loadProducts();
    this.api.get<Product[]>('products').subscribe({
      next: (data) => this.inventoryProducts.set(data.filter((p) => p.isActive)),
      error: () => this.toast.error('No se pudieron cargar productos de inventario'),
    });
  }

  toggleForm() {
    if (this.showForm()) {
      this.cancelForm();
    } else {
      this.editing.set(null);
      this.form.reset({ price: 0, menuModel: '', designRef: '', color: '', internalProductId: '' });
      this.selectedFile.set(null);
      this.previewUrl.set('');
      this.showForm.set(true);
    }
  }

  private loadProducts() {
    this.api.get<CatalogProduct[]>('catalog/admin').subscribe({
      next: (data) => this.products.set(this.sortWithImagesFirst(data)),
      error: () => this.toast.error('No se pudieron cargar los productos')
    });
  }

  private sortWithImagesFirst(data: CatalogProduct[]) {
    return [...data].sort((a, b) => {
      const ai = a.imageUrl ? 0 : 1;
      const bi = b.imageUrl ? 0 : 1;
      if (ai !== bi) return ai - bi;
      return a.title.localeCompare(b.title, 'es');
    });
  }

  onFileChange(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0] || null;
    this.selectedFile.set(file);
    if (file) {
      const reader = new FileReader();
      reader.onload = () => this.previewUrl.set(reader.result as string);
      reader.readAsDataURL(file);
    } else {
      this.previewUrl.set('');
    }
  }

  edit(product: CatalogProduct) {
    this.editing.set(product);
    this.form.patchValue({
      productLine: product.productLine,
      brand: product.brand,
      model: product.model || '',
      menuModel: product.menuModel || product.model || '',
      designRef: product.designRef || '',
      color: product.color || '',
      title: product.title,
      price: product.price,
      internalProductId: product.internalProductId ? String(product.internalProductId) : '',
      description: product.description || ''
    });
    this.previewUrl.set(product.imageUrl || '');
    this.showForm.set(true);
  }

  cancelForm() {
    this.showForm.set(false);
    this.editing.set(null);
    this.form.reset({ price: 0, menuModel: '', designRef: '', color: '', internalProductId: '' });
    this.selectedFile.set(null);
    this.previewUrl.set('');
  }

  save() {
    if (this.form.invalid) return;
    this.saving.set(true);
    const fv = this.form.value;
    const fd = new FormData();
    fd.append('productLine', fv.productLine!);
    fd.append('brand', fv.brand!);
    if (fv.model) fd.append('model', fv.model);
    fd.append('menuModel', fv.menuModel!);
    if (fv.designRef) fd.append('designRef', fv.designRef);
    if (fv.color) fd.append('color', fv.color);
    fd.append('title', fv.title!);
    fd.append('price', String(fv.price!));
    fd.append('internalProductId', fv.internalProductId ? String(fv.internalProductId) : '');
    if (fv.description) fd.append('description', fv.description);
    if (this.selectedFile()) fd.append('file', this.selectedFile()!);

    const edit = this.editing();
    const req = edit
      ? this.api.putForm<CatalogProduct>(`catalog/${edit.id}`, fd)
      : this.api.postForm<CatalogProduct>('catalog', fd);

    req.subscribe({
      next: () => {
        this.toast.success(edit ? 'Producto actualizado' : 'Producto creado');
        this.cancelForm();
        this.loadProducts();
        this.saving.set(false);
      },
      error: () => {
        this.toast.error('Error al guardar el producto');
        this.saving.set(false);
      }
    });
  }

  toggleActive(product: CatalogProduct) {
    this.api.put(`catalog/${product.id}/toggle-active`, {}).subscribe({
      next: () => this.loadProducts(),
      error: () => this.toast.error('Error al cambiar estado')
    });
  }
}
