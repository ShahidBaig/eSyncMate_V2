import { Component, Inject } from '@angular/core';
import { MatDialogRef, MatDialog, MAT_DIALOG_DATA } from '@angular/material/dialog';
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
import { MatSelectChange, MatSelectModule } from '@angular/material/select';
import { ApiService } from '../services/api.service';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
@Component({
  selector: 'update-status-modal',
  templateUrl: './update-status-modal.component.html',
  styleUrls: ['./update-status-modal.component.scss'],
  standalone: true,
  providers: [DatePipe],
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
export class UpdateStatusModalComponent {
  statusOptions = ['ACKNOWLEDGED', 'ASNGEN', 'ASNMARK', 'COMPLETE', 'FINISHED', 'INVEDI', 'INVOICED', 'NEW', 'PROCESSED', 'SYNCERROR', 'SYNCED', 'SPLITED', 'CANCELLED', 'SHIPPED','ERROR' ,'ASNERROR'];
  isCompany: string | undefined = '';
  updateOrderStatusForm: FormGroup;
  orderId: string;
  isCompletionDateVisible: boolean = false;
  statusToRemove: string[] =
    [
      'ACKNOWLEDGED', 'ASNGEN', 'ASNMARK', 'COMPLETE', 'FINISHED', 'INVEDI', 'PROCESSED', 'SYNCERROR', 'SPLITED'
    ];
  constructor(public dialogRef: MatDialogRef<UpdateStatusModalComponent>, private api: ApiService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, public languageService: LanguageService, @Inject(MAT_DIALOG_DATA) public data: any ,private datePipe: DatePipe,) {
      this.orderId = data.element.id;
      this.updateOrderStatusForm = this.fb.group({
        status: [''],
        completionDate: [''],
      });
    }

  onSave(): void {
    const status = this.updateOrderStatusForm.get('status')?.value;

    if (!this.data.isCLT) {
      this.api.updateStatus(status, this.orderId ).subscribe({
        next: (res) => {
          this.toast.success({ detail: "SUCCESS", summary: "Order status updated successfully.", duration: 5000, position: 'topRight' });
          this.data.element.status = status;
          this.dialogRef.close('saved');

        },
        error: (err) => {
          this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        }
      });
    } else
    {
      let completionDate = this.updateOrderStatusForm.get('completionDate')?.value ? this.datePipe.transform(this.updateOrderStatusForm.get('completionDate')?.value, 'yyyy-MM-dd') : '1900-01-01'

      //if (status == "COMPLETE")
      //{
      //  let currentDate = this.datePipe.transform(new Date(), 'yyyy-MM-dd');

      //  if (completionDate && currentDate && completionDate < currentDate) {
      //    this.toast.error({ detail: "ERROR", summary: "Completion Date must be equal or greater to current date.", duration: 5000, position: 'topRight' });
      //    this.data.element.status = status;
      //    return;
      //  } 
      //}
      

      this.api.updateCLTStatus(status, this.orderId, completionDate).subscribe({
        next: (res) => {
          this.toast.success({ detail: "SUCCESS", summary: "Shipment status updated successfully.", duration: 5000, position: 'topRight' });
          this.data.element.status = status;

          if (completionDate != '1900-01-01') {
            this.data.element.completionDate = completionDate;
          }
          
          this.dialogRef.close('saved');

        },
        error: (err) => {
          this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        }
      });
    }


    
    }


  onCancel(): void {
    this.dialogRef.close();
  }


  ngOnInit(): void {
    this.isCompany = this.api.getTokenUserInfo()?.company.toLocaleLowerCase();

    if (!this.data.isCLT)
    {
      if (this.isCompany == 'esyncmate') {
        this.statusOptions = this.statusOptions.filter(column => !this.statusToRemove.includes(column));
      }
    }
    else {
      this.statusOptions = ['ACK', 'COMPLETE']
    }
  }

  onStatusChange(event: MatSelectChange) {
    let status = event.value;

    if (status == "COMPLETE") {
      this.isCompletionDateVisible = true;
    } else {
      this.isCompletionDateVisible = false;
    }

  }
}
