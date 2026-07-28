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
  selector: 'edit-ship-node-dialog',
  templateUrl: './edit-ship-node-dialog.component.html',
  styleUrls: ['../add-ship-node-dialog/add-ship-node-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatSelectModule, MatAutocompleteModule, TranslateModule
  ],
})
export class EditShipNodeDialogComponent implements OnInit {
  updateShipNodeForm: FormGroup;
  warehouses: string[] = [];
  filteredWarehouses: string[] = [];
  isSaving = false;

  constructor(
    public dialogRef: MatDialogRef<EditShipNodeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { source: string; customers: any[]; shipNode: any },
    private fb: FormBuilder,
    private api: ShipNodesService,
    private toast: NgToastService,
    public languageService: LanguageService
  ) {
    this.updateShipNodeForm = this.fb.group({
      id: [this.data.shipNode.id, Validators.required],
      customerID: [this.data.shipNode.customerID, Validators.required],
      whsid: [this.data.shipNode.whsid, Validators.required],
      shipNode: [this.data.shipNode.shipNode, Validators.required],
      apiName: [this.data.shipNode.apiName || 'WalmartAPI']
    });
  }

  get isWalmart(): boolean {
    return this.data.source === 'WALMART';
  }

  ngOnInit(): void {
    this.api.getWarehouses(this.data.source).subscribe({
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

  onUpdate(): void {
    if (!this.updateShipNodeForm.valid || this.isSaving) return;

    this.isSaving = true;

    const model = {
      source: this.data.source,
      id: this.updateShipNodeForm.get('id')?.value,
      customerID: this.updateShipNodeForm.get('customerID')?.value,
      whsid: this.updateShipNodeForm.get('whsid')?.value,
      shipNode: this.updateShipNodeForm.get('shipNode')?.value,
      apiName: this.isWalmart ? this.updateShipNodeForm.get('apiName')?.value : ''
    };

    this.api.updateShipNode(model).subscribe({
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
        this.dialogRef.close('updated');
      },
      error: (err: any) => {
        this.isSaving = false;
        this.toast.error({ detail: 'ERROR', summary: err.message, duration: 5000, position: 'topRight' });
      }
    });
  }
}
