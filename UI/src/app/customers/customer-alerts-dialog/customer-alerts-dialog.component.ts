import { Component, Inject, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MAT_DIALOG_DATA, MatDialogRef, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';

import { CustomersService } from '../../services/customers.service';
import { CustomerAlertAddDialogComponent } from '../customer-alert-add-dialog/customer-alert-add-dialog.component';


export interface CustomerAlertRow {
  alertId: number;
  alertName: string;
  customerId: number;
  dayOfMonth: string;
  emails: string;
  executionTime: string;
  frequencyType: string;
  id: number;
  repeatCount: number;
  status: string;
  weekDays: string;
}

export interface CustomerAlertsResponse {
  alerts: CustomerAlertRow[];
}

@Component({
  selector: 'app-customer-alerts-dialog',
  standalone: true,
  templateUrl: './customer-alerts-dialog.component.html',
  styleUrls: ['./customer-alerts-dialog.component.scss'],
  imports: [
    CommonModule,
    MatDialogModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatPaginatorModule
  ]
})
export class CustomerAlertsDialogComponent implements OnInit, AfterViewInit {

  alerts: CustomerAlertRow[] = [];
  displayedColumns: string[] = ['alertName', 'frequencyType', 'emails', 'actions'];

  loading = false;
  dataSource = new MatTableDataSource<CustomerAlertRow>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(
    @Inject(MAT_DIALOG_DATA)
    public data: { customerId: number; customerName: string },

    private dialogRef: MatDialogRef<CustomerAlertsDialogComponent>,
    private customersService: CustomersService,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this.loadAlerts();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
  }

  loadAlerts(): void {
    this.loading = true;

    this.customersService.getCustomerAlerts(this.data.customerId).subscribe({
      next: (res: CustomerAlertsResponse) => {
        this.alerts = res?.alerts ?? [];
        this.dataSource.data = this.alerts;

        // attach paginator safely after data refresh
        if (this.paginator) {
          this.dataSource.paginator = this.paginator;
        }

        this.loading = false;
      },
      error: err => {
        console.error('Error loading alerts', err);
        this.alerts = [];
        this.dataSource.data = [];
        this.loading = false;
      }
    });
  }

  onAdd(): void {
    const usedAlertIds = this.alerts.map(a => a.alertId);

    const dlg = this.dialog.open(CustomerAlertAddDialogComponent, {
      width: '800px',
      disableClose: true,
      data: {
        customerId: this.data.customerId,
        customerName: this.data.customerName,
        alert: null,
        isEdit: false,
        usedAlertIds
      }
    });

    dlg.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.loadAlerts();
      }
    });
  }

  onEdit(row: CustomerAlertRow): void {
    const dlg = this.dialog.open(CustomerAlertAddDialogComponent, {
      width: '800px',
      disableClose: true,
      data: {
        customerId: this.data.customerId,
        customerName: this.data.customerName,
        alert: row,
        isEdit: true
      }
    });

    dlg.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.loadAlerts();
      }
    });
  }

  onDelete(row: CustomerAlertRow): void {
    this.customersService.deleteCustomerAlert(row).subscribe({
      next: (res: CustomerAlertsResponse) => {
        this.loadAlerts();
      },
      error: err => {
        console.error('Error loading alerts', err);
      }
    });
  }

  close(): void {
    this.dialogRef.close('updated');
  }
}
