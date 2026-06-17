import { ActivatedRoute } from '@angular/router';

export interface MenuRouteLink {
  link: string[];
  queryParams?: Record<string, string>;
}

/** Convierte href del menú (con ?query) en routerLink + queryParams reales. */
export function parseMenuHref(href: string): MenuRouteLink | null {
  if (!href || href.startsWith('http://') || href.startsWith('https://') || href.startsWith('//')) {
    return null;
  }

  const queryIndex = href.indexOf('?');
  const path = queryIndex >= 0 ? href.slice(0, queryIndex) : href;
  const link = path.split('/').filter(Boolean);
  if (!link.length) return null;

  const queryParams: Record<string, string> = {};
  if (queryIndex >= 0) {
    new URLSearchParams(href.slice(queryIndex + 1)).forEach((value, key) => {
      queryParams[key] = value;
    });
  }

  return {
    link,
    queryParams: Object.keys(queryParams).length > 0 ? queryParams : undefined,
  };
}

export interface GalleryFilters {
  slug: string;
  brand: string;
  model: string;
}

/** Lee slug, marca y modelo desde la ruta (incluye query embebido en pathname). */
export function resolveGalleryFilters(route: ActivatedRoute): GalleryFilters {
  let slug = route.snapshot.paramMap.get('slug') || '';
  let brand = route.snapshot.queryParamMap.get('brand') || '';
  let model = route.snapshot.queryParamMap.get('model') || '';

  ({ slug, brand, model } = absorbEmbeddedQuery(slug, brand, model));

  if (typeof window !== 'undefined') {
    const decoded = decodeURIComponent(window.location.pathname);

    if (!slug) {
      const catMatch = decoded.match(/\/categoria\/([^/?#]+)/);
      if (catMatch) slug = catMatch[1];
    }

    if (!brand || !model) {
      const search = new URLSearchParams(window.location.search);
      if (!brand) brand = search.get('brand') || '';
      if (!model) model = search.get('model') || '';
    }

    const pathQm = decoded.indexOf('?');
    if (pathQm >= 0 && (!brand || !model)) {
      const embedded = new URLSearchParams(decoded.slice(pathQm + 1));
      if (!brand) brand = embedded.get('brand') || '';
      if (!model) model = embedded.get('model') || '';
    }
  }

  return { slug, brand, model };
}

function absorbEmbeddedQuery(slug: string, brand: string, model: string): GalleryFilters {
  const qm = slug.indexOf('?');
  if (qm < 0) return { slug, brand, model };

  const embedded = new URLSearchParams(slug.slice(qm + 1));
  return {
    slug: slug.slice(0, qm),
    brand: brand || embedded.get('brand') || '',
    model: model || embedded.get('model') || '',
  };
}
