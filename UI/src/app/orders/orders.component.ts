import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { Order } from '../models/models';
import { ApiService } from '../services/api.service';
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
import { PopupComponent } from '../popup/popup.component';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { StoresOrderComponent } from '../stores-order/stores-order.component';
import { CommonModule } from '@angular/common';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
import { CustomerProductCatalogService } from '../services/customerProductCatalogDialog.service';
import { OrderDetailComponent } from './order-detail/order-detail.component';
interface Customers {
  erpCustomerID: string;
  name: string;
  id: any;
}

@Component({
  selector: 'orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.scss'],
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
export class OrdersComponent implements OnInit {
  mydate = environment.date;
  selectedCustomerId: any;
  customer: string = 'EMPTY';
  listOfOrders: Order[] = [];
  listOfOrderFiles: Order[] = [];
  ordersToDisplay: Order[] = [];
  OrderForm: FormGroup;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  listOfStoresOrder: Order[] = [];
  statusOptions = ['Select Status', 'ACKNOWLEDGED', 'ASNGEN', 'ASNMARK', 'COMPLETE', 'FINISHED', 'INVEDI', 'INVOICED', 'NEW', 'PROCESSED', 'SYNCERROR', 'SYNCED', 'SPLITED', 'CANCELLED', 'SHIPPED', 'ERROR', 'ASNERROR','INPROGRESS'];
  isAdminUser: boolean = false;
  isCompany: string | undefined = '';
  customerOptions: Customers[] | undefined;
  erpCustomerID: any = 'EMPTY';
  id: any = null;
  CustomerName: any = '';
  showDataColumn: boolean = true;
  name: string = '';


  columns: string[] = [
    'id',
    'Status',
    'CustomerName',
    'OrderNumber',
    'ERPSoNum',
    'OrderDate',
    'CreatedDate',
    'File',
    'SendOrderStatus',
    'GenerateASN',
    'MarkOrderforASN',
    'CreateInvoice',
    'Create810',
    'SyncOrder',
    'GetStoresOrder',
    'ModifyOrder',
    'OrderStatusError',
    'ERPCustomerID'
    // 'ProcessShipment'
  ];

  columnsToRemove: string[] = [
    'SendOrderStatus',
    'GenerateASN',
    'MarkOrderforASN',
    'CreateInvoice',
    'Create810',
    'SyncOrder',
    'GetStoresOrder'
  ];

  statusToRemove: string[] =
    [
       'ASNGEN', 'ASNMARK', 'COMPLETE', 'FINISHED', 'INVEDI', 'PROCESSED', 'SYNCERROR', 'SPLITED'
    ];

  constructor(private ERPApi: CustomerProductCatalogService, private translate: TranslateService, private api: ApiService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, public languageService: LanguageService) {
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.OrderForm = this.fb.group({
      orderId: fb.control(''),
      fromDate: new FormControl(formatDate(sevenDaysAgo, "yyyy-MM-dd", "en")),
      toDate: new FormControl(formatDate(today, "yyyy-MM-dd", "en")),
      orderNo: fb.control(''),
      status: fb.control(''),
      soNo: fb.control(''),
      customerName: fb.control('')
    });
  }

  ngOnInit(): void {
    this.getOrders();
    this.isCompany = this.api.getTokenUserInfo()?.company.toLocaleLowerCase();
    this.isAdminUser = ["ADMIN", "WRITER"].includes(this.api.getTokenUserInfo()?.userType || '');
    // if (!this.isAdminUser) {
    //   const editIndex = this.columns.indexOf('ProcessShipment');
    //   if (editIndex !== -1) {
    //     this.columns.splice(editIndex, 1);
    //   }
    // }

    if (this.isCompany?.toLocaleLowerCase() == 'esyncmate') {
      this.columns = this.columns.filter(column => !this.columnsToRemove.includes(column));
    }

    if (this.isCompany?.toLocaleLowerCase() == 'esyncmate') {
      this.statusOptions = this.statusOptions.filter(column => !this.statusToRemove.includes(column));
    }

    if (this.isCompany?.toLocaleLowerCase() == 'repaintstudios') {
      this.columns = this.columns.filter(column => !this.columnsToRemove.includes(column));
    }

    if (this.isCompany?.toLocaleLowerCase() == 'repaintstudios') {
      this.statusOptions = this.statusOptions.filter(column => !this.statusToRemove.includes(column));
    }
    this.getERPCustomer();

  }

  getStatusTooltip(status: string, customerName: string): any {
    switch (status) {
      case 'NEW':
        return { key: 'OrderStatusNew', params: { customerName: customerName.toUpperCase() } };
      case 'SHIPPED':
        return { key: 'OrderStatusShipped', params: { customerName: customerName.toUpperCase() } };
      case 'SYNCED':
        return { key: 'OrderStatusSynced' };
      case 'INVOICED':
        return { key: 'OrderStatusInvoiced', params: { customerName: customerName.toUpperCase() } };
      case 'CANCELLED':
        return { key: 'OrderStatusCancelled' };
      case 'ERROR':
        return { key: 'OrderStatusError' };
      case 'ASNERROR':
        return { key: 'OrderStatusASNERROR' };
      case 'DUPLICATE':
        return { key: 'OrderStatusASNERROR' };
      case 'ACKERROR':
        return { key: 'OrderStatusACKERROR' };
      case 'ACKNOWLEDGED':
        return { key: 'OrderStatusACKNOWLEDGED' };
      case 'INPROGRESS':
        return { key: 'OrderStatusINPROGRESS' };
      default:
        return '';
    }
  }

  getTooltipWithTranslation(element: any): string {
    const tooltipData = this.getStatusTooltip(element.status.toUpperCase(), element.customerName);
    return this.translate.instant(tooltipData.key, tooltipData.params);
  }

  getStatusClass(status: string): string {
    if (status.toUpperCase() === 'NEW') {
      return 'new-status';
    } else if (status.toUpperCase() === 'SYNCERROR') {
      return 'syncerror-status';
    } else if (status.toUpperCase() === 'SYNCED') {
      return 'sysced-status';
    } else if (status.toUpperCase() === 'PROCESSED') {
      return 'processed-status';
    } else if (status.toUpperCase() === 'ACKNOWLEDGED') {
      return 'acknowledged-status';
    } else if (status.toUpperCase() === 'ASNGEN') {
      return 'asngen-status';
    } else if (status.toUpperCase() === 'ASNMARK') {
      return 'asnmark-status';
    } else if (status.toUpperCase() === 'INVOICED') {
      return 'invoiced-status';
    } else if (status.toUpperCase() === 'COMPLETE') {
      return 'complete-status';
    } else if (status.toUpperCase() === 'FINISHED') {
      return 'finished-status';
    } else if (status.toUpperCase() === 'SPLITED') {
      return 'splited-status';
    } else if (status.toUpperCase() === 'INVEDI') {
      return 'invedi-status';
    } else if (status.toUpperCase() === 'CANCELLED') {
      return 'splited-status';
    } else if (status.toUpperCase() === 'SHIPPED') {
      return 'finished-status';
    } else if (status.toUpperCase() === 'ERROR') {
      return 'syncerror-status';
    } else if (status.toUpperCase() === 'DUPLICATE') {
      return 'sysced-status';
    } else if (status.toUpperCase() === 'ACKERROR') {
      return 'syncerror-status';
    } else if (status.toUpperCase() === 'INPROGRESS') {
      return 'sysced-status';
    } else if (status.toUpperCase() === 'ACKERROR') {
      return 'syncerror-status';
    } else {
      return '';
    }
  }

  ReProccess(element: any, ASNError: number) {
    let orderNo = element.id;
    let customerName = element.erpCustomerID
    let status = element.status
    let customerOrderNumber = element.orderNumber
    let isASNError = ASNError;
    this.showSpinner = true;

    this.api.ReProccess(orderNo, customerName, status, customerOrderNumber, isASNError).subscribe({
      next: (res: any) => {
        const { code, message } = res;

        if (code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: message, duration: 5000, position: 'topRight' });
          this.getOrders();
        } else if (code === 400) {
          this.toast.error({ detail: "ERROR", summary: message, duration: 5000, position: 'topRight' });
        } else {
          this.toast.info({ detail: "INFO", summary: message, duration: 5000, position: 'topRight' });
        }

        this.showSpinner = false;
      },
      error: (err: any) => {
        const msg = err?.error?.message || err.message || 'Unexpected error';
        this.toast.error({ detail: "ERROR", summary: msg, duration: 5000, position: 'topRight' });
        this.showSpinner = false;
      }
    });
  }

  getERPCustomer() {
    this.ERPApi.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customerOptions = res.customers;
      },
    });
  }

  editOrder(data: any) {
    const dialogRef = this.dialog.open(OrderDetailComponent, {
      // width: 'auto',
      disableClose: true,
      data: {
        orderData: data
      }
    });

    // dialogRef.afterClosed()
    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getOrders();
      }
    });
  }

  processOrderForShipment(element: any) {
    let orderNumber = element.orderNumber;
    this.showSpinner = true;

    this.api.processForShipment(orderNumber).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        if (this.code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          element.status = "SHIPPED";
          //this.getOrders();
          this.showSpinner = false;
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        }

        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  syncOrder(element: any) {
    let orderId = element.id;
    this.showSpinner = true;

    this.api.syncOrder(orderId).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        if (this.code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.getOrders();
          this.showSpinner = false;
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        }

        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  //process856
  generateASN(element: any) {
    let orderId = element.id;
    this.showSpinner = true;

    this.api.generateASN(orderId).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        if (this.code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.getOrders();
          this.showSpinner = false;
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        }

        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  generate855EDI(element: any) {
    let orderId = element.id;
    this.showSpinner = true;

    this.api.generate855EDI(orderId).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        if (this.code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.getOrders();
          this.showSpinner = false;
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;
        }

        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  markForASN(element: any) {
    let orderId = element.id;
    this.showSpinner = true;

    this.api.markForASN(orderId).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        if (this.code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.getOrders();
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

  createInvoice(element: any) {
    let orderId = element.id;
    this.showSpinner = true;

    this.api.createInvoice(orderId).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        if (this.code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.getOrders();
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

  process810(element: any) {
    let orderId = element.id;
    this.showSpinner = true;

    this.api.process810(orderId).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        if (this.code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.getOrders();
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

  getOrderFiles(element: any) {
    let orderId = element.id;
    this.showSpinner = false;

    this.api.getOrderFiles(orderId).subscribe({
      next: (res: any) => {
        this.listOfOrderFiles = res.files;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfOrderFiles.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noOrderDataMsg'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;

          return;
        }

        const dialogRef = this.dialog.open(PopupComponent, {
          width: '70%',
          disableClose: true,
          data: {
            listOfOrderFiles: this.listOfOrderFiles,
            orderNumber: element.orderNumber
          }
        });

        dialogRef.afterClosed().subscribe(result => {
          console.log('The dialog was closed');
        });

        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  getStoresOrder(element: any) {
    let orderId = element.id;
    this.showSpinner = false;

    this.api.getStoresOrder(orderId).subscribe({
      next: (res: any) => {
        this.listOfStoresOrder = res.orderStores;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfStoresOrder == null || this.listOfStoresOrder.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noOrderDataMsg'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;

          return;
        }

        const dialogRef = this.dialog.open(StoresOrderComponent, {
          width: '50%',
          disableClose: true,
          data: this.listOfStoresOrder,
        });

        dialogRef.afterClosed().subscribe(result => {
          console.log('The dialog was closed');
        });

        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.ordersToDisplay = this.listOfOrders.slice(startIndex, startIndex + event.pageSize);
  }

  getOrders() {
    let orderId = (this.OrderForm.get('orderId') as FormControl).value;
    let fromDate = (this.OrderForm.get('fromDate') as FormControl).value;
    let toDate = (this.OrderForm.get('toDate') as FormControl).value;
    let orderNo = (this.OrderForm.get('orderNo') as FormControl).value;
    let soNo = (this.OrderForm.get('soNo') as FormControl).value;
    let status = (this.OrderForm.get('status') as FormControl).value;
    let customerName = (this.OrderForm.get('customerName') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';
    this.showSpinnerforSearch = true;

    if (((customerName == '' || customerName == null || customerName == undefined) && (orderId == '' || orderId == null || orderId == 0) && (fromDate == '' || fromDate == null) && (toDate == '' || toDate == undefined) && (orderNo == '' || orderNo == 'EMPTY') && (status == '' || status == 'Select Status') && (soNo == '' || soNo == 'EMPTY' || soNo == null || soNo == undefined))) {
      this.toast.info({ detail: "orderId", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
    }

    if (orderId == '') {
      orderId = 0;
    }

    if (orderNo == '') {
      orderNo = 'EMPTY'
    }

    if (soNo == '') {
      soNo = 'EMPTY'
    }

    if (status == '' || status.toLocaleLowerCase() == 'select status') {
      status = 'EMPTY'
    }
    if (customerName == '' || customerName.toLocaleLowerCase() == 'select status') {
      customerName = 'EMPTY'
    }

    if (fromDate !== null) {
      stringFromDate = fromDate.toLocaleString();

      if (stringFromDate.length > 10) {
        stringFromDate = this.getFormattedDate(fromDate);
      }
    } else {
      stringFromDate = '1999-01-01';
    }

    if (toDate !== null) {
      stringToDate = toDate.toLocaleString();

      if (stringToDate.length > 10) {
        stringToDate = this.getFormattedDate(toDate);
      }
    } else {
      stringToDate = '1999-01-01';
    }
    this.api.getOrders(orderId, stringFromDate, stringToDate, orderNo, status, soNo, customerName).subscribe({
      next: (res: any) => {
        this.listOfOrders = res.ordersData;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfOrders == null || this.listOfOrders.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.ordersToDisplay = [];

          return;
        }

        this.ordersToDisplay = this.listOfOrders.slice(0, 10);

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
