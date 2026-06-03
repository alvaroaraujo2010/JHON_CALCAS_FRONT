import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ModuleHeaderComponent } from '../../../../shared/module-header/module-header.component';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { PayrollService } from '../../../../core/services/payroll.service';
import { Deduction } from '../../../../core/models';

@Component({
  selector: 'app-deductions',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './deductions.component.html',
  styleUrl: './deductions.component.scss'
})
export class DeductionsComponent implements OnInit, OnDestroy {
  private payrollService = inject(PayrollService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private navSub?: Subscription;

  deductions = signal<Deduction[]>([]);
  showForm = false;
  editingId: number | null = null;
  saving = false;

  form = this.fb.group({
    name: ['', Validators.required],
    type: ['percentage', Validators.required],
    value: [0, [Validators.required, Validators.min(0)]],
    description: [''],
    isActive: [true]
  });

  ngOnInit() {
    this.reload();
    this.navSub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.reload());
  }

  ngOnDestroy() {
    this.navSub?.unsubscribe();
  }

  reload() {
    this.payrollService.getDeductions().subscribe({
      next: (data) => this.deductions.set(data),
      error: () => console.error('Error loading deductions')
    });
  }

  openForm() {
    this.editingId = null;
    this.showForm = true;
    this.form.reset({ type: 'percentage', isActive: true });
  }

  edit(deduction: Deduction) {
    this.editingId = deduction.id;
    this.showForm = true;
    this.form.patchValue({
      name: deduction.name,
      type: deduction.type,
      value: deduction.value,
      description: deduction.description || '',
      isActive: deduction.isActive
    });
  }

  save() {
    if (this.form.invalid || this.saving) return;
    this.saving = true;
    const body = this.form.getRawValue();

    const req = this.editingId
      ? this.payrollService.updateDeduction(this.editingId, body as any)
      : this.payrollService.createDeduction(body as any);

    req.subscribe({
      next: () => {
        this.showForm = false;
        this.editingId = null;
        this.form.reset({ type: 'percentage', isActive: true });
        this.saving = false;
        this.reload();
      },
      error: () => {
        this.saving = false;
      }
    });
  }

  delete(deduction: Deduction) {
    if (confirm(`¿Eliminar deducción "${deduction.name}"?`)) {
      this.payrollService.deleteDeduction(deduction.id).subscribe({
        next: () => this.reload(),
        error: () => console.error('Error deleting deduction')
      });
    }
  }
}
