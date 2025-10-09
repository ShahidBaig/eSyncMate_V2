import { Component, Inject, OnInit } from '@angular/core';
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
import { RouteData } from '../../models/models';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import * as moment from 'moment';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'route-data-dialog',
  templateUrl: './route-data-dialog.component.html',
  styleUrls: ['./route-data-dialog.component.scss'],
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
    TranslateModule,
    MatPaginatorModule,
  ],
})
export class RouteDataDialogComponent implements OnInit {
  mydate = environment.date;
  displayedColumns: string[] = ['CreatedDate', 'Type', 'fileName','DownloadFile']; // Add the actual column names from your data
  dataSource = this.data.historyData;
  currentRouteId?: string;
  currentRouteName?: string;
  showSpinner: boolean = false;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  loadingStates = new Map<number, boolean>();
  RouteDataForm: FormGroup;
  listOfRouteData: RouteData[] = [];
  constructor(

    public dialogRef: MatDialogRef<RouteDataDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any, private dialog: MatDialog, private api: RouteDataService, private fb: FormBuilder, private toast: NgToastService, public languageService: LanguageService) {
    this.currentRouteId = data.id;
    this.currentRouteName = data.name;
   
    const threeDaysAgo = new Date();
    threeDaysAgo.setDate(threeDaysAgo.getDate() - 3);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.RouteDataForm = this.fb.group({
      fromDate: new FormControl(formatDate(threeDaysAgo, "yyyy-MM-dd", "en")),
      toDate: new FormControl(formatDate(today, "yyyy-MM-dd", "en")),
      type: fb.control('')
    });
  }
  ngOnInit(): void {
    //this.getSearchRouteData();
  }

  onCancel() {
    this.dialogRef.close();
  }

  downloadFile(id: number, fileName: string) {
    this.showSpinner = true;
    let l_data: string = "";

    this.api.getDataRoute(id).subscribe({
      next: (res: any) => {
        this.listOfRouteData = res.routeData;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfRouteData == null || this.listOfRouteData.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;

          return;
        }

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

        l_data = res.routeData[0].data;

        const blob = new Blob([l_data], { type: 'text/plain' });
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = fileName;
        link.click();

        this.showSpinner = false;

        this.showSpinnerforSearch = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinnerforSearch = false;
      },
    });
  }

  

  isLoading(fileId: number): boolean {
    // Retrieve loading state for a specific fileId
    return this.loadingStates.get(fileId) || false;
  }

  viewFile(id: number, fileName: any) {
    const fileExtension = fileName.split('.').pop().toLowerCase();
    let parsedData:any;
    let l_data: string = "";
    this.loadingStates.set(id, true);

    this.api.getDataRoute(id).subscribe({
      next: (res: any) => {
        this.listOfRouteData = res.routeData;
        this.msg = res.message;
        this.code = res.code;

        this.showSpinnerforSearch = false;
        this.loadingStates.set(id, false);
        if (this.listOfRouteData == null || this.listOfRouteData.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates.set(id, false);

          return;
        }

        if (this.code === 200) {
          //this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.loadingStates.set(id, false);
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates.set(id, false);
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates.set(id, false);
        }

        l_data = res.routeData[0].data;

        if (fileExtension === 'json') {
          try {
            parsedData = JSON.parse(l_data);
          } catch (e) {
            console.error('Error parsing JSON', e);
          }
        } else if (fileExtension === 'edi') {
          l_data = l_data.replace(/~/g, '\n');
          parsedData = l_data;
        } else {
          parsedData = l_data;
        }

        this.dialog.open(FileContentViewerDialogComponent, {
          data: { content: parsedData, type: fileExtension },
          width: '800px',
        });

        this.loadingStates.set(id, false);
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.loadingStates.set(id, false);
      },
    });
  }

  getSearchRouteData() {
    let fromDate = (this.RouteDataForm.get('fromDate') as FormControl).value;
    let toDate = (this.RouteDataForm.get('toDate') as FormControl).value;
    let type = (this.RouteDataForm.get('type') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';
    this.showSpinnerforSearch = true;
    let routeID: any = this.currentRouteId;

    if (((fromDate == '' || fromDate == null) && (toDate == '' || toDate == undefined) && (type == '' || type == 'Select Status'))) {
      this.toast.info({ detail: "orderId", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
    }

    if (type == '') {
      type = 'Select type'
    }

    if (fromDate !== null) {
      stringFromDate = this.getFormattedDate(fromDate);

      if (stringFromDate.length > 10) {
        stringFromDate = this.getFormattedDate(fromDate);
      }
    } else {
      stringFromDate = '1999-01-01';
    }

    if (toDate !== null) {
      stringToDate = this.getFormattedDate(toDate);

      if (stringToDate.length > 10) {
        stringToDate = this.getFormattedDate(toDate);
      }
    } else {
      stringToDate = '1999-01-01';
    }

    this.api.getSearchRouteData(routeID, stringFromDate, stringToDate, type).subscribe({
      next: (res: any) => {
        this.listOfRouteData = res.routeDatatable;
        this.msg = res.message;
        this.code = res.code;

        this.dataSource = this.listOfRouteData.slice(0, 10);

        if (this.listOfRouteData == null || this.listOfRouteData.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;

          return;
        }

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

  getFormattedDate(date: any) {
    // let year = date.getFullYear();
    // let month = (1 + date.getMonth()).toString().padStart(2, '0');
    // let day = date.getDate().toString().padStart(2, '0');

    // return year + '-' + month + '-' + day;
    return moment(date).format('YYYY-MM-DD');
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.dataSource = this.listOfRouteData.slice(startIndex, startIndex + event.pageSize);
  }
}
