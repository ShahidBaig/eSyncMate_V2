import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { TranslateModule } from '@ngx-translate/core';
import { NgToastService } from 'ng-angular-popup';

import { LanguageService } from '../../services/language.service';
import { ShipCodesService } from '../../services/ship-codes.service';

@Component({
  selector: 'edit-ship-code-dialog',
  templateUrl: './edit-ship-code-dialog.component.html',
  styleUrls: ['../add-ship-code-dialog/add-ship-code-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatSelectModule, MatCheckboxModule, TranslateModule
  ],
})
export class EditShipCodeDialogComponent {
  form: FormGroup;
  isSaving = false;

  constructor(
    public dialogRef: MatDialogRef<EditShipCodeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { customers: any[]; shipCode: any },
    private fb: FormBuilder,
    private api: ShipCodesService,
    private toast: NgToastService,
    public languageService: LanguageService
  ) {
    this.form = this.fb.group({
      id: [this.data.shipCode.id, Validators.required],
      customerID: [this.data.shipCode.customerID, Validators.required],
      isDefault: [this.data.shipCode.isDefault],
      sourceMethod: [this.data.shipCode.sourceMethod, Validators.required],
      matchType: [this.data.shipCode.matchType || 'EXACT', Validators.required],
      levelOfService: [this.data.shipCode.levelOfService],
      shippingMethod: [this.data.shipCode.shippingMethod],
      priority: [this.data.shipCode.priority || 1]
    });

    // A default row is the customer's fallback — Source Method must stay empty.
    this.form.get('isDefault')!.valueChanges.subscribe((isDef: boolean) => {
      const src = this.form.get('sourceMethod')!;
      if (isDef) { src.reset(''); src.disable(); }
      else { src.enable(); }
    });

    if (this.data.shipCode.isDefault) {
      this.form.get('sourceMethod')!.disable();
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onUpdate(): void {
    if (!this.form.valid || this.isSaving) return;

    this.isSaving = true;

    const model = {
      id: this.form.get('id')?.value,
      customerID: this.form.get('customerID')?.value,
      isDefault: this.form.get('isDefault')?.value,
      sourceMethod: this.form.get('sourceMethod')?.value,
      matchType: this.form.get('matchType')?.value,
      levelOfService: this.form.get('levelOfService')?.value,
      shippingMethod: this.form.get('shippingMethod')?.value,
      priority: this.form.get('priority')?.value || 1,
      isActive: true
    };

    this.api.updateShipCode(model).subscribe({
      next: (res: any) => {
        this.isSaving = false;

        if (res.code === 400 || res.code === 500) {
          this.toast.error({ detail: 'ERROR', summary: res.message, duration: 5000, position: 'topRight' });
          return;
        }

        if (res.code === 401) {
          this.toast.warning({ detail: 'WARNING', summary: res.description, duration: 6000, position: 'topRight' });
          return;
        }

        this.toast.success({ detail: 'SUCCESS', summary: res.description, duration: 5000, position: 'topRight' });
        this.dialogRef.close('updated');
      },
      error: (err: any) => {
        this.isSaving = false;
        this.toast.error({ detail: 'ERROR', summary: err.message, duration: 5000, position: 'topRight' });
      }
    });
  }
}
