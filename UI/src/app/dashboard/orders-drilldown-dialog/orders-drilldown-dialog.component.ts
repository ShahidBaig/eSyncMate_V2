import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule, DatePipe, formatDate } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NgToastService } from 'ng-angular-popup';
import { ApiService } from '../../services/api.service';
import { LanguageService } from '../../services/language.service';
import { OrderDetailComponent } from '../../orders/order-detail/order-detail.component';
import { PopupComponent } from '../../popup/popup.component';
import { ResubmitConfirmDialogComponent } from '../../orders/resubmit-confirm-dialog/resubmit-confirm-dialog.component';
import { ReTransmitConfirmDialogComponent } from '../../orders/retransmit-confirm-dialog/retransmit-confirm-dialog.component';

@Component({
  selector: 'orders-drilldown-dialog',
  templateUrl: './orders-drilldown-dialog.component.html',
  styleUrls: ['./orders-drilldown-dialog.component.scss'],
  standalone: true,
  imports: [CommonModule, DatePipe, MatButtonModule, MatIconModule, MatProgressBarModule, MatTableModule, MatPaginatorModule, MatTooltipModule],
})
export class OrdersDrilldownDialogComponent implements OnInit {
  orders: any[] = [];
  isLoading = false;
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  reprocessingId: number | null = null;
  lastOrderDate: string | null = null;
  canEdit = false;
  canResubmit = false;
  canReTransmit = false;
  showResubmitAction = false;
  showReTransmitAction = false;

  errorStatuses = ['ERROR', 'ASNERROR', 'ACKERROR', 'SYNCERROR'];

  // Statuses where the order already exists in the ERP — ERP columns are meaningful only for these
  erpStatuses = ['SYNCED', 'SHIPPED', 'CANCELLED', 'Partially Shipped', 'Partially Cancelled'];

  get isErrorStatus(): boolean {
    return this.errorStatuses.includes(this.data.status);
  }

  // Statuses that never carry a shipped date back from the ERP
  noShippedDateStatuses = ['SYNCED', 'Partially Cancelled'];

  // No status = Total Orders drilldown — ERP SO No / ERP Created Date still apply
  get isErpStatus(): boolean {
    if (!this.data.status) return true;
    return this.erpStatuses.some(s => s.toUpperCase() === this.data.status.toUpperCase());
  }

  get showShippedDate(): boolean {
    if (!this.data.status) return false;
    return this.isErpStatus &&
      !this.noShippedDateStatuses.some(s => s.toUpperCase() === this.data.status.toUpperCase());
  }

  // Actions column always shown first (View Order / Show Files available for every row)
  get columns(): string[] {
    if (this.isErpStatus) {
      const cols = ['actions', 'orderNumber', 'orderDate', 'externalId', 'erpCreatedDate', 'erpCustomerID', 'displayStatus', 'createdDate'];
      if (this.showShippedDate) cols.push('shippedDate');
      return cols;
    }
    return ['actions', 'orderNumber', 'orderDate', 'erpCustomerID', 'displayStatus', 'createdDate'];
  }

  // Re-Process only applies to error rows
  isRowError(row: any): boolean {
    return this.errorStatuses.includes(row?.status);
  }

  // Status can arrive as DisplayStatus ('Synced'/'Shipped', mixed case) — compare case-insensitively.
  private rowStatus(row: any): string {
    return ((row?.status ?? row?.displayStatus ?? '') + '').toUpperCase();
  }

  isRowSynced(row: any): boolean {
    return this.rowStatus(row) === 'SYNCED';
  }

  isRowShipped(row: any): boolean {
    return this.rowStatus(row) === 'SHIPPED';
  }

  // OrderData.Type → tooltip heading
  private errorTypeHeadings: { [key: string]: string } = {
    'ERP-ERROR': 'ERP ERROR',
    'ERPASN-ERR': 'ERP ASN ERROR',
    'ASN-ERR': 'ASN ERROR'
  };

  hasErpError(row: any): boolean {
    return !!this.getErpErrorText(row);
  }

  /** Formats the raw error payload (OrderData) into readable tooltip lines. */
  getErpErrorText(row: any): string {
    const raw = row?.errorData;
    if (!raw) return '';

    const label = this.errorTypeHeadings[row?.errorType] || 'ERROR';
    const when = row?.errorDate ? formatDate(row.errorDate, 'MM/dd/yyyy hh:mm a', 'en-US') : '';
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

  constructor(
    public dialogRef: MatDialogRef<OrdersDrilldownDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: {
      status: string;
      customerID: string;
      fromDate: string;
      toDate: string;
      title: string;
      color: string;
      icon: string;
    },
    private api: ApiService,
    private dialog: MatDialog,
    private toast: NgToastService,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    // Role access — Resubmit / Re-Transmit require edit permission on Orders (same as the Orders screen)
    const isAdmin = ['ADMIN', 'WRITER'].includes(this.api.getTokenUserInfo()?.userType || '');
    const permissions = this.api.getMenuPermissions('edi/all-orders');
    if (permissions) {
      this.canEdit = permissions.canEdit;
      // Assigned value when present; fall back to admin if missing (older cached menus).
      this.canResubmit = permissions.canResubmit ?? isAdmin;
      this.canReTransmit = permissions.canReTransmit ?? isAdmin;
    } else {
      this.canEdit = isAdmin;
      this.canResubmit = isAdmin;
      this.canReTransmit = isAdmin;
    }
    // Global feature toggles (live) — flag 0 hides the action button entirely
    this.api.getActionColumnVisibility().subscribe({
      next: (res: any) => {
        this.showResubmitAction = !!res?.showResubmit;
        this.showReTransmitAction = !!res?.showReTransmit;
      },
      error: () => { /* default hidden */ }
    });
    this.loadOrders();
  }

  loadOrders(): void {
    this.isLoading = true;
    const status = this.data.status || 'EMPTY';
    const customerId = this.data.customerID || 'EMPTY';
    const fromDate = this.data.fromDate || '';
    const toDate = this.data.toDate || '';

    this.api.getDashboardOrders(status, customerId, fromDate, toDate, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.orders = res.ordersData || res.orders || [];
        this.totalCount = res.totalCount || 0;
        // Orders come back CreatedDate DESC — the top row of page 1 is the latest order.
        // Captured only on page 1 so paging doesn't change the "last order" value.
        if (this.pageNumber === 1 && this.orders.length) {
          this.lastOrderDate = this.orders[0].createdDate || null;
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadOrders();
  }

  reprocessOrder(element: any): void {
    this.reprocessingId = element.id;
    const isASNError = element.status === 'ASNERROR' ? 1 : 0;

    this.api.ReProccess(element.id, element.erpCustomerID, element.status, element.orderNumber, isASNError).subscribe({
      next: (res: any) => {
        this.reprocessingId = null;
        if (res.code === 200) {
          this.loadOrders();
        }
      },
      error: () => {
        this.reprocessingId = null;
      }
    });
  }

  resubmitOrder(element: any): void {
    const ref = this.dialog.open(ResubmitConfirmDialogComponent, {
      width: '460px', disableClose: true, data: { orderNumber: element.orderNumber }
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.reprocessingId = element.id;
      this.api.ResubmitOrder(element.id, element.erpCustomerID).subscribe({
        next: (res: any) => {
          this.reprocessingId = null;
          if (res.code === 200) { this.toast.success({ detail: "SUCCESS", summary: res.message, duration: 5000, position: 'topRight' }); this.loadOrders(); }
          else { this.toast.error({ detail: "ERROR", summary: res.message, duration: 5000, position: 'topRight' }); }
        },
        error: (err: any) => { this.reprocessingId = null; this.toast.error({ detail: "ERROR", summary: err?.error?.message || err.message, duration: 5000, position: 'topRight' }); }
      });
    });
  }

  reTransmitASN(element: any): void {
    const ref = this.dialog.open(ReTransmitConfirmDialogComponent, {
      width: '460px', disableClose: true, data: { orderNumber: element.orderNumber }
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.reprocessingId = element.id;
      this.api.ReTransmitASN(element.id, element.erpCustomerID).subscribe({
        next: (res: any) => {
          this.reprocessingId = null;
          if (res.code === 200) { this.toast.success({ detail: "SUCCESS", summary: res.message, duration: 5000, position: 'topRight' }); this.loadOrders(); }
          else { this.toast.error({ detail: "ERROR", summary: res.message, duration: 5000, position: 'topRight' }); }
        },
        error: (err: any) => { this.reprocessingId = null; this.toast.error({ detail: "ERROR", summary: err?.error?.message || err.message, duration: 5000, position: 'topRight' }); }
      });
    });
  }

  getOrderFiles(row: any): void {
    this.api.getOrderFiles(row.id).subscribe({
      next: (res: any) => {
        const files = res.files || [];

        if (files.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noOrderDataMsg'), duration: 5000, position: 'topRight' });
          return;
        }

        this.dialog.open(PopupComponent, {
          width: '85%',
          maxWidth: '1200px',
          disableClose: true,
          panelClass: 'elevated-dialog-panel',
          backdropClass: 'elevated-dialog-backdrop',
          data: {
            listOfOrderFiles: files,
            orderNumber: row.orderNumber
          }
        });
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
      }
    });
  }

  viewOrder(row: any): void {
    this.dialog.open(OrderDetailComponent, {
      width: '80%',
      maxWidth: '950px',
      maxHeight: '92vh',
      disableClose: true,
      data: { orderData: row }
    });
  }

  onClose(): void {
    this.dialogRef.close();
  }
}
