import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { LegalParameterService } from '../../../../core/services/legal-parameter.service';
import { LegalParameter } from '../../../../core/models';
import { ModuleHeaderComponent } from '../../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-legal-parameters',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './legal-parameters.component.html',
  styleUrl: './legal-parameters.component.scss'
})
export class LegalParametersComponent implements OnInit {
  private svc = inject(LegalParameterService);
  private fb = inject(FormBuilder);

  parameters = signal<LegalParameter[]>([]);
  selected = signal<LegalParameter | null>(null);
  saving = false;
  loading = signal(true);

  form = this.fb.group({
    year: [new Date().getFullYear(), [Validators.required, Validators.min(2000)]],
    smlmv: [1_423_500, [Validators.required, Validators.min(0)]],
    uvt: [49_799, [Validators.required, Validators.min(0)]],
    transportAllowance: [200_000, [Validators.required, Validators.min(0)]],
    transportAllowanceTop: [2, [Validators.required, Validators.min(1)]],
    minimumWithholdingUvt: [95, [Validators.required, Validators.min(0)]],
    exemptIncomeUvt: [240, [Validators.required, Validators.min(0)]],
    maxHealthIbcSmlmv: [25, [Validators.required, Validators.min(1)]],
    arlRiskOneRate: [0.522, [Validators.required, Validators.min(0)]],
    employerHealthRate: [8.5, [Validators.required, Validators.min(0)]],
    employerPensionRate: [12.0, [Validators.required, Validators.min(0)]],
    compensationFundRate: [4.0, [Validators.required, Validators.min(0)]],
    senaRate: [2.0, [Validators.required, Validators.min(0)]],
    icbfRate: [3.0, [Validators.required, Validators.min(0)]],
    employeeHealthRate: [4.0, [Validators.required, Validators.min(0)]],
    employeePensionRate: [4.0, [Validators.required, Validators.min(0)]],
    solidarityFundLowRate: [1.0, [Validators.required, Validators.min(0)]],
    solidarityFundHighRate: [1.2, [Validators.required, Validators.min(0)]],
    primaYearFraction: [1, [Validators.required, Validators.min(0)]],
    cesantiasYearFraction: [1, [Validators.required, Validators.min(0)]],
    cesantiasInterestRate: [12, [Validators.required, Validators.min(0)]],
    vacationDaysPerYear: [15, [Validators.required, Validators.min(0)]],
    effectiveFrom: [new Date().toISOString().split('T')[0], Validators.required],
    notes: ['']
  });

  ngOnInit() { this.reload(); }

  reload() {
    this.loading.set(true);
    this.svc.list().subscribe({
      next: data => {
        this.parameters.set(data);
        this.loading.set(false);
        if (data.length > 0) this.select(data[0]);
      },
      error: () => this.loading.set(false)
    });
  }

  select(p: LegalParameter) {
    this.selected.set(p);
    this.form.patchValue({
      year: p.year,
      smlmv: p.smlmv,
      uvt: p.uvt,
      transportAllowance: p.transportAllowance,
      transportAllowanceTop: p.transportAllowanceTop,
      minimumWithholdingUvt: p.minimumWithholdingUvt,
      exemptIncomeUvt: p.exemptIncomeUvt,
      maxHealthIbcSmlmv: p.maxHealthIbcSmlmv,
      arlRiskOneRate: p.arlRiskOneRate,
      employerHealthRate: p.employerHealthRate,
      employerPensionRate: p.employerPensionRate,
      compensationFundRate: p.compensationFundRate,
      senaRate: p.senaRate,
      icbfRate: p.icbfRate,
      employeeHealthRate: p.employeeHealthRate,
      employeePensionRate: p.employeePensionRate,
      solidarityFundLowRate: p.solidarityFundLowRate,
      solidarityFundHighRate: p.solidarityFundHighRate,
      primaYearFraction: p.primaYearFraction,
      cesantiasYearFraction: p.cesantiasYearFraction,
      cesantiasInterestRate: p.cesantiasInterestRate,
      vacationDaysPerYear: p.vacationDaysPerYear,
      effectiveFrom: p.effectiveFrom?.split('T')[0] ?? new Date().toISOString().split('T')[0],
      notes: p.notes ?? ''
    });
  }

  newParam() {
    this.selected.set(null);
    this.form.reset({
      year: new Date().getFullYear() + 1,
      effectiveFrom: `${new Date().getFullYear() + 1}-01-01`
    });
  }

  save() {
    if (this.form.invalid || this.saving) return;
    this.saving = true;
    const v = this.form.getRawValue();
    const body: LegalParameter = {
      year: Number(v.year),
      smlmv: Number(v.smlmv),
      uvt: Number(v.uvt),
      transportAllowance: Number(v.transportAllowance),
      transportAllowanceTop: Number(v.transportAllowanceTop),
      minimumWithholdingUvt: Number(v.minimumWithholdingUvt),
      exemptIncomeUvt: Number(v.exemptIncomeUvt),
      maxHealthIbcSmlmv: Number(v.maxHealthIbcSmlmv),
      arlRiskOneRate: Number(v.arlRiskOneRate),
      employerHealthRate: Number(v.employerHealthRate),
      employerPensionRate: Number(v.employerPensionRate),
      compensationFundRate: Number(v.compensationFundRate),
      senaRate: Number(v.senaRate),
      icbfRate: Number(v.icbfRate),
      employeeHealthRate: Number(v.employeeHealthRate),
      employeePensionRate: Number(v.employeePensionRate),
      solidarityFundLowRate: Number(v.solidarityFundLowRate),
      solidarityFundHighRate: Number(v.solidarityFundHighRate),
      primaYearFraction: Number(v.primaYearFraction),
      cesantiasYearFraction: Number(v.cesantiasYearFraction),
      cesantiasInterestRate: Number(v.cesantiasInterestRate),
      vacationDaysPerYear: Number(v.vacationDaysPerYear),
      effectiveFrom: v.effectiveFrom!,
      notes: v.notes ?? ''
    };
    this.svc.upsert(body.year, body).subscribe({
      next: () => { this.saving = false; this.reload(); },
      error: () => { this.saving = false; }
    });
  }

  fmt(n: number | undefined): string {
    if (n == null) return '';
    return new Intl.NumberFormat('es-CO', { maximumFractionDigits: 2 }).format(n);
  }
}
