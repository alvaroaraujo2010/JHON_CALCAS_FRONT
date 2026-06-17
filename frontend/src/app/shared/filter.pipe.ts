import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'filter', standalone: true })
export class FilterPipe implements PipeTransform {
  transform<T extends { name?: string; fullName?: string }>(items: T[], query: string): T[] {
    if (!query) return [];
    const t = query.toLowerCase();
    return items.filter(i => (i.name || i.fullName || '').toLowerCase().includes(t));
  }
}
