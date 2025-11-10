import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { PurchaseOrdersTracking } from '../models/models';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
import { PurchaseOrdersTrackingService } from '../services/purchaseOrdersTracking.service';

@Component({
  selector: 'purchase-orders-tracking',
  templateUrl: './purchase-orders-tracking.component.html',
  styleUrls: ['./purchase-orders-tracking.component.scss'],
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
    MatPaginatorModule,
    TranslateModule
  ],
})
export class PurchaseOrdersTrackingComponent {
  mydate = environment.date;

  listOfPurchaseOrdersTracking: PurchaseOrdersTracking[] = [];
  inventoryToDisplay: PurchaseOrdersTracking[] = [];
  PurchaseOrdersTracking: FormGroup;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;

  columns: string[] = [
    'ID',
    'PurchaseOrderNo',
    'PoNumber',
    'OrderDate',
    'SKU',
    'OrderQty',
    'ReceivedDate',
    'ReceivedQty',
    'BackOrderQty'
  ];

  constructor(private translate: TranslateService, private api: PurchaseOrdersTrackingService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, public languageService: LanguageService) {
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.PurchaseOrdersTracking = this.fb.group({
      ID: fb.control(''),
      purchaseOrderNo: fb.control(''),
      poNumber: fb.control(''),
      orderDate: new FormControl(formatDate(sevenDaysAgo, "yyyy-MM-dd", "en")),
      sku: fb.control(''),
      OrderQty: fb.control(''),
      ReceivedDate: new FormControl(formatDate(sevenDaysAgo, "yyyy-MM-dd", "en")),
      ReceivedQty: fb.control(''),
      BackOrderQty: fb.control(''),
    });
  }

  ngOnInit(): void {
    this.getPurchaseOrdersTracking();
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.inventoryToDisplay = this.listOfPurchaseOrdersTracking.slice(startIndex, startIndex + event.pageSize);
  }

  getPurchaseOrdersTracking() {
    let purchaseOrderNo = (this.PurchaseOrdersTracking.get('purchaseOrderNo') as FormControl).value;
    let orderDate = (this.PurchaseOrdersTracking.get('orderDate') as FormControl).value;
    let sku = (this.PurchaseOrdersTracking.get('sku') as FormControl).value;
    let poNumber = (this.PurchaseOrdersTracking.get('poNumber') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';
    this.showSpinnerforSearch = true;

    if (((orderDate == '' || orderDate == null) && (purchaseOrderNo == '' || purchaseOrderNo == 'EMPTY') && (sku == '' || sku == 'Select') && (poNumber == '' || poNumber.toUpperCase().includes('SELECT')))) {
      this.toast.info({ detail: "inventory", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
    }

    if (purchaseOrderNo == '') {
      purchaseOrderNo = 0
    }

    if (sku == '') {
      sku = 'EMPTY'
    }

    if (poNumber == '') {
      poNumber = 'EMPTY'
    }

    if (orderDate !== null) {
      stringFromDate = orderDate.toLocaleString();

      if (stringFromDate.length > 10) {
        stringFromDate = this.getFormattedDate(orderDate);
      }
    } else {
      stringFromDate = '1999-01-01';
    }

    this.api.getPurchaseOrdersTracking(purchaseOrderNo, stringFromDate, sku, poNumber).subscribe({
      next: (res: any) => {
        this.listOfPurchaseOrdersTracking = res.purchaseOrdersTracking;
        this.msg = res.message;
        this.code = res.code;


        if (this.listOfPurchaseOrdersTracking == null || this.listOfPurchaseOrdersTracking.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.inventoryToDisplay = [];

          return;
        }

        this.inventoryToDisplay = this.listOfPurchaseOrdersTracking.slice(0, 10);

        if (this.code === 200) {
          //this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.showSpinnerforSearch = false;
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
        }

        this.showSpinnerforSearch = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinnerforSearch = false;
      },
    });
  }
}
