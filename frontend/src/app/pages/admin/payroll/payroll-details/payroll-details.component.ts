import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormArray } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PayrollService } from '../../../../core/services/payroll.service';
import { Payroll, Employee, Deduction, PayrollDetail } from '../../../../core/models';
import { ModuleHeaderComponent } from '../../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-payroll-details',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './payroll-details.component.html',
  styleUrl: './payroll-details.component.scss'
})
export class PayrollDetailsComponent implements OnInit {
  private payrollService = inject(PayrollService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  payroll = signal<Payroll | null>(null);
  employees = signal<Employee[]>([]);
  deductions = signal<Deduction[]>([]);
  loading = signal(true);
  saving = false;
  selectedDetail = signal<PayrollDetail | null>(null);

  form = this.fb.group({
    details: this.fb.array([])
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadData(parseInt(id));
  }

  loadData(id: number) {
    this.loading.set(true);
    this.payrollService.getPayroll(id).subscribe({
      next: (payroll) => {
        this.payroll.set(payroll);
        if (payroll.details?.length > 0) this.selectedDetail.set(payroll.details[0]);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
    this.payrollService.getEmployees().subscribe({
      next: data => this.employees.set(data)
    });
    this.payrollService.getDeductions().subscribe({
      next: data => this.deductions.set(data)
    });
  }

  selectDetail(d: PayrollDetail) { this.selectedDetail.set(d); }

  process() {
    const p = this.payroll();
    if (!p) return;
    if (!confirm(`¿Procesar nómina del período ${p.periodStart} - ${p.periodEnd}?\nSe calcularán prestaciones, retención y se generará el asiento contable.`)) return;
    this.saving = true;
    this.payrollService.processPayroll(p.id).subscribe({
      next: (updated) => { this.payroll.set(updated); this.saving = false; },
      error: (e) => { alert(e?.error?.message ?? 'Error al procesar'); this.saving = false; }
    });
  }

  pay() {
    const p = this.payroll();
    if (!p) return;
    const date = prompt('Fecha de pago (YYYY-MM-DD):', new Date().toISOString().split('T')[0] ?? '');
    if (!date) return;
    this.saving = true;
    this.payrollService.payPayroll(p.id, { paymentDate: date, paymentMethod: 'transfer' }).subscribe({
      next: () => { alert('Nómina pagada'); this.saving = false; this.loadData(p.id); },
      error: (e) => { alert(e?.error?.message ?? 'Error'); this.saving = false; }
    });
  }

  cancel() { this.router.navigate(['/admin/nomina']); }

  fmt(n: number | undefined | null): string {
    if (n == null) return '$0';
    return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(n);
  }

  statusLabel(s: string) {
    return ({ draft: 'Borrador', processed: 'Procesada', paid: 'Pagada', cancelled: 'Cancelada' } as any)[s] ?? s;
  }
  statusColor(s: string) {
    return ({ draft: '#f59e0b', processed: '#3b82f6', paid: '#22c55e', cancelled: '#94a3b8' } as any)[s] ?? '#94a3b8';
  }
}
