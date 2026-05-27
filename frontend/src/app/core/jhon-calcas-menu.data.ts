import menuJson from './jhon-calcas-menu.json';
import { JhonCalcasNavItem } from './jhon-calcas-menu.types';

export const JHON_CALCAS_NAV_MENU = menuJson as JhonCalcasNavItem[];

/** Anclas en la landing para los titulos principales del menu. */
export const JHON_CALCAS_SECTION_ANCHORS: Record<string, string> = {
  'PROTECTORES DE TANQUE': '#protectores-tanque',
  'CALCAS MOTOS': '#calcas-moto',
  'CALCAS RINES': '#calcas-rines',
  OTROS: '#emblemas'
};
