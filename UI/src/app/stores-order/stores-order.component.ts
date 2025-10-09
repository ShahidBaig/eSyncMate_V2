import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { Component, OnInit, Inject } from '@angular/core';
import { DatePipe, NgIf } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../services/api.service';
import { NgToastService } from 'ng-angular-popup';
import { Order } from '../models/models';
import { CommonModule } from '@angular/common';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'stores-order',
  templateUrl: './stores-order.component.html',
  standalone: true,
  styleUrls: ['./stores-order.component.scss'],
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
    TranslateModule
  ],
})
export class StoresOrderComponent {
  showSpinner: boolean = false;
  showDataColumn: boolean = true;
  msg: string = '';
  code: number = 0;
  storesData: Order[] = [];
  dataLength: number = 0;
  count: number = 0;
  parentOrderId: number = 0;
  markForASNDataLength: number = 0;
  countMarkForASN: number = 0;
  createInvoiceDataLength: number = 0;
  countCreateInvoice: number = 0;

  columns: string[] = [
    'id',
    'Status',
    'CustomerPO',
    'SyncOrderStore',
  ];

  constructor(@Inject(MAT_DIALOG_DATA) public data: any, private api: ApiService, private toast: NgToastService, private dialog: MatDialog, public dialogRef: MatDialogRef<StoresOrderComponent>,
  public languageService: LanguageService) {
    this.parentOrderId = data[0].orderId;
  }

  async syncStoreOrder(storeOrderId: any, orderId: any, isSelectedAll: Boolean) {
    if (isSelectedAll === false) {
      this.showSpinner = true;
    }

    await (await this.api.syncOrderStore(storeOrderId)).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;
        if (isSelectedAll === true) {
          this.count++;
        }

        if (isSelectedAll === false) {
          if (this.code === 200) {
            this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
            this.getStoresOrder(orderId);
            this.showSpinner = false;
          }
          else if (this.code === 400) {
            this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
            this.showSpinner = false;
          } else {
            this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
            this.showSpinner = false;
          }

          if (isSelectedAll === false) {
            this.showSpinner = false;
          }
        }

        if (isSelectedAll === true && this.dataLength === this.count) {
          this.getStoresOrder(orderId);
          this.count = 0;
          this.dataLength = 0;
          this.showSpinner = false;
        }
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        if (isSelectedAll === true && this.dataLength === this.count) {
          this.getStoresOrder(orderId);
        }
        this.showSpinner = false;
      },
    });
  }

  syncAllStoreOrders(dataSource: any[]) {
    var eSyncMateID = 0;
    this.showSpinner = true;

    const newData = dataSource.filter(data => data.status !== 'SYNCED');
    this.dataLength = newData.length;

    for (const data of newData) {
      this.syncStoreOrder(data.id, data.orderId, true);
      eSyncMateID = data.orderId;
    }
  }

  getStoresOrder(orderId: any) {
    this.api.getStoresOrder(orderId).subscribe({
      next: (res: any) => {
        this.storesData = res.orderStores;
        this.msg = res.message;
        this.code = res.code;

        this.toast.success({ detail: "SUCCESS", summary: this.languageService.getTranslation('processSuccessMsg'), duration: 5000, position: 'topRight' });
        this.data = this.storesData;
        this.showSpinner = false;

      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  getStatusClass(status: string): string {
    if (status === 'NEW') {
      return 'new-status';
    } else if (status === 'SYNCERROR') {
      return 'syncerror-status';
    } else if (status === 'SYNCED') {
      return 'sysced-status';
    } else {
      return '';
    }
  }

  closeDialog(): void {
    this.dialogRef.close();
  }

  uploadGenerateASNForStoreOrders(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selectedFileASN = input.files?.[0];

    if (selectedFileASN) {
      const fileNameParts = selectedFileASN.name.split('.');
      const fileExtension = fileNameParts[fileNameParts.length - 1].toLowerCase();

      if (fileExtension === 'csv') {
        this.api.generateASNForStoreOrders(selectedFileASN, this.parentOrderId).subscribe({
          next: (res: any) => {
            this.msg = res.message;
            this.code = res.code;

            if (this.code === 200) {
              this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
              this.getStoresOrder(this.parentOrderId);
              this.showSpinner = false;
            }
            else if (this.code === 400) {
              this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
              this.showSpinner = false;
            } else {
              this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
              this.showSpinner = false;
            }

            input.value = '';
          },
          error: (err: any) => {
            this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
            this.showSpinner = false;
            input.value = '';
          },
        });
      } else {
        this.toast.error({ detail: this.languageService.getTranslation('csvFileMsg'), summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
        input.value = '';
      }
    }
  }

  markForASNForStoreOrdersSelectAll(dataSource: any[]) {
    this.showSpinner = true;

    const newData = dataSource.filter(data => data.status === 'ASNGEN');

    if (newData.length === 0) {
      this.toast.error({ detail: "no data exists to process the Mark for ASN for store orders.", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinner = false;
      return;
    }

    this.markForASNDataLength = newData.length;

    for (const data of newData) {
      this.markForASNForStoreOrders(data.id);
    }
  }

  markForASNForStoreOrders(orderStoreId: any) {
    this.api.markForASNForStoreOrders(orderStoreId).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;
        this.countMarkForASN++;

        if (this.markForASNDataLength === this.countMarkForASN) {
          this.countMarkForASN = 0;
          this.markForASNDataLength = 0;
          this.showSpinner = false;
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.getStoresOrder(this.parentOrderId);
          this.showSpinner = false;
        }
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  createInvoiceForStoreOrdersSelectAll(dataSource: any[]) {
    this.showSpinner = true;

    const newData = dataSource.filter(data => data.status === 'ASNMARK');

    if (newData.length === 0) {
      this.toast.error({ detail: "no data exists to process the invoice for store orders.", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinner = false;
      return;
    }

    this.createInvoiceDataLength = newData.length;

    for (const data of newData) {
      this.createInvoiceForStoreOrders(data.id);
    }
  }

  createInvoiceForStoreOrders(orderStoreId: any) {
    this.api.createInvoiceForStoreOrders(orderStoreId).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;
        this.countCreateInvoice++;

        if (this.createInvoiceDataLength === this.countCreateInvoice) {
          this.countCreateInvoice = 0;
          this.createInvoiceDataLength = 0;
          this.showSpinner = false;
          this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.getStoresOrder(this.parentOrderId);
          this.showSpinner = false;
        }
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  uploadGenerate810ForStoreOrders(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selectedFile = input.files?.[0];

    if (selectedFile) {
      const fileNameParts = selectedFile.name.split('.');
      const fileExtension = fileNameParts[fileNameParts.length - 1].toLowerCase();

      if (fileExtension === 'csv') {
        this.api.generate810ForStoreOrders(selectedFile, this.parentOrderId).subscribe({
          next: (res: any) => {
            this.msg = res.message;
            this.code = res.code;

            if (this.code === 200) {
              this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
              this.getStoresOrder(this.parentOrderId);
              this.showSpinner = false;
            }
            else if (this.code === 400) {
              this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
              this.showSpinner = false;
            } else {
              this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
              this.showSpinner = false;
            }

            input.value = '';
          },
          error: (err: any) => {
            this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
            this.showSpinner = false;
            input.value = '';
          },
        });
      } else {
        this.toast.error({ detail: "Invalid file type. Please select a .csv file.", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
        input.value = '';
      }
    }
  }
}

