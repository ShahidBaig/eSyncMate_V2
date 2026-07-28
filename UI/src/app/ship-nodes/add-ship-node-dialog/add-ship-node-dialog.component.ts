import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { TranslateModule } from '@ngx-translate/core';
import { NgToastService } from 'ng-angular-popup';

import { LanguageService } from '../../services/language.service';
import { ShipNodesService } from '../../services/ship-nodes.service';

@Component({
  selector: 'add-ship-node-dialog',
  templateUrl: './add-ship-node-dialog.component.html',
  styleUrls: ['./add-ship-node-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatSelectModule, MatAutocompleteModule, TranslateModule
  ],
})
export class AddShipNodeDialogComponent implements OnInit {
  newShipNodeForm: FormGroup;
  warehouses: string[] = [];
  filteredWarehouses: string[] = [];
  isSaving = false;

  constructor(
    public dialogRef: MatDialogRef<AddShipNodeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { customers: any[] },
    private fb: FormBuilder,
    private api: ShipNodesService,
    private toast: NgToastService,
    public languageService: LanguageService
  ) {
    this.newShipNodeForm = this.fb.group({
      customerID: ['', Validators.required],
      whsid: ['', Validators.required],
      shipNode: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    // Existing warehouse codes are offered as suggestions; a new one can still be typed
    this.api.getWarehouses('').subscribe({
      next: (res: any) => {
        this.warehouses = res.warehouses || [];
        this.filteredWarehouses = this.warehouses;
      }
    });
  }

  filterWarehouses(value: string): void {
    const search = (value || '').toLowerCase();
    this.filteredWarehouses = this.warehouses.filter(w => w.toLowerCase().includes(search));
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (!this.newShipNodeForm.valid || this.isSaving) return;

    this.isSaving = true;

    // Source (Walmart vs Target) + apiName are derived on the server from the customer.
    const model = {
      customerID: this.newShipNodeForm.get('customerID')?.value,
      whsid: this.newShipNodeForm.get('whsid')?.value,
      shipNode: this.newShipNodeForm.get('shipNode')?.value
    };

    this.api.saveShipNode(model).subscribe({
      next: (res: any) => {
        this.isSaving = false;

        if (res.code === 400 || res.code === 500) {
          this.toast.error({ detail: 'ERROR', summary: res.message, duration: 5000, position: 'topRight' });
          return;
        }

        // 401 = the customer/warehouse/ship node combination already exists
        if (res.code === 401) {
          this.toast.warning({ detail: 'WARNING', summary: res.description, duration: 6000, position: 'topRight' });
          return;
        }

        this.toast.success({ detail: 'SUCCESS', summary: res.description, duration: 5000, position: 'topRight' });
        this.dialogRef.close('saved');
      },
      error: (err: any) => {
        this.isSaving = false;
        this.toast.error({ detail: 'ERROR', summary: err.message, duration: 5000, position: 'topRight' });
      }
    });
  }
}
