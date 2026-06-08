import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { PayrollService } from '../../../../core/services/payroll.service';
import { Employee, PayrollSettlement, SettlementRequest, SettlementSimulation } from '../../../../core/models';
import { ModuleHeaderComponent } from '../../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-settlements',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './settlements.component.html',
  styleUrl: './settlements.component.scss'
})
export class SettlementsComponent implements OnInit {
  private svc = inject(PayrollService);
  private fb = inject(FormBuilder);

  settlements = signal<PayrollSettlement[]>([]);
  employees = signal<Employee[]>([]);
  loading = signal(true);
  simulating = signal(false);
  saving = false;
  simulation = signal<SettlementSimulation | null>(null);
  showForm = false;
  showSimulation = signal(false);

  form = this.fb.group({
    employeeId: [0, Validators.required],
    settlementDate: [new Date().toISOString().split('T')[0], Validators.required],
    lastDayWorked: [''],
    variableAverage3Months: [0],
    primaAlreadyPaid: [0],
    vacationsAlreadyPaid: [0],
    notes: ['']
  });

  ngOnInit() { this.reload(); }

  reload() {
    this.loading.set(true);
    this.svc.getSettlements().subscribe({
      next: data => { this.settlements.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.svc.getEmployees().subscribe({
      next: data => this.employees.set(data.filter(e => e.isActive))
    });
  }

  openForm() {
    this.showForm = true;
    this.showSimulation.set(false);
    this.simulation.set(null);
    this.form.reset({
      settlementDate: new Date().toISOString().split('T')[0],
      variableAverage3Months: 0,
      primaAlreadyPaid: 0,
      vacationsAlreadyPaid: 0
    });
  }

  private buildRequest(): SettlementRequest | null {
    if (this.form.invalid) return null;
    const v = this.form.getRawValue();
    return {
      employeeId: v.employeeId!,
      settlementDate: v.settlementDate!,
      lastDayWorked: v.lastDayWorked || undefined,
      variableAverage3Months: Number(v.variableAverage3Months) || 0,
      primaAlreadyPaid: Number(v.primaAlreadyPaid) || 0,
      vacationsAlreadyPaid: Number(v.vacationsAlreadyPaid) || 0,
      notes: v.notes ?? ''
    };
  }

  simulate() {
    const req = this.buildRequest();
    if (!req) return;
    this.simulating.set(true);
    this.svc.simulateSettlement(req).subscribe({
      next: (res) => {
        this.simulation.set(res);
        this.showSimulation.set(true);
        this.simulating.set(false);
      },
      error: () => this.simulating.set(false)
    });
  }

  save() {
    const req = this.buildRequest();
    if (!req || this.saving) return;
    this.saving = true;
    this.svc.createSettlement(req).subscribe({
      next: () => { this.saving = false; this.showForm = false; this.reload(); },
      error: () => this.saving = false
    });
  }

  close() {
    this.showForm = false;
    this.simulation.set(null);
  }

  format(n: number | undefined): string {
    if (n == null) return '$0';
    return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(n);
  }

  statusLabel(s: string) {
    return ({ draft: 'Borrador', paid: 'Pagada', cancelled: 'Cancelada' } as any)[s] ?? s;
  }

  statusColor(s: string) {
    return ({ draft: '#f59e0b', paid: '#22c55e', cancelled: '#94a3b8' } as any)[s] ?? '#94a3b8';
  }
}
