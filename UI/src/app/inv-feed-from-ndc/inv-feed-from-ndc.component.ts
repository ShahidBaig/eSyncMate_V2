import { Component, OnInit } from '@angular/core';
import { InvFeedFromNDC, Map } from '../models/models';
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
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { InvFeedFromNDCService } from '../services/invFeedFromNDC.service';
import { InvFeedDialogComponent } from './inv-feed-dialog/inv-feed-dialog.component';
import { RouteDataDialogComponent } from '../routes/route-data-dialog/route-data-dialog.component';

@Component({
  selector: 'inv-feed-from-ndc',
  templateUrl: './inv-feed-from-ndc.component.html',
  styleUrls: ['./inv-feed-from-ndc.component.scss'],
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
    FormsModule,
    MatPaginatorModule,
    TranslateModule
  ],
})
export class InvFeedFromNDCComponent implements OnInit {


  listOfMaps: InvFeedFromNDC[] = [];
  mapsToDisplay: InvFeedFromNDC[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Item', 'ItemId', 'UPC', 'Created Date'];
  selectedOption: string = 'Select Item';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  showDataColumn: boolean = true;
  isAdminUser: boolean = false;

  columns: string[] = [
    'id',
    'NDCItemID',
    'SKU',
    'ItemID',
    'ProductName',
    'UnitPrice',
    'ManufacturerName',
    'UOM',
    'PrimaryCategoryName',
    'SecondaryCategoryName',
    'ETADate',
    'ETAQty',
    'CreatedDate',
    'actions'
  ];
  process850 = 'EDI Process 850';
  selectedFile: File | null = null;
  isButtonDisabled: boolean = false;
  constructor(private api: ApiService, private fb: FormBuilder, private toast: NgToastService,
    private dialog: MatDialog, public languageService: LanguageService, private Invapi: InvFeedFromNDCService,) {

    this.isAdminUser = ["ADMIN"].includes(this.api.getTokenUserInfo()?.userType || '');
  }

  ngOnInit(): void {
    if (this.selectedOption === 'Select Item') {
      this.getInvFeedFromNDC();
    }
  }
  get label(): string {
    return this.selectedOption === 'Select Item' ? 'Select Item' : this.selectedOption;
  }

  onSelectionChange() {
    this.searchValue = '';
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.mapsToDisplay = this.listOfMaps.slice(startIndex, startIndex + event.pageSize);
  }

  getInvFeedFromNDC() {
    this.showSpinnerforSearch = false;
    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Item') {
      this.searchValue = 'ALL';
    }

    if (this.selectedOption === 'Created Date' && this.startDate.toLocaleString().length > 10) {
      stringFromDate = this.getFormattedDate(this.startDate);
    }
    if (this.selectedOption === 'Created Date' && this.endDate.toLocaleString().length > 10) {
      stringToDate = this.getFormattedDate(this.endDate);
    }
    if (this.selectedOption === 'Created Date' && this.startDate.toLocaleString().length > 10 && this.endDate.toLocaleString().length > 10) {
      this.searchValue = stringFromDate + '/' + stringToDate;
    }

    this.Invapi.getInvFeedFromNDC(this.selectedOption, this.searchValue).subscribe({
      next: (res: any) => {
        this.listOfMaps = res.inv;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfMaps == null || this.listOfMaps.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.mapsToDisplay = [];

          return;
        }

        this.mapsToDisplay = this.listOfMaps.slice(0, 10);

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

  getRoutes() {

  }

  openRouteLogDialog(id: any, name: any): void {
    console.log("idddd", id);
    const dialogRef = this.dialog.open(InvFeedDialogComponent, {
      width: '900px',
      disableClose: true,
      data: { id: id, name: name }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('Dialog closed with result:', result);
    });
  }

  openRouteDataDialog(id: any, name: any): void {
    const dialogRef = this.dialog.open(RouteDataDialogComponent, {
      width: '900px',
      disableClose: true,
      data: { id: id, name: name }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('Dialog closed with result:', result);
    });
  }

  showRouteLog(connectorData: any) {
    this.openRouteLogDialog(connectorData.itemID, connectorData.name);
  }

  showRouteData(connectorData: any) {

    this.openRouteDataDialog(connectorData.id, connectorData.name);
  }

  onFileSelected(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    if (inputElement.files) {
      this.selectedFile = inputElement.files[0];
    }
  }

  clearFile() {
    const input = document.querySelector('.file-input') as HTMLInputElement;
    if (input) {
      input.value = '';
      this.selectedFile = null;
    }
    this.showSpinner = false;
    this.isButtonDisabled = false;
  }

  uploadFile() {
    if (this.selectedFile == null) {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('choosefileWarning'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      return;
    }

    if (this.selectedFile) {
      this.isButtonDisabled = true;
      this.showSpinner = true;

      this.Invapi.uploadFile(this.selectedFile).subscribe(
        {
          next: (res: any) => {
            this.msg = res.message;
            this.code = res.code;
            if (this.code === 200) {
              this.toast.success({ detail: "SUCCESS", summary: this.languageService.getTranslation('file') + ': [ ' + this.selectedFile?.name + ' ] ' + this.languageService.getTranslation('successfully'), duration: 5000, sticky: true, position: 'topRight' });
            }
            else if (this.code === 400) {
              this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
            } else {
              this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
            }

            this.clearFile();
            this.getInvFeedFromNDC();
          },
          error: (err: any) => {
            this.toast.error({ detail: "ERROR", summary: err, duration: 5000, sticky: true, position: 'topRight' });
            this.isButtonDisabled = false;
            this.showSpinner = false;
          }
        });
    }
  }

}
