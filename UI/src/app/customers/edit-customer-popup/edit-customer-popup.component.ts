import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { CustomersService } from '../../services/customers.service';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'edit-customer-popup',
  templateUrl: './edit-customer-popup.component.html',
  styleUrls: ['./edit-customer-popup.component.scss'],
  standalone: true,
  imports: [
    MatButtonToggleModule,
    MatTableModule,
    DatePipe,
    MatCardModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    NgIf,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTooltipModule,
    MatIconModule,
    MatProgressSpinnerModule,
    CommonModule,
    MatSelectModule,
    FormsModule,
    TranslateModule
  ],
})
export class EditCustomerPopupComponent implements OnInit {
  updateCustomerForm: FormGroup | any;

  constructor(
    public dialogRef: MatDialogRef<EditCustomerPopupComponent>,
    private formBuilder: FormBuilder,
    private CustomersApi: CustomersService,
    private toast: NgToastService,
    public languageService: LanguageService,
    @Inject(MAT_DIALOG_DATA) public data: any 
  ) { }

  ngOnInit() {
    this.initializeForm();
  }

  initializeForm() {
    this.updateCustomerForm = this.formBuilder.group({
      id: [this.data.id, Validators.required],
      name: [this.data.name, Validators.required],
      erpCustomerId: [this.data.erpCustomerID, Validators.required],
      isaCustomerId: [this.data.isaCustomerID],
      isa810ReceiverId: [this.data.isA810ReceiverId],
      isa856ReceiverId: [this.data.isA856ReceiverId],
      marketplace: [this.data.marketplace]
    });
  }

  onCancel() {
    this.dialogRef.close();
  }

  updateCustomer(): void {
    const customerModel = {
      id: this.updateCustomerForm.get('id')?.value,
      name: this.updateCustomerForm.get('name')?.value,
      erpCustomerId: this.updateCustomerForm.get('erpCustomerId')?.value,
      isaCustomerId: this.updateCustomerForm.get('isaCustomerId')?.value,
      isa810ReceiverId: this.updateCustomerForm.get('isa810ReceiverId')?.value,
      isa856ReceiverId: this.updateCustomerForm.get('isa856ReceiverId')?.value,
      marketplace: this.updateCustomerForm.get('marketplace')?.value
    };

    if (this.updateCustomerForm.valid) {
      this.CustomersApi.updateCustomer(customerModel).subscribe({
        next: (res) => {
          if (res.code === 100) {
            this.toast.success({ detail: "SUCCESS", summary: res.description, duration: 5000, position: 'topRight' });
          } else if (res.code === 400) {
            this.toast.error({ detail: "ERROR", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          } else if (res.code === 401) {
            this.toast.warning({ detail: "WARNING", summary: res.description, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          } else {
            this.toast.info({ detail: "INFO", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          }

          this.dialogRef.close('updated');
        },
        error: (err) => {
          this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });

        }
      });
    }
  }
}
