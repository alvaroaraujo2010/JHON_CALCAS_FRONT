import { Injectable, computed, effect, signal } from '@angular/core';

export type JcTheme = 'dark' | 'light';

const STORAGE_KEY = 'jc-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<JcTheme>(ThemeService.readStored());
  readonly isDark = computed(() => this.theme() === 'dark');

  constructor() {
    effect(() => {
      const theme = this.theme();
      document.documentElement.setAttribute('data-jc-theme', theme);
      try {
        localStorage.setItem(STORAGE_KEY, theme);
      } catch {
        /* private mode / quota */
      }
    });
  }

  toggle() {
    this.theme.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }

  setTheme(theme: JcTheme) {
    this.theme.set(theme);
  }

  private static readStored(): JcTheme {
    try {
      const fromDom = document.documentElement.getAttribute('data-jc-theme');
      if (fromDom === 'light' || fromDom === 'dark') return fromDom;
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'light' || stored === 'dark') return stored;
    } catch {
      /* ignore */
    }
    return 'dark';
  }
}
