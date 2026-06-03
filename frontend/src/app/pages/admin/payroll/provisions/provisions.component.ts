import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PayrollService } from '../../../../core/services/payroll.service';
import { ModuleHeaderComponent } from '../../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-provisions',
  standalone: true,
  imports: [CommonModule, FormsModule, ModuleHeaderComponent],
  templateUrl: './provisions.component.html',
  styleUrl: './provisions.component.scss'
})
export class ProvisionsComponent implements OnInit {
  private svc = inject(PayrollService);

  year = signal(new Date().getFullYear());
  summary = signal<any[]>([]);
  details = signal<any[]>([]);
  loading = signal(true);
  view = signal<'summary' | 'detail'>('summary');

  totalPrima = computed(() => this.summary().reduce((s, x) => s + x.prima, 0));
  totalCesantias = computed(() => this.summary().reduce((s, x) => s + x.cesantias, 0));
  totalIntereses = computed(() => this.summary().reduce((s, x) => s + x.cesantiasInterest, 0));
  totalVacaciones = computed(() => this.summary().reduce((s, x) => s + x.vacaciones, 0));
  totalGeneral = computed(() => this.summary().reduce((s, x) => s + x.total, 0));

  ngOnInit() { this.reload(); }

  reload() {
    this.loading.set(true);
    this.svc.getProvisionSummary(this.year()).subscribe({
      next: data => { this.summary.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.svc.getProvisions(this.year()).subscribe({
      next: data => this.details.set(data)
    });
  }

  setView(v: 'summary' | 'detail') { this.view.set(v); }

  format(n: number | undefined): string {
    if (n == null) return '$0';
    return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(n);
  }
}
