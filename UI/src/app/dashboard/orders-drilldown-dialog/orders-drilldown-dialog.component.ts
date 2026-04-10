import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../services/api.service';

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

  errorStatuses = ['ERROR', 'ASNERROR', 'ACKERROR', 'SYNCERROR'];

  get isErrorStatus(): boolean {
    return this.errorStatuses.includes(this.data.status);
  }

  get columns(): string[] {
    const cols = ['orderNumber', 'erpCustomerID', 'displayStatus', 'orderDate', 'createdDate'];
    if (this.isErrorStatus) cols.push('actions');
    return cols;
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
    private api: ApiService
  ) {}

  ngOnInit(): void {
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

  onClose(): void {
    this.dialogRef.close();
  }
}
