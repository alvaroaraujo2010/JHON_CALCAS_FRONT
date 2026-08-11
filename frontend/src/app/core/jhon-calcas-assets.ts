/** Assets y contenido de marca Jhon Calcas (sitio publico). */
export const JHON_CALCAS_IMAGES = {
  logo: '/images/jhoncalcas/logo.png',
  heroPoster: '/images/jhoncalcas/hero-poster.jpg',
  whatsapp: '/images/jhoncalcas/whatsapp.png',
  favicon: '/images/jhoncalcas/logo.png',
  payments: {
    mercadoPago: '/images/jhoncalcas/pago-mercadopago.png',
    visa: '/images/jhoncalcas/pago-visa.png',
    mastercard: '/images/jhoncalcas/pago-mastercard.png',
    amex: '/images/jhoncalcas/pago-amex.png',
    diners: '/images/jhoncalcas/pago-diners.png'
  }
} as const;

export const JHON_CALCAS_WHATSAPP_URL = 'https://wa.link/122ehn';

/** Enlaces de redes (mismo orden del banner hero). left/width en % del banner. */
export const JHON_CALCAS_SOCIAL_LINKS = [
  {
    label: 'Instagram',
    href: 'https://www.instagram.com/jhoncalcas',
    left: 38.1,
    width: 2.8,
  },
  {
    label: 'Facebook',
    href: 'https://www.facebook.com/people/Jhoncalcas/100063970326433/',
    left: 41.4,
    width: 2.9,
  },
  {
    label: 'TikTok',
    href: 'https://www.tiktok.com/@jhoncalcasmedellin',
    left: 44.9,
    width: 2.4,
  },
  {
    label: 'YouTube',
    href: 'https://www.youtube.com/@jhoncalcas6757',
    left: 47.6,
    width: 2.9,
  },
  {
    label: 'Google Maps — Tienda física',
    href: 'https://www.google.com/maps/search/?api=1&query=Centro+Comercial+Florida+Cl.+71+%2365-150+Medell%C3%ADn',
    left: 51.0,
    width: 2.5,
  },
  {
    label: 'Mercado Libre',
    href: 'https://listado.mercadolibre.com.co/jhoncalcas',
    left: 54.0,
    width: 8.2,
  },
] as const;

export interface JhonCalcasSection {
  id: string;
  title: string;
  description: string;
  image: string;
  collectionUrl: string;
  imagePosition?: string;
  reverse?: boolean;
}

export const JHON_CALCAS_SECTIONS: JhonCalcasSection[] = [
  {
    id: 'calcas-moto',
    title: 'Calcas para moto',
    description:
      'Personaliza tu moto con nuestras calcomanías duraderas y de alta calidad. ¡Dale un toque único a tu motocicleta con estilos que resaltan tu personalidad!',
    image: '/images/jhoncalcas/calcas-moto.png',
    collectionUrl: 'https://jhoncalcas.com/collections/calcas-motos'
  },
  {
    id: 'calcas-rines',
    title: 'Calcas para rines',
    description:
      'Calcomanía para rines en vinilo y reflectivo, resistente al agua y decoloración, sin necesidad de barniz. Diseño universal que se adapta a cualquier moto, aportando estilo y protección.',
    image: '/images/jhoncalcas/calcas-rines.png',
    collectionUrl: 'https://jhoncalcas.com/collections/calcomanias-para-rines',
    imagePosition: '54.5% 78.8%',
    reverse: true
  },
  {
    id: 'protectores-tanque',
    title: 'Protectores de tanque',
    description:
      'Protege el tanque de tu moto con nuestros protectores de alta adherencia, diseñados para prevenir rayones. Fáciles de instalar y remover, ofrecen una solución práctica y eficiente para mantener tu moto impecable y libre de daños.',
    image: '/images/jhoncalcas/protectores-tanque.jpg',
    collectionUrl: 'https://jhoncalcas.com/collections/protectores-de-tanque'
  },
  {
    id: 'emblemas',
    title: 'Emblemas',
    description:
      'Dale un estilo único a tu moto con nuestros emblemas para motocicletas. Fáciles de aplicar, resistentes a la intemperie y diseñados tipo original. Estos emblemas aportan personalidad y durabilidad, asegurando un acabado profesional y personalizada.',
    image: '/images/jhoncalcas/emblemas.jpg',
    collectionUrl: 'https://jhoncalcas.com/collections/emblemas',
    reverse: true
  }
];
