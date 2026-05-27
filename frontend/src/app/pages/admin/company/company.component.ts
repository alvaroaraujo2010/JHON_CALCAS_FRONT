import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { Company } from '../../../core/models';
import { ModuleHeaderComponent } from '../../../shared/module-header/module-header.component';

@Component({
  selector: 'app-company',
  standalone: true,
  imports: [ReactiveFormsModule, ModuleHeaderComponent],
  templateUrl: './company.component.html'
})
export class CompanyComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  form = this.fb.group({
    businessName: ['', Validators.required],
    tagline: [''],
    description: [''],
    address: [''],
    phone: [''],
    email: [''],
    website: [''],
    taxId: [''],
    currency: ['COP']
  });

  ngOnInit() {
    this.api.get<Company>('company').subscribe(c => this.form.patchValue(c));
  }

  save() {
    if (this.form.invalid) return;
    this.api
      .run(this.api.put('company', this.form.getRawValue()), {
        success: 'Datos de empresa actualizados correctamente',
        error: 'No se pudo guardar los datos de la empresa'
      })
      .subscribe();
  }
}
