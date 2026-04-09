import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { RouteLog } from '../models/models';
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
import { MatProgressBarModule } from '@angular/material/progress-bar';
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
    MatProgressBarModule,
    CommonModule,
    MatSelectModule,
    MatPaginatorModule,
    TranslateModule,
    FormsModule
  ],
})
export class RouteExceptionComponent implements OnInit {
  isLoadingData: boolean = false;
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
  filteredRouteOptions: any[] = [];
  routeSearchText = '';
  loadingStates: { [key: number]: boolean } = {};
  isAdminUser: boolean = false;
  canAdd = false;
  canEdit = false;
  canDelete = false;
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;

  columns: string[] = [
    'name',
    'Message',
    'CreatedDate',
    'DownloadFile'
  ];

    constructor(private routeApi: RoutesService, private api: ApiService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, private routeLogApi: RouteLogService, private userApi: ApiService, public languageService: LanguageService) {
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    const permissions = this.userApi.getMenuPermissions('edi/routeExceptions');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
    } else {
      const isAdmin = ["ADMIN", "WRITER", "READER"].includes(this.userApi.getTokenUserInfo()?.userType || '');
      this.canAdd = isAdmin;
      this.canEdit = isAdmin;
      this.canDelete = isAdmin;
      this.isAdminUser = isAdmin;
    }

    this.RouteLogForm = this.fb.group({
      name: fb.control(''),
      message: fb.control(''),
      fromDate: new FormControl(formatDate(sevenDaysAgo, "yyyy-MM-dd", "en")),
      toDate: new FormControl(formatDate(today, "yyyy-MM-dd", "en")),
      status: fb.control('')
    });
  }

  ngOnInit(): void {

    if (!this.canEdit) {
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
        this.filteredRouteOptions = this.routeOptions;
      },
      error: (error) => {
        console.error('Error fetching route names:', error);
      }
    });
  }

  filterRouteOptions() {
    const search = this.routeSearchText.toLowerCase();
    this.filteredRouteOptions = (this.routeOptions || []).filter(r =>
      r.name.toLowerCase().includes(search)
    );
  }

  onRouteSelectOpened(opened: boolean) {
    if (opened) {
      this.routeSearchText = '';
      this.filteredRouteOptions = this.routeOptions || [];
    }
  }

  onPageChange(event: PageEvent) {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.getRouteExceptions();
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


  getRouteExceptions(resetPage: boolean = false) {
    if (resetPage) {
      this.pageNumber = 1;
    }

    let routeName = (this.RouteLogForm.get('name') as FormControl).value;
    let fromDate = (this.RouteLogForm.get('fromDate') as FormControl).value;
    let toDate = (this.RouteLogForm.get('toDate') as FormControl).value;
    let message = (this.RouteLogForm.get('message') as FormControl).value;
    let status = (this.RouteLogForm.get('status') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';
    let name = routeName.name;

    if (((fromDate == '' || fromDate == undefined) && (toDate == '' || toDate == undefined) && (message == '' || message == 'EMPTY') && (name == '' || name == 'EMPTY' || name == 'Select Route Name') && (status == '' || status == 'Select Status'))) {
      this.toast.info({ detail: "Name", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, position: 'topRight' });
      return;
    }

    if (name == '' || name == undefined || name == 'Select Route Name') { name = 'EMPTY' }
    if (message == '') { message = 'EMPTY' }
    if (status == '') { status = 'Select Status' }

    if (fromDate !== null) {
      stringFromDate = fromDate.toLocaleString();
      if (stringFromDate.length > 10) { stringFromDate = this.getFormattedDate(fromDate); }
    } else {
      stringFromDate = '1999-01-01';
    }

    if (toDate !== null) {
      stringToDate = toDate.toLocaleString();
      if (stringToDate.length > 10) { stringToDate = this.getFormattedDate(toDate); }
    } else {
      stringToDate = '1999-01-01';
    }

    this.isLoadingData = true;
    this.api.getRouteExceptions(name, message, stringFromDate, stringToDate, status, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.listOfRouteExceptions = res.routeExceptionsData ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.routeExceptionsToDisplay = this.listOfRouteExceptions;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfRouteExceptions.length === 0 && this.pageNumber === 1) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, position: 'topRight' });
        }

        this.isLoadingData = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.isLoadingData = false;
      },
    });
  }

  isLoading(fileId: number): boolean {
    return this.loadingStates[fileId] || false;
  }

  viewFile(id: number) {
    let parsedData: any;
    let l_data: string = "";
    this.loadingStates[id] = true;

    this.routeLogApi.getRouteLog(id).subscribe({
      next: (res: any) => {
        let routeLogData = res.routeLog;
        this.msg = res.message;
        this.code = res.code;

        this.showSpinnerforSearch = false;
        this.loadingStates[id] = false;
        if (routeLogData == null || routeLogData.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates[id] = false;

          return;
        }

        if (this.code === 200) {
          //this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.loadingStates[id] = false;
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates[id] = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates[id] = false;
        }

        l_data = routeLogData[0].details;

        parsedData = l_data;

        if (parsedData)
        {
          this.dialog.open(FileContentViewerDialogComponent, {
            data: { content: parsedData, type: "txt", routeName: routeLogData[0].name },
            width: '800px',
          });
        }
        else
          this.toast.error({ detail: "Info", summary: this.languageService.getTranslation('noDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.loadingStates[id] = false;
      },
    });
  }
}
