/** Rutas de marca en `public/images` (servidas en `/images/...`). */
export const BRAND_IMAGES = {
  logoWhite: '/images/contanexo_logo_blanco_transparente.png',
  logoBlack: '/images/contanexo_logo_negro_transparente.png',
  logoColor: '/images/contanexo_logo_color_transparente.png',
  iconWhite: '/images/contanexo_icon_blanco_transparente.png',
  iconBlack: '/images/contanexo_icon_negro_transparente.png',
  iconColor: '/images/contanexo_icon_color_transparente.png',
  heroBg: '/images/hero-bg.jpg',
  favicon32: '/images/favicon-32x32.png',
  favicon64: '/images/favicon-64x64.png',
  favicon180: '/images/favicon-180x180.png',
  favicon192: '/images/favicon-192x192.png',
  favicon512: '/images/favicon-512x512.png'
} as const;

export type BrandLogoVariant =
  | 'white'
  | 'black'
  | 'color'
  | 'icon-white'
  | 'icon-black'
  | 'icon-color';

export function brandLogoSrc(variant: BrandLogoVariant): string {
  const map: Record<BrandLogoVariant, string> = {
    white: BRAND_IMAGES.logoWhite,
    black: BRAND_IMAGES.logoBlack,
    color: BRAND_IMAGES.logoColor,
    'icon-white': BRAND_IMAGES.iconWhite,
    'icon-black': BRAND_IMAGES.iconBlack,
    'icon-color': BRAND_IMAGES.iconColor
  };
  return map[variant];
}
