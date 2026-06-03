import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';
import { PayrollService } from '../../../core/services/payroll.service';
import { Payroll } from '../../../core/models';

@Component({
  selector: 'app-payroll',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './payroll.component.html',
  styleUrl: './payroll.component.scss'
})
export class PayrollComponent implements OnInit, OnDestroy {
  private svc = inject(PayrollService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  payrolls = signal<Payroll[]>([]);
  filterStatus = signal<string>('');
  showForm = false;
  saving = false;

  form = this.fb.group({
    periodStart: ['', Validators.required],
    periodEnd:   ['', Validators.required],
    paymentDate: [''],
    notes:       ['']
  });

  loading = signal(true);

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.reload());
  }

  ngOnDestroy() { this.navSub?.unsubscribe(); }

  reload() {
    this.loading.set(true);
    this.svc.getPayrolls().subscribe({
      next: d => { this.payrolls.set(d); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openForm() {
    this.showForm = true;
    this.form.reset({ periodStart: '', periodEnd: '', paymentDate: '', notes: '' });
  }

  save() {
    if (this.form.invalid || this.saving) return;
    this.saving = true;
    const v = this.form.getRawValue();
    this.svc.createPayroll({
      periodStart: v.periodStart!,
      periodEnd:   v.periodEnd!,
      paymentDate: v.paymentDate ?? undefined,
      notes:       v.notes ?? undefined,
      status: 'draft'
    } as any).subscribe({
      next: created => {
        // Inmediatamente procesa para que el backend calcule todo automáticamente
        this.svc.processPayroll(created.id).subscribe({
          next: () => { this.showForm = false; this.saving = false; this.reload(); },
          error: () => { this.showForm = false; this.saving = false; this.reload(); }
        });
      },
      error: () => { this.saving = false; }
    });
  }

  viewDetails(p: Payroll) { this.router.navigate(['/admin/nomina', p.id]); }

  processPayroll(p: Payroll) {
    if (!confirm(`¿Procesar nómina del período ${p.periodStart} al ${p.periodEnd}?\nSe calcularán prestaciones, seguridad social, retención y provisiones.`)) return;
    this.svc.processPayroll(p.id).subscribe({ next: () => this.reload() });
  }

  payPayroll(p: Payroll) {
    const date = prompt('Fecha de pago (YYYY-MM-DD):', new Date().toISOString().split('T')[0]);
    if (!date) return;
    this.svc.payPayroll(p.id, { paymentDate: date, paymentMethod: 'transfer' }).subscribe({
      next: () => { alert('Nómina pagada correctamente'); this.reload(); }
    });
  }

  delete(p: Payroll) {
    if (!confirm(`¿Eliminar nómina del período ${p.periodStart}?`)) return;
    this.svc.deletePayroll(p.id).subscribe({ next: () => this.reload() });
  }

  getFilteredPayrolls(): Payroll[] {
    const s = this.filterStatus();
    return s ? this.payrolls().filter(p => p.status === s) : this.payrolls();
  }

  statusLabel(s: string) {
    return ({ draft: 'Borrador', processed: 'Procesada', paid: 'Pagada', cancelled: 'Cancelada' } as any)[s] ?? s;
  }

  statusColor(s: string) {
    return ({ draft: '#f59e0b', processed: '#3b82f6', paid: '#22c55e', cancelled: '#94a3b8' } as any)[s] ?? '#94a3b8';
  }

  fmt(n: number | undefined | null): string {
    return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(n ?? 0);
  }
}
