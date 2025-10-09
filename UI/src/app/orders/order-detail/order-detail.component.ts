import { Component, inject, Inject, OnInit } from '@angular/core';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule, FormsModule, FormArray } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { LanguageService } from '../../services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AsyncPipe } from '@angular/common';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatDividerModule } from '@angular/material/divider';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatCardModule } from '@angular/material/card';
import { MatTableDataSource } from '@angular/material/table';
import { ApiService } from 'src/app/services/api.service';

export interface DetailItem {
  lineNo?: number;
  unitPrice: number;
  lineQty: number;
  asnQty: number;
  cancelQty: number,
  status: string,
  totalAmount: number,
  statusDescription: string,
  itemID: string
}

@Component({
  selector: 'order-detail',
  templateUrl: './order-detail.component.html',
  styleUrls: ['./order-detail.component.scss'],
  standalone:true,
  providers: [DatePipe],
  imports: [
    MatButtonToggleModule,
    MatTableModule,
    DatePipe,
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
    MatTabsModule,
    MatCheckboxModule,
    MatIconModule,
    MatChipsModule,
    TranslateModule,
    MatAutocompleteModule,
    AsyncPipe,
    MatDividerModule,
    MatGridListModule,
    MatCardModule,
  ],
})
export class OrderDetailComponent {
  statusOptions = ['INVOICED', 'NEW', 'SYNCED', 'CANCELLED', 'SHIPPED', 'ERROR', 'ASNERROR'];
  orderDetailForm: FormGroup;
  displayedColumns: string[] = ['LineNo','ItemID','OrderQty','AsnQty','CancelQty','UnitPrice','TotalAmount','Status'];
  dataSource = new MatTableDataSource<DetailItem>();
  orderData: any = [];
  public inView: boolean = false;
    
  constructor(
    public dialogRef: MatDialogRef<OrderDetailComponent>,
    private formBuilder: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public data: any = [],
    private orderService: ApiService,
    private toast: NgToastService,
    private datePipe: DatePipe,
    public languageService: LanguageService,
    private translate: TranslateService
  ) {
    this.orderDetailForm = this.formBuilder.group({
      orderDate: [''],
      orderNumber: [''],
      externalId: [''],
      customerID: [''],
      shipToAddress1: [''],
      shipToAddress2: [''],
      shipToCity: [''],
      shipToState: [''],
      shipToZip: [''],
      shipToCountry: [''],
      shipToEmail: [''],
      shipToPhone: [''],
      status: [''],
      shipToName:['']
    });
  }

  ngOnInit() {
    this.initializeForm();
  }

  initializeForm() {

    this.orderData = this.data.orderData;
    this.inView = true;
    this.orderDetailForm.get('status')?.disable();
    this.getOrderDetail();

    this.orderDetailForm = this.formBuilder.group({
      id: this.orderData.id,
      orderDate: this.datePipe.transform(this.orderData.orderDate, 'MMM-dd-yyyy'),
      orderNumber: this.orderData.orderNumber,
      externalId: this.orderData.externalId,
      customerID: this.orderData.erpCustomerID,
      shipToAddress1: this.orderData.shipToAddress1,
      shipToAddress2: this.orderData.shipToAddress2,
      shipToCity: this.orderData.shipToCity,
      shipToState: this.orderData.shipToState,
      shipToZip: this.orderData.shipToZip,
      shipToCountry: this.orderData.shipToCountry,
      shipToEmail: this.orderData.shipToEmail,
      shipToPhone: this.orderData.shipToPhone,
      status: this.orderData.status,
      shipToName: this.orderData.shipToName
    });
  }

  getOrderDetail() {
    this.orderService.getSalesOrderDetail(this.orderData.id).subscribe({
      next: (res: any) => {
        const processedData = res.detailData.map((item: DetailItem) => {
        const statusDescriptions: { [key: string]: string } = {
            ASNRVD: 'In Ship',
            ASNSNT: 'Shipped',
            CANRVD: 'In Cancel',
            CANSNT: 'Cancelled',
          };

          const statusDescription = statusDescriptions[item.status] || 'Unknown Status';

          return {
            ...item,
            totalAmount: item.lineQty * item.unitPrice,
            statusDescription,
          };
        }).sort((a: DetailItem, b: DetailItem) => (a.lineNo || 0) - (b.lineNo || 0));

        this.dataSource.data = processedData;
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (!this.orderDetailForm.get('shipToAddress1')?.value || !this.orderDetailForm.get('shipToCity')?.value || !this.orderDetailForm.get('shipToState')?.value || !this.orderDetailForm.get('shipToZip')?.value || !this.orderDetailForm.get('shipToCountry')?.value) {
      this.toast.warning({detail: "WARNING",summary: "Address Info can't be empty except Address Line 2.",duration: 5000, position: 'topRight'});
      return;
    }

    const orderModel = {
      Id: this.orderData.id,
      orderDate: this.orderDetailForm.get('orderDate')?.value
        ? this.datePipe.transform(this.orderDetailForm.get('orderDate')?.value, 'yyyy-MM-ddTHH:mm:ss')
        : '1900-01-01',
      customerPO: this.orderDetailForm.get('orderNumber')?.value,
      customerID: this.orderDetailForm.get('customerID')?.value,
      status: this.orderDetailForm.get('status')?.value,
      shipToAddress1: this.orderDetailForm.get('shipToAddress1')?.value,
      shipToAddress2: this.orderDetailForm.get('shipToAddress2')?.value,
      shipToCity: this.orderDetailForm.get('shipToCity')?.value,
      shipToState: this.orderDetailForm.get('shipToState')?.value,
      shipToZip: this.orderDetailForm.get('shipToZip')?.value,
      shipToCountry: this.orderDetailForm.get('shipToCountry')?.value,
      shipToName: this.orderDetailForm.get('shipToName')?.value,
    };

    this.orderService.updateSalesOrder(orderModel).subscribe({
      next: (res: { code: number; description: any; message: any; }) => {
        if (res.code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: res.message, duration: 5000, position: 'topRight' });
          this.dialogRef.close('updated');
        } else {
          this.toast.error({ detail: "ERROR", summary: res.message || 'An error occurred', duration: 5000, position: 'topRight' });
        }
      },
      error: (err: { message: any; }) => {
        this.toast.error({ detail: "ERROR", summary: err.message || 'An error occurred', duration: 5000, position: 'topRight' });
      }
    });
  }

  getStatusClass(status: string): string {
    if (status.toUpperCase() === 'ASNRVD') {
      return 'in-ship-status';
    } else if (status.toLocaleUpperCase() === 'ASNSNT') {
      return 'shipped-status';
    } else if (status.toLocaleUpperCase() === 'CANRVD') {
      return 'in-cancel-status';
    } else if (status.toLocaleUpperCase() === 'CANSNT') {
      return 'cancelled-status';
    } else {
      return '';
    }
  }

  getStatusTooltip(status: string): any {
    switch (status) {
      case 'ASNRVD':
        return { key: 'ASN received from ERP' };
      case 'ASNSNT':
        return { key: 'ASN sent to Customer Portal'};
      case 'CANRVD':
        return { key: 'Cancellation received from ERP' };
      case 'CANSNT':
        return { key: 'Cancellation sent to Customer Portal'};
      default:
        return '';
    }
  }

  getTooltipWithTranslation(status: any): string {
    const tooltipData = this.getStatusTooltip(status.toLocaleUpperCase());
    return this.translate.instant(tooltipData.key, tooltipData.params);
  }

  editDocument(): void {
    this.inView = false;
    this.orderDetailForm.get('status')?.enable();
  }

  isReadonly(): boolean {
    let val: boolean;

    if (this.inView)
      val = true;
    else if (this.orderDetailForm.get('status')?.value !== 'NEW' && this.orderDetailForm.get('status')?.value !== 'ERROR')
      val = true;
    else
      val = false;

    return val;
  }

}
