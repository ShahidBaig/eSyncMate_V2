import { Component, OnInit } from '@angular/core';
import { AlertConfiguration, Connector } from '../models/models';
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
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
//import { AddConnectorDialogComponent } from './add-connector-dialog/add-connector-dialog.component';
//import { EditConnectorDialogComponent } from './edit-connector-dialog/edit-connector-dialog.component';
import { ConnectorsService } from '../services/connectors.service';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { ApiService } from '../services/api.service';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { ViewChild } from '@angular/core';
import { alertsConfigurationService } from '../services/alertsConfiguration.service';
import { AddAlertConfigurationDialogComponent } from './add-alert-configuration-dialog/add-alert-configuration-dialog.component';
import { EditAlertConfigurationComponent } from './edit-alert-configuration/edit-alert-configuration.component';
interface Customers {
  erpCustomerID: string;
}

@Component({
  selector: 'alert-configuration',
  templateUrl: './alert-configuration.component.html',
  styleUrls: ['./alert-configuration.component.scss'],
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
export class AlertConfigurationComponent implements OnInit {
  listOfConnectors: AlertConfiguration[] = [];
  connectorsToDisplay: AlertConfiguration[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Connector', 'AlertID', 'Alert Name', 'Customer ID', 'Created Date'];
  selectedOption: string = 'Select Alerts Configuration';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  showDataColumn: boolean = true;
  isAdminUser: boolean = false;
  dataSource = new MatTableDataSource<AlertConfiguration>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  customersOptions: Customers[] | undefined;

  columns: string[] = [
    'AlertID',
    'AlertName',
    //'CustomerID',
    'CreatedDate',
    'Edit',
  ];

  constructor(private alertsConfigurationApi: alertsConfigurationService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, private api: ApiService, public languageService: LanguageService) {
    this.isAdminUser = ["ADMIN"].includes(this.api.getTokenUserInfo()?.userType || '');
  }

  ngOnInit(): void {
    if (!this.isAdminUser) {
      const editIndex = this.columns.indexOf('Edit');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
      
    }

    if (this.selectedOption === 'Select Alerts Configuration') {
      this.getAlertsConfiguration();
    }
    this.getERPCustomer();
  }

  getERPCustomer() {
    this.alertsConfigurationApi.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customersOptions = res.customers;
      },
    });
  }

  openAddCustomerDialog(): void {
    const dialogRef = this.dialog.open(AddAlertConfigurationDialogComponent, {
      width: '800px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.getAlertsConfiguration();
      }
    });
  }

  openEditDialog(connectorData: any) {
    const dialogRef = this.dialog.open(EditAlertConfigurationComponent, {
      width: '800px',
      data: connectorData,
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getAlertsConfiguration();
      }
    });
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.connectorsToDisplay = this.listOfConnectors.slice(startIndex, startIndex + event.pageSize);
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

  getAlertsConfiguration() {
    this.showSpinnerforSearch = false;
    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Alerts Configuration') {
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

    this.alertsConfigurationApi.getAlertsConfiguration(this.selectedOption, this.searchValue).subscribe({
      next: (res: any) => {
        this.listOfConnectors = res.alertsConfiguration;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfConnectors == null || this.listOfConnectors.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.connectorsToDisplay = [];
          return;
        }

        this.dataSource.data = this.listOfConnectors;  // set full list

        setTimeout(() => {
          this.dataSource.paginator = this.paginator;
        }, 0); // ensures paginator initializes

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
}
