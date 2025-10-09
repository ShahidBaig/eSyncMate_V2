import { Component} from '@angular/core';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';
import { CarrierLoadTenderService } from '../services/carrierLoadTender.service';
import { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { CustomersService } from '../services/customers.service';
import { CsvExportServiceService } from '../services/csv-export-service.service';
import {EdiCountFile} from '../models/models';
import {HeaderMapping}from '../models/models';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'edi-file-counter',
  templateUrl: './edi-file-counter.component.html',
  styleUrls: ['./edi-file-counter.component.scss'],
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
    TranslateModule,
    FormsModule 
  ],
})
export class EdiFileCounterComponent {
  mydate = environment.date;

  ediCountFilesList: EdiCountFile[] = [];
  ediDataToDisplay: any[] = [];
  customersList: any[] = [];
  EDIFilesCountForm: FormGroup;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  total204Count = 0;
  total214Count = 0;
  shipmentID: string = '';
  shipperNo: string = '';
 


  columns: string[] = [
    'CustomerName',
    'DocumentDate',
    'EDI204',
    'EDI214',
    'ShipmentId',
    'ShipperNo'
  ];

  constructor(private api: CarrierLoadTenderService, private fb: FormBuilder, private toast: NgToastService,
      public languageService: LanguageService, private customerService: CustomersService, private csvExportService: CsvExportServiceService) {
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.EDIFilesCountForm = this.fb.group({
      fromDate: new FormControl(formatDate(sevenDaysAgo, "yyyy-MM-dd", "en")),
      toDate: new FormControl(formatDate(today, "yyyy-MM-dd", "en")),
      customer: fb.control(''),
      shipmentID: fb.control(''),
      shipperNo: fb.control('')
    });
  }

  ngOnInit(): void {
    this.getCustomers();
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  getCustomers(){
    this.customerService.getCustomersList().subscribe({
      next: (res: any) => {
        if (res.customersList)
        this.customersList = res.customersList;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
      },
    });
  }

  exportData(){
    const headers: HeaderMapping = {
      customerName: 'Customer',
      docDate: 'Document Date',
      counT_204: 'EDI204',
      counT_214: 'EDI214'
    };
    let filename = this.ediCountFilesList[0].customerName + '_edi.csv';
    this.csvExportService.exportToCsv(filename, this.ediCountFilesList, headers,['counT_204', 'counT_214']);
  }

  getEdiFilesCount() {
    let fromDate = (this.EDIFilesCountForm.get('fromDate') as FormControl).value;
    let toDate = (this.EDIFilesCountForm.get('toDate') as FormControl).value;
    let customer = (this.EDIFilesCountForm.get('customer') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';
    this.showSpinnerforSearch = true;
    let shipmentID = (this.EDIFilesCountForm.get('shipmentID') as FormControl).value;
    let shipperNo = (this.EDIFilesCountForm.get('shipperNo') as FormControl).value;

    if (shipmentID == '') {
      shipmentID = 'EMPTY'
    }

    if (shipperNo == '') {
      shipperNo = 'EMPTY'
    }

    if (((fromDate == '' || fromDate == null) || (toDate == '' || toDate == undefined) || (customer == '' || customer == 'Select Customer'))) {
      this.toast.info({ detail: "EDI Files Count", summary: this.languageService.getTranslation('provideFilters'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
    }

    if (shipperNo != "EMPTY" && shipmentID == "EMPTY")
    {
      this.toast.info({ detail: "EDI Files Count", summary: this.languageService.getTranslation('searchfilter'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
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
    this.ediCountFilesList= [];
    this.ediDataToDisplay = [];
    this.total204Count = 0;
    this.total204Count = 0;

    this.api.getEdiFilesCounter(stringFromDate, stringToDate, customer, shipmentID, shipperNo).subscribe({
      next: (res: any) => {
        if (res && res.ediCounterData)
              this.ediCountFilesList = res.ediCounterData;

        this.msg = res.message;
        this.code = res.code;

        if (this.ediCountFilesList == null || this.ediCountFilesList.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
          return;
        }

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

        this.ediDataToDisplay = this.ediCountFilesList.slice(0, 10);
        this.total204Count = this.ediCountFilesList.reduce((acc, curr) => acc + curr.counT_204, 0);
        this.total214Count = this.ediCountFilesList.reduce((acc, curr) => acc + curr.counT_214, 0);
        this.showSpinnerforSearch = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinnerforSearch = false;
        this.ediCountFilesList= [];
        this.ediDataToDisplay = [];
      },
    });
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.ediDataToDisplay = this.ediCountFilesList.slice(startIndex, startIndex + event.pageSize);
  }

}

