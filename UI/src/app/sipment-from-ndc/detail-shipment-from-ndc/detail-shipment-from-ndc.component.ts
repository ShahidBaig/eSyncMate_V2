import { Component, Inject, OnInit, NgModule } from '@angular/core';
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
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { FileContentViewerDialogComponent } from '../../file-content-viewer-dialog/file-content-viewer-dialog.component';
import { RouteDataService } from '../../services/routedata.service';
import { BatchWiseInventory, Inventory, RouteData, ShipmentDetailFromNDC } from '../../models/models';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import * as moment from 'moment';
import { environment } from 'src/environments/environment';
import { ShipmentFromNDCService } from '../../services/shipmentFromNDC.service';

@Component({
  selector: 'detail-shipment-from-ndc',
  templateUrl: './detail-shipment-from-ndc.component.html',
  styleUrls: ['./detail-shipment-from-ndc.component.scss'],
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
export class DetailShipmentFromNdcComponent {
  mydate = environment.date;
  displayedColumns: string[] = ['ShipmentID', 'PoNumber', 'Status', 'EDILineID', 'ItemID', 'QTY', 'LotNumber', 'ExpirationDate', 'SupplierStyle', 'UPC', 'SKU', 'TrackingNo', 'SSCC', 'BOLNO', 'BarCode', 'CarrierName' ]; 
  dataSource = this.data.listofDetail || [];
  id?: string;
  showSpinner: boolean = false;
  showDataColumn: boolean = true;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  loadingStates = new Map<number, boolean>();
  listShipmentDetailFromNDC: ShipmentDetailFromNDC[] = [];
  constructor(

    public dialogRef: MatDialogRef<DetailShipmentFromNdcComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any, private dialog: MatDialog, private api: ShipmentFromNDCService, private fb: FormBuilder, private toast: NgToastService, public languageService: LanguageService) {
    this.id = data.listofDetail[0].shipmentID;

    const threeDaysAgo = new Date();
    threeDaysAgo.setDate(threeDaysAgo.getDate() - 3);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

   
  }
  ngOnInit(): void {
    this.listShipmentDetailFromNDC = this.dataSource;
    this.dataSource = this.listShipmentDetailFromNDC.slice(0, 10);
    console.log(this.dataSource);
  }

  onCancel() {
    this.dialogRef.close();
  }

  isLoading(fileId: number): boolean {
    // Retrieve loading state for a specific fileId
    return this.loadingStates.get(fileId) || false;
  }

  

  getFormattedDate(date: any) {
    // let year = date.getFullYear();
    // let month = (1 + date.getMonth()).toString().padStart(2, '0');
    // let day = date.getDate().toString().padStart(2, '0');

    // return year + '-' + month + '-' + day;
    return moment(date).format('YYYY-MM-DD');
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.dataSource = this.listShipmentDetailFromNDC.slice(startIndex, startIndex + event.pageSize);
  }


  
}
