import { Component, OnInit } from '@angular/core';
import { Routes, UserType } from '../models/models';
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
import { AddRoutesDialogComponent } from './add-routes-dialog/add-routes-dialog.component';
import { EditRoutesDialogComponent } from './edit-routes-dialog/edit-routes-dialog.component';
import { RoutesService } from '../services/routes.service';
import { RouteLogDialogComponent } from './route-log-dialog/route-log-dialog.component';
import { RouteDataDialogComponent } from './route-data-dialog/route-data-dialog.component';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { InventoryService } from '../services/inventory.service';
interface Customers {
  erpCustomerID: string;
}

@Component({
  selector: 'routes',
  templateUrl: './routes.component.html',
  styleUrls: ['./routes.component.scss'],
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
    RouteDataDialogComponent,
    TranslateModule
  ],
})
export class RoutesComponent {
  isLoading: boolean = false;
  listOfRoutes: Routes[] = [];
  routesToDisplay: Routes[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  //options = ['Select Route', 'Id', 'Name', 'Source Party', 'Destination Party', 'Source Connector', 'Destination Connector', 'Party Group', 'Route Type', 'Created Date'];
  options = ['Select Route', 'Id', 'Name',  'Created Date','Route Group', 'ERP CustomerID'];
  selectedOption: string = 'Select Route';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  showDataColumn: boolean = true;
  isAdminUser: boolean = false;
  canAdd = false;
  canEdit = false;
  canDelete = false;
  customersOptions: Customers[] | undefined;
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;


  columns: string[] = [
    'id',
    'ERPCustomerID',
    'Name',
    'Status',
    'RouteGroup',
    'SourceParty',
    'DestinationParty',
    'PartnerGroup',
    'CreatedDate',
    'Edit'
  ];

  constructor(private api: RoutesService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, public token: ApiService,
    public languageService: LanguageService, private Userapi: ApiService, private inventoryServie: InventoryService) {
  }

  ngOnInit(): void {
    this.selectedOption = 'Select Route'; // Set default selected option
    this.getRoutes(true);
    this.getERPCustomer();
    const permissions = this.token.getMenuPermissions('edi/routes');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
    } else {
      const isAdmin = ["ADMIN", "WRITER"].includes(this.token.getTokenUserInfo()?.userType || '');
      this.canAdd = isAdmin;
      this.canEdit = isAdmin;
      this.canDelete = isAdmin;
      this.isAdminUser = isAdmin;
    }

    if (!this.canEdit) {
      const editIndex = this.columns.indexOf('Edit');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }
    if (this.selectedOption === 'Select Route') {
      this.getRoutes(true);
    }
  }

  onPageChange(event: PageEvent) {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.getRoutes();
  }

  openAddRouteDialog(): void {
    const dialogRef = this.dialog.open(AddRoutesDialogComponent, {
      width: '900px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.getRoutes(false);
      }
    });
  }

  openEditDialog(connectorData: any) {
    const dialogRef = this.dialog.open(EditRoutesDialogComponent, {
      width: '900px',
      disableClose: true,
      data: connectorData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getRoutes(false);
      }
    });
  }

  get label(): string {
    return this.selectedOption === 'Select Route' ? 'Select Route' : this.selectedOption;
  }

  onSelectionChange() {
    this.searchValue = '';
  }

  getERPCustomer() {
    this.inventoryServie.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customersOptions = res.customers;
      },
    });
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  getRoutes(resetPage: boolean = false) {
    if (resetPage) {
      this.pageNumber = 1;
    }

    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Route') {
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
    this.api.getRoutes(this.selectedOption, this.searchValue, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        this.listOfRoutes = res.routes ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.routesToDisplay = this.listOfRoutes;

        if (this.listOfRoutes.length === 0 && this.pageNumber === 1) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, position: 'topRight' });
        }

        this.isLoading = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.isLoading = false;
      },
    });
  }

  openRouteLogDialog(id:any,name:any): void {
    const dialogRef = this.dialog.open(RouteLogDialogComponent, {
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
      data: { id: id, name: name }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('Dialog closed with result:', result);
    });
  }


  showRouteLog(connectorData: any) {
    this.openRouteLogDialog(connectorData.id, connectorData.name);
  }

  showRouteData(connectorData: any) {

    this.openRouteDataDialog(connectorData.id, connectorData.name);
  }

  private showInfoToast(message: string): void {
    this.toast.info({
      detail: message,
      summary: 'INFO',
      duration: 5000,
      position: 'topRight'
    });
  }

  private showErrorToast(message: string): void {
    this.toast.error({
      detail: message,
      summary: 'ERROR',
      duration: 5000,
      sticky: true,
      position: 'topRight'
    });
  }
}
