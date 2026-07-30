import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { Order } from '../models/models';
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
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { StoresOrderComponent } from '../stores-order/stores-order.component';
import { ResubmitConfirmDialogComponent } from './resubmit-confirm-dialog/resubmit-confirm-dialog.component';
import { ReTransmitConfirmDialogComponent } from './retransmit-confirm-dialog/retransmit-confirm-dialog.component';
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
import { OrderHelpDialogComponent } from './order-help-dialog/order-help-dialog.component';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { AfterViewInit, inject } from '@angular/core';
import { MatSort, Sort, MatSortModule } from '@angular/material/sort';



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
    MatProgressBarModule,
    CommonModule,
    MatSelectModule,
    MatPaginatorModule,
    TranslateModule,
    FormsModule
  ],
})
export class OrdersComponent implements OnInit {
  isLoading: boolean = false;
  mydate = environment.date;
  selectedCustomerId: any;
  customer: string = 'EMPTY';
  listOfOrders: Order[] = [];
  listOfOrderFiles: Order[] = [];
  OrderForm: FormGroup;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  listOfStoresOrder: Order[] = [];
  statusOptions = ['Select Status', 'Acknowledged', 'Asn Gen', 'Asn Mark', 'Complete', 'Finished', 'Inv EDI', 'Invoiced', 'New', 'Processed', 'Sync Error', 'Synced', 'Splited', 'Cancelled', 'Shipped', 'Partially Shipped', 'Partially Cancelled', 'Error', 'Asn Error', 'In Progress'];

  // Map display names back to API values
  statusDisplayToValue: { [key: string]: string } = {
    'Acknowledged': 'ACKNOWLEDGED', 'Asn Gen': 'ASNGEN', 'Asn Mark': 'ASNMARK',
    'Complete': 'COMPLETE', 'Finished': 'FINISHED', 'Inv EDI': 'INVEDI',
    'Invoiced': 'INVOICED', 'New': 'NEW', 'Processed': 'PROCESSED',
    'Sync Error': 'SYNCERROR', 'Synced': 'SYNCED', 'Splited': 'SPLITED',
    'Cancelled': 'CANCELLED', 'Shipped': 'SHIPPED', 'Partially Shipped': 'Partially Shipped',
    'Partially Cancelled': 'Partially Cancelled', 'Error': 'ERROR', 'Asn Error': 'ASNERROR',
    'In Progress': 'INPROGRESS'
  };

  statusValueToDisplay: { [key: string]: string } = {
    'ACKNOWLEDGED': 'Acknowledged', 'ASNGEN': 'Asn Gen', 'ASNMARK': 'Asn Mark',
    'COMPLETE': 'Complete', 'FINISHED': 'Finished', 'INVEDI': 'Inv EDI',
    'INVOICED': 'Invoiced', 'NEW': 'New', 'PROCESSED': 'Processed',
    'SYNCERROR': 'Sync Error', 'SYNCED': 'Synced', 'SPLITED': 'Splited',
    'CANCELLED': 'Cancelled', 'SHIPPED': 'Shipped', 'Partially Shipped': 'Partially Shipped',
    'Partially Cancelled': 'Partially Cancelled', 'ERROR': 'Error', 'ASNERROR': 'Asn Error',
    'INPROGRESS': 'In Progress', 'DUPLICATE': 'Duplicate', 'ACKERROR': 'Ack Error'
  };
  isAdminUser: boolean = false;
  canAdd = false;
  canEdit = false;
  canDelete = false;
  canResubmit = false;
  canReTransmit = false;
  // Global feature toggles (live from ApplicationSettings) — turn the whole action on/off without redeploy
  showResubmitAction = false;
  showReTransmitAction = false;
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;
  isCompany: string | undefined = '';
  customerOptions: Customers[] | undefined;
  filteredCustomerOptions: Customers[] = [];
  customerSearchText = '';
  filteredStatusOptions: string[] = [];
  statusSearchText = '';
  erpCustomerID: any = 'EMPTY';
  id: any = null;
  CustomerName: any = '';
  showDataColumn: boolean = true;
  name: string = '';
  ordersToDisplay: Order[] = [];

  columns: string[] = [
    'id',
    'Status',
    'CustomerName',
    'OrderNumber',
    'OrderDate',
    'ERPSoNum',
    'ERPSyncDate',
    'CreatedDate',
    'Actions',
    'ERPCustomerID'
  ];

  statusToRemove: string[] =
    [
       'Asn Gen', 'Asn Mark', 'Complete', 'Finished', 'Inv EDI', 'Processed', 'Sync Error', 'Splited'
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
    this.getOrders(true);
    this.isCompany = this.api.getTokenUserInfo()?.company.toLocaleLowerCase();
    const isAdmin = ["ADMIN", "WRITER"].includes(this.api.getTokenUserInfo()?.userType || '');
    const permissions = this.api.getMenuPermissions('edi/all-orders');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
      // New action flags: use the assigned value when present; if missing (older cached
      // menus that predate these flags), fall back to admin so admins aren't locked out.
      this.canResubmit = permissions.canResubmit ?? isAdmin;
      this.canReTransmit = permissions.canReTransmit ?? isAdmin;
    } else {
      this.canAdd = isAdmin;
      this.canEdit = isAdmin;
      this.canDelete = isAdmin;
      this.canResubmit = isAdmin;
      this.canReTransmit = isAdmin;
      this.isAdminUser = isAdmin;
    }

    // Global feature toggles (live) — flag 0 hides the action button entirely, no redeploy
    this.api.getActionColumnVisibility().subscribe({
      next: (res: any) => {
        this.showResubmitAction = !!res?.showResubmit;
        this.showReTransmitAction = !!res?.showReTransmit;
      },
      error: () => { /* default hidden */ }
    });
    // if (!this.canEdit) {
    //   const editIndex = this.columns.indexOf('ProcessShipment');
    //   if (editIndex !== -1) {
    //     this.columns.splice(editIndex, 1);
    //   }
    // }

    if (this.isCompany?.toLocaleLowerCase() == 'esyncmate' || this.isCompany?.toLocaleLowerCase() == 'repaintstudios') {
      this.statusOptions = this.statusOptions.filter(column => !this.statusToRemove.includes(column));
    }
    this.getERPCustomer();
    this.filteredStatusOptions = this.getFilterableStatuses();
  }

  onPageChange(event: PageEvent) {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.getOrders();
  }

  getStatusTooltip(status: string, customerName: string): any {
    const s = (status || '').toUpperCase();
    const cn = (customerName || '').toUpperCase();
    switch (s) {
      case 'NEW':
        return { key: 'OrderStatusNew', params: { customerName: cn } };
      case 'SHIPPED':
        return { key: 'OrderStatusShipped', params: { customerName: cn } };
      case 'SYNCED':
        return { key: 'OrderStatusSynced' };
      case 'INVOICED':
        return { key: 'OrderStatusInvoiced', params: { customerName: cn } };
      case 'CANCELLED':
        return { key: 'OrderStatusCancelled' };
      case 'PARTIALLY SHIPPED':
        return { key: 'OrderStatusPartiallyShipped', params: {} };
      case 'PARTIALLY CANCELLED':
        return { key: 'OrderStatusPartiallyCancelled', params: {} };
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
    const displayStatus = element.displayStatus || element.status || '';
    const tooltipData = this.getStatusTooltip(displayStatus, element.customerName || '');
    const statusText = (tooltipData && tooltipData.key) ? this.translate.instant(tooltipData.key, tooltipData.params) : '';

    // Errored orders show the actual ERP payload (OrderData Type = 'ERP-ERROR') instead of the
    // generic status text — the ERP message is what the user needs to act on.
    const errorText = this.getErpErrorText(element);
    return errorText || statusText;
  }

  hasErpError(element: any): boolean {
    return !!this.getErpErrorText(element);
  }

  // OrderData.Type → tooltip heading
  private errorTypeHeadings: { [key: string]: string } = {
    'ERP-ERROR': 'ERP ERROR',
    'ERPASN-ERR': 'ERP ASN ERROR',
    'ASN-ERR': 'ASN ERROR'
  };

  // Statuses that actually mean "this order is in error right now".
  private errorStatuses = ['ERROR', 'ACKERROR', 'ASNERROR'];

  /**
   * True only while the order itself is in an error state. The error payload is fetched as the
   * order's LATEST error row (Orders.cs OUTER APPLY, no status filter), so a since-recovered
   * order keeps carrying it — without this gate it would still show a stale error on hover.
   */
  isErrorStatus(element: any): boolean {
    // Status arrives raw ('ASNERROR') or as a display name ('Asn Error') — compare on letters
    // only so both spellings match.
    const s = ((element?.status ?? element?.displayStatus ?? '') + '').toUpperCase().replace(/[^A-Z]/g, '');
    return this.errorStatuses.includes(s);
  }

  /** Formats the raw error payload (OrderData) into readable tooltip lines. */
  getErpErrorText(element: any): string {
    if (!this.isErrorStatus(element)) return '';

    const raw = element?.errorData;
    if (!raw) return '';

    const label = this.errorTypeHeadings[element?.errorType] || 'ERROR';
    const when = element?.errorDate ? formatDate(element.errorDate, 'MM/dd/yyyy hh:mm a', 'en-US') : '';

    // Date sits on the heading line itself
    const heading = when ? `${label}  ·  ${when}` : label;

    let message = '';
    const detailItems: string[] = [];

    let parsed: any = null;
    try {
      parsed = JSON.parse(raw);
    } catch {
      // Not JSON (plain text / EDI payload) — show it as-is under the heading
    }

    if (parsed) {
      const output = parsed?.OutPut || parsed?.output || parsed;

      message = String(output?.Message || output?.message || '');

      const details = output?.ErrorDetail || output?.errorDetail || [];
      if (Array.isArray(details)) {
        details.forEach((d: any) => {
          const no = d?.ErrorNo || d?.errorNo || '';
          const desc = d?.ErrorDescription || d?.errorDescription || '';
          if (no || desc) detailItems.push(no ? `${no} — ${desc}` : String(desc));
        });
      }
    }

    // Nothing recognisable in the payload — fall back to the raw text
    if (!message && detailItems.length === 0) message = String(raw).trim();

    const parts = [heading];

    if (message) {
      parts.push('');
      parts.push(message);
    }

    if (detailItems.length > 0) {
      parts.push('');
      parts.push(detailItems.map(l => `•  ${l}`).join('\n'));
    }

    return parts.join('\n');
  }

  getStatusClass(status: string): string {
    if (!status) return '';
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
    } else if (status.toUpperCase() === 'PARTIALLY SHIPPED') {
      return 'acknowledged-status';
    } else if (status.toUpperCase() === 'PARTIALLY CANCELLED') {
      return 'syncerror-status';
    } else if (status.toUpperCase() === 'ERROR') {
      return 'syncerror-status';
    } else if (status.toUpperCase() === 'DUPLICATE') {
      return 'sysced-status';
    } else if (status.toUpperCase() === 'ACKERROR') {
      return 'syncerror-status';
    } else if (status.toUpperCase() === 'INPROGRESS') {
      return 'sysced-status';
    } else if (status.toUpperCase() === 'ASNERROR') {
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
    this.isLoading = true;

    this.api.ReProccess(orderNo, customerName, status, customerOrderNumber, isASNError).subscribe({
      next: (res: any) => {
        const { code, message } = res;

        if (code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: message, duration: 5000, position: 'topRight' });
          this.getOrders(false);
        } else if (code === 400) {
          this.toast.error({ detail: "ERROR", summary: message, duration: 5000, position: 'topRight' });
        } else {
          this.toast.info({ detail: "INFO", summary: message, duration: 5000, position: 'topRight' });
        }

        this.isLoading = false;
      },
      error: (err: any) => {
        const msg = err?.error?.message || err.message || 'Unexpected error';
        this.toast.error({ detail: "ERROR", summary: msg, duration: 5000, position: 'topRight' });
        this.isLoading = false;
      }
    });
  }

  ResubmitToERP(element: any) {
    const dialogRef = this.dialog.open(ResubmitConfirmDialogComponent, {
      width: '460px',
      disableClose: true,
      data: { orderNumber: element.orderNumber }
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;

      this.isLoading = true;
      this.api.ResubmitOrder(element.id, element.erpCustomerID).subscribe({
        next: (res: any) => {
          const { code, message } = res;
          if (code === 200) {
            this.toast.success({ detail: "SUCCESS", summary: message, duration: 5000, position: 'topRight' });
            this.getOrders(false);
          } else if (code === 400) {
            this.toast.error({ detail: "ERROR", summary: message, duration: 5000, position: 'topRight' });
          } else {
            this.toast.info({ detail: "INFO", summary: message, duration: 5000, position: 'topRight' });
          }
          this.isLoading = false;
        },
        error: (err: any) => {
          const msg = err?.error?.message || err.message || 'Unexpected error';
          this.toast.error({ detail: "ERROR", summary: msg, duration: 5000, position: 'topRight' });
          this.isLoading = false;
        }
      });
    });
  }

  // Status arrives as DisplayStatus ('Shipped'/'Synced', mixed case) — compare case-insensitively.
  private rowStatus(el: any): string {
    return ((el?.status ?? el?.displayStatus ?? '') + '').toUpperCase();
  }
  isSynced(el: any): boolean { return this.rowStatus(el) === 'SYNCED'; }
  isShipped(el: any): boolean { return this.rowStatus(el) === 'SHIPPED'; }

  ReTransmitASN(element: any) {
    const dialogRef = this.dialog.open(ReTransmitConfirmDialogComponent, {
      width: '460px',
      disableClose: true,
      data: { orderNumber: element.orderNumber }
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;

      this.isLoading = true;
      this.api.ReTransmitASN(element.id, element.erpCustomerID).subscribe({
        next: (res: any) => {
          const { code, message } = res;
          if (code === 200) {
            this.toast.success({ detail: "SUCCESS", summary: message, duration: 5000, position: 'topRight' });
            this.getOrders(false);
          } else if (code === 400) {
            this.toast.error({ detail: "ERROR", summary: message, duration: 5000, position: 'topRight' });
          } else {
            this.toast.info({ detail: "INFO", summary: message, duration: 5000, position: 'topRight' });
          }
          this.isLoading = false;
        },
        error: (err: any) => {
          const msg = err?.error?.message || err.message || 'Unexpected error';
          this.toast.error({ detail: "ERROR", summary: msg, duration: 5000, position: 'topRight' });
          this.isLoading = false;
        }
      });
    });
  }

  getERPCustomer() {
    this.ERPApi.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customerOptions = res.customers;
        this.filteredCustomerOptions = this.customerOptions || [];
      },
    });
  }

  filterCustomerOptions() {
    const search = this.customerSearchText.toLowerCase();
    this.filteredCustomerOptions = (this.customerOptions || []).filter(c =>
      c.name.toLowerCase().includes(search) || c.erpCustomerID.toLowerCase().includes(search)
    );
  }

  onCustomerSelectOpened(opened: boolean) {
    if (opened) {
      this.customerSearchText = '';
      this.filteredCustomerOptions = this.customerOptions || [];
    }
  }

  private getFilterableStatuses(): string[] {
    return this.statusOptions.filter(s => s !== 'Select Status');
  }

  filterStatusOptions() {
    const search = this.statusSearchText.toLowerCase();
    this.filteredStatusOptions = this.getFilterableStatuses().filter(s =>
      s.toLowerCase().includes(search)
    );
  }

  onStatusSelectOpened(opened: boolean) {
    if (opened) {
      this.statusSearchText = '';
      this.filteredStatusOptions = this.getFilterableStatuses();
    }
  }

  openHelp(): void {
    this.dialog.open(OrderHelpDialogComponent, {
      width: '90%',
      maxWidth: '1200px',
      maxHeight: '90vh',
    });
  }

  editOrder(data: any) {
    const dialogRef = this.dialog.open(OrderDetailComponent, {
      width: '80%',
      maxWidth: '950px',
      maxHeight: '92vh',
      disableClose: true,
      data: {
        orderData: data
      }
    });

    // dialogRef.afterClosed()
    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getOrders(false);
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
          this.getOrders(false);
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
          this.getOrders(false);
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
          this.getOrders(false);
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
          this.getOrders(false);
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
          this.getOrders(false);
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
          this.getOrders(false);
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
          width: '85%',
          maxWidth: '1200px',
          disableClose: true,
          panelClass: 'elevated-dialog-panel',
          backdropClass: 'elevated-dialog-backdrop',
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

  formatStatus(status: string): string {
    if (!status) return '';
    return this.statusValueToDisplay[status] || status;
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  getOrders(resetPage: boolean = false) {
    if (resetPage) {
      this.pageNumber = 1;
    }
    let orderId = (this.OrderForm.get('orderId') as FormControl).value;
    let fromDate = (this.OrderForm.get('fromDate') as FormControl).value;
    let toDate = (this.OrderForm.get('toDate') as FormControl).value;
    let orderNo = (this.OrderForm.get('orderNo') as FormControl).value;
    let soNo = (this.OrderForm.get('soNo') as FormControl).value;
    let status = (this.OrderForm.get('status') as FormControl).value;
    let customerName = (this.OrderForm.get('customerName') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';

    if (((customerName == '' || customerName == null || customerName == undefined) && (orderId == '' || orderId == null || orderId == 0) && (fromDate == '' || fromDate == null) && (toDate == '' || toDate == undefined) && (orderNo == '' || orderNo == 'EMPTY') && (status == '' || status == 'Select Status') && (soNo == '' || soNo == 'EMPTY' || soNo == null || soNo == undefined))) {
      this.toast.info({ detail: "orderId", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
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
    } else {
      status = this.statusDisplayToValue[status] || status;
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
    this.isLoading = true;
    this.api.getOrders(orderId, stringFromDate, stringToDate, orderNo, status, soNo, customerName, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        this.listOfOrders = res.ordersData ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.ordersToDisplay = this.listOfOrders;

        if (this.listOfOrders.length === 0 && this.pageNumber === 1) {
          this.toast.info({
            detail: "INFO",
            summary: this.languageService.getTranslation('noFilterDataMessage'),
            duration: 5000,
            position: 'topRight'
          });
        }

        if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, position: 'topRight' });
        }

        this.isLoading = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.isLoading = false;
      },
    });

  }
}
