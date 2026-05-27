export interface JhonCalcasMenuNode {
  label: string;
  href?: string;
  children?: JhonCalcasMenuNode[];
}

export interface JhonCalcasNavItem extends JhonCalcasMenuNode {
  children: JhonCalcasMenuNode[];
}
