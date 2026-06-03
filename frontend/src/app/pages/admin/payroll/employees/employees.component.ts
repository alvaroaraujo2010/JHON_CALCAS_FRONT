import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { PayrollService } from '../../../../core/services/payroll.service';
import { ModuleHeaderComponent } from '../../../../shared/module-header/module-header.component';
import { Employee } from '../../../../core/models';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './employees.component.html',
  styleUrl: './employees.component.scss'
})
export class EmployeesComponent implements OnInit {
  private svc = inject(PayrollService);
  private fb = inject(FormBuilder);

  employees = signal<Employee[]>([]);
  loading = signal(true);
  showForm = false;
  editingId: number | null = null;
  saving = false;
  showTerminate = false;
  terminatingId: number | null = null;

  contractTypes = [
    { value: 'indefinido', label: 'Término indefinido' },
    { value: 'fijo', label: 'Término fijo' },
    { value: 'obra_labor', label: 'Obra o labor' },
    { value: 'prestacion', label: 'Prestación de servicios' }
  ];

  terminationReasons = [
    { value: 'retiro_voluntario', label: 'Retiro voluntario' },
    { value: 'sin_justa_causa', label: 'Despido sin justa causa (genera indemnización)' },
    { value: 'justa_causa', label: 'Despido con justa causa' },
    { value: 'pension', label: 'Pensión' },
    { value: 'vencimiento_contrato', label: 'Vencimiento de contrato' }
  ];

  form = this.fb.group({
    name:        ['', Validators.required],
    email:       [''],
    phone:       [''],
    position:    [''],
    department:  [''],
    hireDate:    [new Date().toISOString().split('T')[0], Validators.required],
    taxId:       [''],
    bankAccount: [''],
    bankName:    [''],
    bankAccountType: ['savings'],
    baseSalary:  [0, [Validators.required, Validators.min(1)]],
    isActive:    [true],
    contractType: ['indefinido'],
    integralSalary: [false],
    withholdingProcedure2: [false],
    transportAllowanceOverride: [null as boolean | null]
  });

  terminateForm = this.fb.group({
    terminationDate: [new Date().toISOString().split('T')[0], Validators.required],
    terminationReason: ['retiro_voluntario', Validators.required]
  });

  ngOnInit() { this.reload(); }

  reload() {
    this.loading.set(true);
    this.svc.getEmployees().subscribe({
      next: data => { this.employees.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openForm() {
    this.editingId = null;
    this.showForm = true;
    this.form.reset({
      isActive: true,
      hireDate: new Date().toISOString().split('T')[0],
      baseSalary: 0,
      contractType: 'indefinido',
      integralSalary: false,
      withholdingProcedure2: false,
      transportAllowanceOverride: null,
      bankAccountType: 'savings'
    });
  }

  edit(emp: Employee) {
    this.editingId = emp.id;
    this.showForm = true;
    this.form.patchValue({
      name: emp.name, email: emp.email ?? '', phone: emp.phone ?? '',
      position: emp.position ?? '', department: emp.department ?? '',
      hireDate: emp.hireDate, taxId: emp.taxId ?? '',
      bankAccount: emp.bankAccount ?? '', bankName: emp.bankName ?? '',
      bankAccountType: emp.bankAccountType ?? 'savings',
      baseSalary: emp.baseSalary, isActive: emp.isActive,
      contractType: emp.contractType ?? 'indefinido',
      integralSalary: emp.integralSalary ?? false,
      withholdingProcedure2: emp.withholdingProcedure2 ?? false,
      transportAllowanceOverride: emp.transportAllowanceOverride ?? null
    });
  }

  save() {
    if (this.form.invalid || this.saving) return;
    this.saving = true;
    const v = this.form.getRawValue();
    const body: Partial<Employee> = {
      name: v.name!, email: v.email!, phone: v.phone!,
      position: v.position!, department: v.department!,
      hireDate: v.hireDate!, taxId: v.taxId!,
      bankAccount: v.bankAccount!, bankName: v.bankName!,
      bankAccountType: v.bankAccountType!,
      baseSalary: v.baseSalary!, isActive: v.isActive!,
      contractType: v.contractType as any,
      integralSalary: v.integralSalary!,
      withholdingProcedure2: v.withholdingProcedure2!,
      transportAllowanceOverride: v.transportAllowanceOverride
    };
    const req = this.editingId
      ? this.svc.updateEmployee(this.editingId, body)
      : this.svc.createEmployee(body as Omit<Employee, 'id'>);

    req.subscribe({
      next: () => { this.showForm = false; this.saving = false; this.reload(); },
      error: () => { this.saving = false; }
    });
  }

  openTerminate(emp: Employee) {
    this.terminatingId = emp.id;
    this.showTerminate = true;
    this.terminateForm.reset({
      terminationDate: new Date().toISOString().split('T')[0],
      terminationReason: 'retiro_voluntario'
    });
  }

  confirmTerminate() {
    if (this.terminateForm.invalid || !this.terminatingId) return;
    const v = this.terminateForm.getRawValue();
    const emp = this.employees().find(e => e.id === this.terminatingId);
    if (!emp) return;
    const body: Partial<Employee> = {
      ...emp,
      terminationDate: v.terminationDate!,
      terminationReason: v.terminationReason!,
      isActive: false
    };
    this.svc.updateEmployee(this.terminatingId!, body).subscribe({
      next: () => { this.showTerminate = false; this.terminatingId = null; this.reload(); },
      error: () => {}
    });
  }

  close() { this.showForm = false; }
  closeTerminate() { this.showTerminate = false; this.terminatingId = null; }

  format(n: number): string {
    return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(n);
  }
}
