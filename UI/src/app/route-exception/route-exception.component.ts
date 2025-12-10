import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { RouteLog } from '../models/models';
import { ApiService } from '../services/api.service';
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
import { RoutesService } from '../services/routes.service';
import { RouteLogService } from '../services/routelog.service';
import { FileContentViewerDialogComponent } from '../file-content-viewer-dialog/file-content-viewer-dialog.component';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { ViewChild } from '@angular/core';

@Component({
  selector: 'route-exception',
  templateUrl: './route-exception.component.html',
  styleUrls: ['./route-exception.component.scss'],
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
export class RouteExceptionComponent implements OnInit {
  mydate = environment.date;
  listOfRouteExceptions: RouteLog[] = [];
  routeExceptionsToDisplay: RouteLog[] = [];
  RouteLogForm: FormGroup;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  statusOptions = ['Select Status ', 'Active', 'In-Active',];
  routeOptions: any[] = [];
  loadingStates = new Map<number, boolean>();
  isAdminUser: boolean = false;
  dataSource = new MatTableDataSource<RouteLog>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  columns: string[] = [
    'name',
    'Message',
    'CreatedDate',
    'Status',
    'DownloadFile'
  ];

    constructor(private routeApi: RoutesService, private api: ApiService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, private routeLogApi: RouteLogService, private userApi: ApiService, public languageService: LanguageService) {
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.isAdminUser = ["ADMIN", "WRITER","READER"].includes(this.userApi.getTokenUserInfo()?.userType || '');

    this.RouteLogForm = this.fb.group({
      name: fb.control(''),
      message: fb.control(''),
      fromDate: new FormControl(formatDate(sevenDaysAgo, "yyyy-MM-dd", "en")),
      toDate: new FormControl(formatDate(today, "yyyy-MM-dd", "en")),
      status: fb.control('')
    });
  }

  ngOnInit(): void {

    if (!this.isAdminUser) {
      const editIndex = this.columns.indexOf('DownloadFile');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }
    this.getRouteName();
    this.getRouteExceptions();
  }

  getRouteName() {
    this.routeApi.getRouteName().subscribe({
      next: (res: any) => {
        this.routeOptions = res.routes;
        this.routeOptions.unshift({ name: 'Select Route Name' });
      },
      error: (error) => {
        console.error('Error fetching route names:', error);
      }
    });
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.routeExceptionsToDisplay = this.listOfRouteExceptions.slice(startIndex, startIndex + event.pageSize);
  }

  getStatusClass(status: string): string {
    if (status === 'Active') {
      return 'Active';
    } else if (status === 'Active') {
      return 'InActive';
    } else {
      return '';
    }
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }


  getRouteExceptions() {
    let routeName = (this.RouteLogForm.get('name') as FormControl).value;
    let fromDate = (this.RouteLogForm.get('fromDate') as FormControl).value;
    let toDate = (this.RouteLogForm.get('toDate') as FormControl).value;
    let message = (this.RouteLogForm.get('message') as FormControl).value;
    let status = (this.RouteLogForm.get('status') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';
    this.showSpinnerforSearch = true;
    let name = routeName.name;

    if (((fromDate == '' || fromDate == undefined) && (toDate == '' || toDate == undefined) && (message == '' || message == 'EMPTY') && (name == '' || name == 'EMPTY' || name == 'Select Route Name') && (status == '' || status == 'Select Status'))) {
      this.toast.info({ detail: "Name", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
    }

    if (name == '' || name == undefined || name == 'Select Route Name') {
      name = 'EMPTY'
    }

    if (message == '') {
      message = 'EMPTY'
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

    this.api.getRouteExceptions(name, message, stringFromDate, stringToDate, status).subscribe({
      next: (res: any) => {
        this.listOfRouteExceptions = res.routeExceptionsData;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfRouteExceptions == null || this.listOfRouteExceptions.length === 0) {
          this.showSpinnerforSearch = false;
          this.routeExceptionsToDisplay = [];

          return;
        }

        //this.routeExceptionsToDisplay = this.listOfRouteExceptions.slice(0, 10);

        this.dataSource.data = this.listOfRouteExceptions;  // set full list

        setTimeout(() => {
          this.dataSource.paginator = this.paginator;
        }, 0); // ensures paginator initializes

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

  viewFile(id: number) {
    let parsedData: any;
    let l_data: string = "";
    this.loadingStates.set(id, true);

    this.routeLogApi.getRouteLog(id).subscribe({
      next: (res: any) => {
        this.listOfRouteExceptions = res.routeLog;
        this.msg = res.message;
        this.code = res.code;

        this.showSpinnerforSearch = false;
        this.loadingStates.set(id, false);
        if (this.listOfRouteExceptions == null || this.listOfRouteExceptions.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
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

        l_data = res.routeLog[0].details;

        parsedData = l_data;

        if (parsedData)
        {
          this.dialog.open(FileContentViewerDialogComponent, {
            data: { content: parsedData, type: "txt", routeName: res.routeLog[0].name },
            width: '800px',
          });
        }
        else
          this.toast.error({ detail: "Info", summary: this.languageService.getTranslation('noDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.loadingStates.set(id, false);
      },
    });
  }
}
