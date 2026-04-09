import { Component } from '@angular/core';
import { DatePipe, NgIf } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormBuilder, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { RouteTypesHelpDialogComponent } from './route-types-help-dialog/route-types-help-dialog.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { RouteTypesService } from '../services/route-types.service';
import { RouteType } from '../models/models';
import { AddRouteTypesDialogComponent } from './add-route-types-dialog/add-route-types-dialog.component';
import { EditRouteTypesDialogComponent } from './edit-route-types-dialog/edit-route-types-dialog.component';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

import { HttpClientModule, HttpClient } from '@angular/common/http';
import { ApiService } from '../services/api.service';


@Component({
  selector: 'route-types',
  templateUrl: './route-types.component.html',
  styleUrls: ['./route-types.component.scss'],
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
    FormsModule,
    HttpClientModule,
    MatPaginatorModule,
    TranslateModule
  ],
})
export class RouteTypesComponent {
  isLoading: boolean = false;
  listOfRouteType: RouteType[] = [];
  RouteTypeToDisplay: RouteType[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Route Types', 'Id', 'Route Types Name', 'Description', 'Created Date'];
  selectedOption: string = 'Select Route Types';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  showDataColumn: boolean = true;
  isAdminUser: boolean = false;
  canAdd = false;
  canEdit = false;
  canDelete = false;
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;

  columns: string[] = [
    'id',
    'Name',
    'Description',
    'CreatedDate',
    'Edit',
  ];

    constructor(private api: RouteTypesService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, private userApi: ApiService, public languageService: LanguageService) {
    const permissions = this.userApi.getMenuPermissions('edi/routeTypes');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
    } else {
      const isAdmin = ["ADMIN", "WRITER"].includes(this.userApi.getTokenUserInfo()?.userType || '');
      this.canAdd = isAdmin;
      this.canEdit = isAdmin;
      this.canDelete = isAdmin;
      this.isAdminUser = isAdmin;
    }
  }

  ngOnInit(): void {
    if (!this.canEdit) {
      const editIndex = this.columns.indexOf('Edit');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }

    if (this.selectedOption === 'Select Route Types') {
      this.getRouteTypes();
    }
  }

  openHelp(): void {
    this.dialog.open(RouteTypesHelpDialogComponent, { width: '90%', maxWidth: '1200px', maxHeight: '90vh' });
  }

  openAddRouteTypeDialog(): void {
    const dialogRef = this.dialog.open(AddRouteTypesDialogComponent, {
      width: '800px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.getRouteTypes();
      }
    });
  }

  openEditDialog(connectorData: any) {
    const dialogRef = this.dialog.open(EditRouteTypesDialogComponent, {
      width: '800px',
      disableClose: true,
      data: connectorData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getRouteTypes();
      }
    });
  }

  get label(): string {
    return this.selectedOption === 'Select Route Types' ? 'Select Route Types' : this.selectedOption;
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
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.getRouteTypes();
  }

  getRouteTypes(resetPage: boolean = false) {
    if (resetPage) {
      this.pageNumber = 1;
    }

    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Route Types') {
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

    this.isLoading = true;
    this.api.getRouteTypes(this.selectedOption, this.searchValue, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.listOfRouteType = res.routeType ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.RouteTypeToDisplay = this.listOfRouteType;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfRouteType.length === 0 && this.pageNumber === 1) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, position: 'topRight' });
        }

        if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, position: 'topRight' });
        }

        this.isLoading = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.isLoading = false;
      },
    });
  }
}
