import { Component, OnInit } from '@angular/core';
import { InvFeedFromNDC, Map, PurchaseOrder } from '../models/models';
import { ApiService } from '../services/api.service';
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
import { PopupComponent } from '../popup/popup.component';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { PurchaseOrderService } from '../services/purchaseOrder.service';
import { AddPurchaseOrderComponent } from './add-purchase-order/add-purchase-order.component';
import { EditPurchaseOrderComponent } from './edit-purchase-order/edit-purchase-order.component';
import { ViewPurchaseOrderComponent } from './view-purchase-order/view-purchase-order.component';



@Component({
  selector: 'purchase-order',
  templateUrl: './purchase-order.component.html',
  styleUrls: ['./purchase-order.component.scss'],
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
    MatPaginatorModule,
    TranslateModule
  ],
})
export class PurchaseOrderComponent implements OnInit {


  listOfPurchaseOrders: PurchaseOrder[] = [];
  mapsToDisplay: PurchaseOrder[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Purchase Order', 'Purchase Order No', 'Status', 'Purchase Order Date', 'ItemID'];
  selectedOption: string = 'Select Purchase Order';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  showDataColumn: boolean = true;
  isAdminUser: boolean = false;

  columns: string[] = [
    'id',
    'PONumber',
    'Status',
    'OrderDate',
    'SupplierID',
    'ExpectedDate',
    'ReferenceNo',
    'ShipServiceCode',
    'shipToAddress1',
    'shipToAddress2',
    'shipToCity',
    'shipToState',
    'shipToZip',
    'shipToCountry',
    'shipToEmail',
    'shipToPhone',
    'billToAddress1',
    'billToAddress2',
    'billToCity',
    'billToState',
    'billToZip',
    'billToCountry',
    'billToEmail',
    'billToPhone',
    'warehouseName',
    'warehouseID',
    'manufacturerName',
    'uom',
    'ndcItemID',
    'productName',
    'totalQty',
    'totalExtendedPrice',
    'primaryCategoryName',
    'secondaryCategoryName',
    'Edit',
    'View',
    'MarkOrderforRelease'
  ];

  constructor(private translate: TranslateService, private api: ApiService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, public languageService: LanguageService, private PurchaseOrderService: PurchaseOrderService,) {
    this.isAdminUser = ["ADMIN", "WRITER"].includes(this.api.getTokenUserInfo()?.userType || '');
  }

  ngOnInit(): void {
    if (this.selectedOption === 'Select Purchase Order') {
      this.getPurchaseOrder();
    }
  }

  markForRelease(element: any) {
    let orderId = element.id;
    this.showSpinner = true;

    this.PurchaseOrderService.markForRelease(orderId).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        if (this.code === 100) {
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.getPurchaseOrder();
          this.showSpinner = false;
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        }
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  openAddPurchaseOrderCompenent(): void {
    const dialogRef = this.dialog.open(AddPurchaseOrderComponent, {
      width: '1700px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.getPurchaseOrder();
      }
    });
  }

  openEditPurchaseOrderCompenent(orderData: any) {
    const dialogRef = this.dialog.open(EditPurchaseOrderComponent, {
      width: '1700px',
      data: orderData,
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getPurchaseOrder();
      }
    });
  }

  openViewPurchaseOrderCompenent(orderData: any) {
    const dialogRef = this.dialog.open(ViewPurchaseOrderComponent, {
      width: '1700px',
      data: orderData,
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getPurchaseOrder();
      }
    });
  }
  get label(): string {
    return this.selectedOption === 'Select Purchase Order' ? 'Select Purchase Order' : this.selectedOption;
  }

  onSelectionChange() {
    this.searchValue = '';
    this.startDate = '';
    this.endDate = '';
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.mapsToDisplay = this.listOfPurchaseOrders.slice(startIndex, startIndex + event.pageSize);
  }

  getPurchaseOrder() {
    this.showSpinnerforSearch = true;
    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Purchase Order') {
      this.searchValue = 'ALL';
    }

    if (this.selectedOption === 'Purchase Order Date' && this.startDate.toLocaleString().length > 10) {
      stringFromDate = this.getFormattedDate(this.startDate);
    }
    if (this.selectedOption === 'Purchase Order Date' && this.endDate.toLocaleString().length > 10) {
      stringToDate = this.getFormattedDate(this.endDate);
    }
    if (this.selectedOption === 'Purchase Order Date' && this.startDate.toLocaleString().length > 10 && this.endDate.toLocaleString().length > 10) {
      this.searchValue = stringFromDate + '/' + stringToDate;
    }

    if (this.selectedOption === 'ItemID' && !this.searchValue.trim()) {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('Please enter an Item ID'), duration: 5000, position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
    }

    this.PurchaseOrderService.getPurchaseOrder(this.selectedOption, this.searchValue).subscribe({
      next: (res: any) => {
        this.listOfPurchaseOrders = res?.purchaseOrders || [];
        this.msg = res?.message;
        this.code = res?.code;

        if (this.listOfPurchaseOrders.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, position: 'topRight' });
          this.mapsToDisplay = [];
          this.showSpinnerforSearch = false;
          return;
        }

        this.mapsToDisplay = this.listOfPurchaseOrders.slice(0, 10);

        if (this.code === 200) {
          this.showSpinnerforSearch = false;
        } else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, position: 'topRight' });
          this.showSpinnerforSearch = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, position: 'topRight' });
          this.showSpinnerforSearch = false;
        }
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.showSpinnerforSearch = false;
      }
    });
  }

  getStatusTooltip(status: string, batchID: string): any {
    switch (status) {
      case 'NEW':
        return { key: 'NEW' };
      case 'INPROGRESS':
        return { key: 'INPROGRESS' };
      case 'SYNCED':
        return { key: 'SYNCED' };
      case 'ACKNOWLEDGED':
        return { key: 'ACKNOWLEDGED' };
      case 'INVOICED':
        return { key: 'INVOICED' };
      case 'RECEIVED':
        return { key: 'RECEIVED' };
      case 'ERROR':
        return { key: 'Batch Error', params: { batchID: batchID.toUpperCase() } };
      default:
        return { key: '' };
    }
  }

  getTooltipWithTranslation(element: any): string {
    const tooltipData = this.getStatusTooltip(element.status.toUpperCase(), element.batchID);
    return this.translate.instant(tooltipData.key, tooltipData.params);
  }

  getStatusClass(status: string): string {
    if (status.toLocaleUpperCase() === 'NEW') {
      return 'new-status';
    } else if (status.toLocaleUpperCase() === 'INPROGRESS') {
      return 'inprogress-status';
    } else if (status.toLocaleUpperCase() === 'ERROR') {
      return 'syncerror-status';
    } else if (status.toLocaleUpperCase() === 'SYNCED') {
      return 'sysced-status';
    } else if (status.toLocaleUpperCase() === 'ACKNOWLEDGED') {
      return 'acknowledged-status';
    } else if (status.toLocaleUpperCase() === 'INVOICED') {
      return 'invoiced-status';
    } else if (status.toLocaleUpperCase() === 'RECEIVED') {
      return 'complete-status';
    } else {
      return '';
    }
  }
}
