import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { CarrierLoadTender } from '../models/models';
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
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { CarrierLoadTenderService } from '../services/carrierLoadTender.service';
import { ApiService } from '../services/api.service';
import { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { StatesModel } from '../models/models';
import { environment } from 'src/environments/environment';
import { UpdateStatusModalComponent } from '../update-status-modal/update-status-modal.component';

@Component({
  selector: 'carrier-load-tender',
  templateUrl: './carrier-load-tender.component.html',
  styleUrls: ['./carrier-load-tender.component.scss'],
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
export class CarrierLoadTenderComponent implements OnInit {
  mydate = environment.date;
  listOfCarrier: CarrierLoadTender[] = [];
  carrierToDisplay: CarrierLoadTender[] = [];
  CarrierLoadTenderForm: FormGroup;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  listAckData: any[] = [];
  listStatesData: StatesModel[] = [];
  statusOptions = ['Select Status', 'NEW', 'ACK', 'REPLIED', 'COMPLETE', 'ACKNOWLEDGE'];
  isAdminUser: boolean = false;

  columns: string[] = [
    'id',
    'Status',
    'CustomerName',
    'DocumentDate',
    'CreatedDate',
    'ShipmentId',
    'ShipperNo',
    'CompletionDate',
    'File'
    //'OrderStatus'
  ];

  constructor(public apiService: ApiService, private api: CarrierLoadTenderService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog,
    public languageService: LanguageService, private Userapi: ApiService) {
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);
    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.isAdminUser = ["ADMIN", "WRITER"].includes(this.Userapi.getTokenUserInfo()?.userType || '');

    this.CarrierLoadTenderForm = this.fb.group({
      carrierLoadTenderId: fb.control(''),
      fromDate: new FormControl(formatDate(sevenDaysAgo, "yyyy-MM-dd", "en")),
      toDate: new FormControl(formatDate(today, "yyyy-MM-dd", "en")),
      shipmentId: fb.control(''),
      shipmentShipperNo: fb.control(''),
      status: fb.control('')
    });
  }

  ngOnInit(): void {
    if (!this.isAdminUser) {
      const editIndex = this.columns.indexOf('File');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }

    this.getCarrierLoadTender();
    this.getAckData();
    this.getStatesData();
  }

  getStatusClass(status: string): string {
    if (status === 'NEW') {
      return 'new-status';
    }
    else if (status === 'REPLIED') {
      return 'processed-status';
    }
    else if (status === 'ACK') {
      return 'acknowledged-status';
    }
    else if (status === 'COMPLETE') {
      return 'complete-status';
    }
    else if (status === 'ReadyToComplete') {
      return 'readyToComplete-status';
    }
    else if (status === 'ACKNOWLEDGE') {
      return 'acknowledged-status';
    }
    else {
      return '';
    }
  }

  updateAckStatus(ackStatus: string, element: any) {
    element.ackStatus = ackStatus;
    this.showSpinner = true;

    this.api.updateAckStatus(element).subscribe({
      next: (res: any) => {
        if (res && res.code === 200) {
          this.toast.success({ detail: 'SUCCESS', summary: res.message, duration: 5000, position: 'topRight' });
          this.getCarrierLoadTender();
        } else {
          this.toast.error({ detail: 'ERROR', summary: res.message, duration: 5000, position: 'topRight' });
        }
        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: 'ERROR', summary: err.message, duration: 5000, position: 'topRight' });
        this.showSpinner = false;
      }
    });
  }

  getCarrierLoadTenderFiles(element: any) {
    let carrierLoadTenderId = element.id;
    this.showSpinner = false;

    this.api.getCarrierLoadTenderFiles(carrierLoadTenderId).subscribe({
      next: (res: any) => {
        this.listOfCarrier = res.files;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfCarrier.length === 0) {
          this.toast.info({ detail: "INFO", summary: 'No data found.', duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;

          return;
        }

        const dialogRef = this.dialog.open(PopupComponent, {
          width: '70%',
          disableClose: true,
          data: {
            listOfOrderFiles: this.listOfCarrier,
            orderNumber: element.id
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

  getAckData() {
    this.api.getAckData().subscribe({
      next: (res: any) => {
        if (res.ackData)
          this.listAckData = res.ackData;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
      },
    });
  }

  getStatesData() {
    this.api.getStatesData().subscribe({
      next: (res: any) => {
        if (res.statesData)
          this.listStatesData = res.statesData;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
      },
    });
  }

  openTrackAckPopUp(element: any) {
    const dialogRef = this.dialog.open(PopupComponent, {
      width: '55%',
      height: '50%',
      disableClose: true,
      data: {
        listOfCarrier: element,
        listAckData: this.listAckData,
        listStatesData: this.listStatesData,
        orderNumber: element.id
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      this.getCarrierLoadTender();
    });
  }

  getCarrierLoadTender() {
    let carrierLoadTenderId = (this.CarrierLoadTenderForm.get('carrierLoadTenderId') as FormControl).value;
    let fromDate = (this.CarrierLoadTenderForm.get('fromDate') as FormControl).value;
    let toDate = (this.CarrierLoadTenderForm.get('toDate') as FormControl).value;
    let shipmentId = (this.CarrierLoadTenderForm.get('shipmentId') as FormControl).value;
    let shipmentShipperNo = (this.CarrierLoadTenderForm.get('shipmentShipperNo') as FormControl).value;
    let status = (this.CarrierLoadTenderForm.get('status') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';
    let customerName = this.Userapi.getTokenUserInfo()?.customerName || 'EMPTY';
    this.showSpinnerforSearch = true;

    if (((carrierLoadTenderId == '' || carrierLoadTenderId == null || carrierLoadTenderId == 0) && (fromDate == '' || fromDate == null) && (toDate == '' || toDate == undefined) && (shipmentId == '' || shipmentId == 'EMPTY') && (shipmentShipperNo == '' || shipmentShipperNo == 'EMPTY') && (status == '' || status == 'Select Status'))) {
      this.toast.info({ detail: "carrierLoadTenderId", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
    }

    if (carrierLoadTenderId == '') {
      carrierLoadTenderId = 0;
    }

    if (shipmentId == '') {
      shipmentId = 'EMPTY'
    }

    if (shipmentShipperNo == '') {
      shipmentShipperNo = 'EMPTY'
    }

    if (status == '') {
      status = 'Select Status'
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

    this.api.getCarrierLoadTender(carrierLoadTenderId, stringFromDate, stringToDate, shipmentId, shipmentShipperNo, status, customerName).subscribe({
      next: (res: any) => {
        this.listOfCarrier = res.carrierLoadTender;

        this.msg = res.message;
        this.code = res.code;

        if (this.listOfCarrier == null || this.listOfCarrier.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.carrierToDisplay = [];
          return;
        }

        this.carrierToDisplay = this.listOfCarrier.slice(0, 10);

        if (this.code === 200) {
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

  openEditDialog(element: any) {
    const dialogRef = this.dialog.open(UpdateStatusModalComponent, {
      width: '800px',
      disableClose: true,
      data: {
        element: element,
        isCLT: true
      }
    });

    dialogRef.afterClosed()

  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.carrierToDisplay = this.listOfCarrier.slice(startIndex, startIndex + event.pageSize);
  }

}
