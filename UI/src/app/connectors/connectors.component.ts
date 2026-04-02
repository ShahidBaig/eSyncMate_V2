import { Component, OnInit } from '@angular/core';
import { Connector } from '../models/models';
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
import { PopupComponent } from '../popup/popup.component';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { AddConnectorDialogComponent } from './add-connector-dialog/add-connector-dialog.component';
import { EditConnectorDialogComponent } from './edit-connector-dialog/edit-connector-dialog.component';
import { ConnectorsService } from '../services/connectors.service';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core'; 
import { ApiService } from '../services/api.service';

@Component({
  selector: 'connectors',
  templateUrl: './connectors.component.html',
  styleUrls: ['./connectors.component.scss'],
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
    MatPaginatorModule,
    TranslateModule
  ],
})
export class ConnectorsComponent implements OnInit {
  isLoading: boolean = false;
  listOfConnectors: Connector[] = [];
  connectorsToDisplay: Connector[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Connector', 'Id', 'Connector Name', 'Connector Type', 'Created Date'];
  selectedOption: string = 'Select Connector';
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
    'ConnectorType',
    'CreatedDate',
    'Data',
    'Edit',
  ];

    constructor(private ConnectorsApi: ConnectorsService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, private api: ApiService, public languageService: LanguageService) {
    const permissions = this.api.getMenuPermissions('edi/connectors');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
    } else {
      const isAdmin = ["ADMIN", "WRITER"].includes(this.api.getTokenUserInfo()?.userType || '');
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

    if (this.selectedOption === 'Select Connector') {
      this.getConnectors(true);
    }
  }

  openAddCustomerDialog(): void {
    const dialogRef = this.dialog.open(AddConnectorDialogComponent, {
      width: '800px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.getConnectors(false);
      }
    });
  }

  openEditDialog(connectorData: any) {
    const dialogRef = this.dialog.open(EditConnectorDialogComponent, {
      width: '800px',
      data: connectorData,
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getConnectors(false);
      }
    });
  }

  onPageChange(event: PageEvent) {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.getConnectors();
  }

  get label(): string {
    return this.selectedOption === 'Select Connector' ? 'Select Connector' : this.selectedOption;
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

  getConnectors(resetPage: boolean = false) {
    if (resetPage) {
      this.pageNumber = 1;
    }

    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Connector') {
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
    this.ConnectorsApi.getConnectors(this.selectedOption, this.searchValue, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        this.listOfConnectors = res.connectors ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.connectorsToDisplay = this.listOfConnectors;

        if (this.listOfConnectors.length === 0 && this.pageNumber === 1) {
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
