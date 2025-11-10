import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { BatchWiseInventory, Inventory, Order, ShipmentDetailFromNDC, ShipmentFromNDC } from '../models/models';
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
import { InventoryService } from '../services/inventory.service';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
import { ShipmentFromNDCService } from '../services/shipmentFromNDC.service';
import { DetailShipmentFromNdcComponent } from './detail-shipment-from-ndc/detail-shipment-from-ndc.component';

@Component({
  selector: 'sipment-from-ndc',
  templateUrl: './sipment-from-ndc.component.html',
  styleUrls: ['./sipment-from-ndc.component.scss'],
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
export class SipmentFromNdcComponent {
  mydate = environment.date;

  listOfInventory: ShipmentFromNDC[] = [];
  inventoryToDisplay: ShipmentFromNDC[] = [];
  listOfShipmentDetailFromNDC: ShipmentDetailFromNDC[] = []
  ShipmentFromNDC: FormGroup;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  statusOptions = ['Select Status', 'NEW', 'SYNCED'];

  columns: string[] = [
    'PoNumber',
    'PoDate',
    'ShipmentID',
    'TransactionDate',
    'Detail',
  ];

  constructor(private translate: TranslateService, private api: ShipmentFromNDCService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, public languageService: LanguageService) {
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.ShipmentFromNDC = this.fb.group({
      //customerIDId: fb.control(''),
      shipmentID: fb.control(''),
      transactionDate: new FormControl(formatDate(sevenDaysAgo, "yyyy-MM-dd", "en")),
      poNumber: fb.control(''),
      status: fb.control(''),
    });
  }

  ngOnInit(): void {
    this.getShipmentFromNDC();
  }

 

  getStatusTooltip(status: string, batchID: string): any {
    switch (status) {
      case 'NEW':
        return { key: 'NEW' };
      case 'SYNC':
        return { key: 'SYNCED' };
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
    } else if (status.toLocaleUpperCase() === 'ERROR') {
      return 'syncerror-status';
    } else if (status.toLocaleUpperCase() === 'SYNC') {
      return 'sysced-status';
    } else {
      return '';
    }
  }

  //getInventoryFiles(element: any) {
  //  let customerId = element.customerID;
  //  let itemId = element.itemId;

  //  this.showSpinner = false;

  //  this.api.getInventoryFiles(customerId, itemId).subscribe({
  //    next: (res: any) => {
  //      this.listOfInventoryFiles = res.files;
  //      this.msg = res.message;
  //      this.code = res.code;

  //      if (this.listOfInventoryFiles.length === 0) {
  //        this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noInventoryDataMsg'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
  //        this.showSpinner = false;

  //        return;
  //      }

  //      const dialogRef = this.dialog.open(InventorypopupComponent, {
  //        width: '70%',
  //        data: this.listOfInventoryFiles,
  //      });

  //      dialogRef.afterClosed().subscribe(result => {
  //        console.log('The dialog was closed');
  //      });

  //      this.showSpinner = false;
  //    },
  //    error: (err: any) => {
  //      this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
  //      this.showSpinner = false;
  //    },
  //  });
  //}



  getBatchiWiseInventory(element: any) {
    let batchID = element.id;

    this.showSpinner = false;

    this.api.getbatchWise(batchID).subscribe({
      next: (res: any) => {
        this.listOfShipmentDetailFromNDC = res.shipmentDetailFromNDC;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfShipmentDetailFromNDC.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noshipmentdetailDataMsg'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;

          return;
        }

        const dialogRef = this.dialog.open(DetailShipmentFromNdcComponent, {
          width: '50%',
          data: {
            listofDetail: this.listOfShipmentDetailFromNDC
             // Add itemID only if it exists, otherwise set as null
          },
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
    this.inventoryToDisplay = this.listOfInventory.slice(startIndex, startIndex + event.pageSize);
  }

  getShipmentFromNDC() {
    //let customerID = (this.InventoryForm.get('customerID') as FormControl).value;
    let shipmentID = (this.ShipmentFromNDC.get('shipmentID') as FormControl).value;
    let transactionDate = (this.ShipmentFromNDC.get('transactionDate') as FormControl).value;
    let status = (this.ShipmentFromNDC.get('status') as FormControl).value;
    let poNumber = (this.ShipmentFromNDC.get('poNumber') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';
    this.showSpinnerforSearch = true;

    if (((transactionDate == '' || transactionDate == null) && (shipmentID == '' || shipmentID == 'EMPTY') && (status == '' || status == 'Select Status') && (poNumber == '' || poNumber.toUpperCase().includes('SELECT')))) {
      this.toast.info({ detail: "inventory", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
    }

    if (shipmentID == '') {
      shipmentID = 'EMPTY'
    }

    if (status == 'Select Status' || status == '') {
      status = 'EMPTY'
    }

    if (poNumber == '') {
      poNumber = 'EMPTY'
    }


    if (transactionDate !== null) {
      stringFromDate = transactionDate.toLocaleString();

      if (stringFromDate.length > 10) {
        stringFromDate = this.getFormattedDate(transactionDate);
      }
    } else {
      stringFromDate = '1999-01-01';
    }

    this.api.getInventory(shipmentID, stringFromDate, status, poNumber).subscribe({
      next: (res: any) => {
        this.listOfInventory = res.shipmentFromNDC;
        this.msg = res.message;
        this.code = res.code;


        if (this.listOfInventory == null || this.listOfInventory.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.inventoryToDisplay = [];

          return;
        }

        this.inventoryToDisplay = this.listOfInventory.slice(0, 10);

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
