import { Component, OnInit, ViewChild } from '@angular/core';
import { ApiService } from '../services/api.service';
import { DatePipe, NgIf, CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageService } from '../services/language.service';
import { FlowsService } from '../services/flows.service';
import { CustomerProductCatalogService } from '../services/customerProductCatalogDialog.service';
import { AddFlowDialogComponent } from './add-flow-dialog/add-flow-dialog.component';
import { EditFlowDialogComponent } from './edit-flow-dialog/edit-flow-dialog.component';

export interface Flows {
  id: number;
  customerID: string;
  title: string;
  description: string;
  status: string;
  createdDate: string;
  flowDetails: any[];
}

interface Customers {
  erpCustomerID: string;
  name: string;
  id: any;
}

@Component({
  selector: 'app-flows',
  templateUrl: './flows.component.html',
  styleUrls: ['./flows.component.scss'],
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
export class FlowsComponent implements OnInit {
  listOfFlows: Flows[] = [];
  flowsToDisplay: Flows[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  selectedCustomer: string = 'EMPTY';
  showDataColumn: boolean = true;
  isAdminUser: boolean = false;
  customerOptions: Customers[] | undefined;

  dataSource = new MatTableDataSource<Flows>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  columns: string[] = [
    'id',
    'CustomerID',
    'Title',
    'Description',
    'Status',
    'CreatedDate',
    'Edit'
  ];

  constructor(
    private flowsApi: FlowsService,
    private toast: NgToastService,
    private dialog: MatDialog,
    public token: ApiService,
    public languageService: LanguageService,
    private ERPApi: CustomerProductCatalogService
  ) { }

  ngOnInit(): void {
    this.getFlows(true);
    this.getERPCustomer();
    this.isAdminUser = ["ADMIN", "WRITER"].includes(this.token.getTokenUserInfo()?.userType || '');

    if (!this.isAdminUser) {
      const editIndex = this.columns.indexOf('Edit');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
  }

  openAddFlowDialog(): void {
    const dialogRef = this.dialog.open(AddFlowDialogComponent, {
      width: '1100px',
      disableClose: true
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.getFlows(false);
      }
    });
  }

  openEditDialog(flowData: any) {
    const dialogRef = this.dialog.open(EditFlowDialogComponent, {
      width: '1100px',
      disableClose: true,
      data: flowData
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getFlows(false);
      }
    });
  }

  getERPCustomer() {
    this.ERPApi.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customerOptions = res.customers;
      },
    });
  }

  getFlows(resetPage: boolean = false) {
    this.showSpinnerforSearch = true;

    let searchOption = 'Customer ID';
    let searchValue = this.selectedCustomer;
    if (searchValue === '' || searchValue === 'EMPTY') {
      searchOption = '';
      searchValue = '';
    }

    this.flowsApi.getFlows(searchOption, searchValue).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        const oldPageIndex = this.paginator?.pageIndex ?? 0;
        const oldPageSize = this.paginator?.pageSize ?? 10;

        this.listOfFlows = res.flows ?? [];

        if (this.listOfFlows == null || this.listOfFlows.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.flowsToDisplay = [];
          this.dataSource.data = [];
          return;
        }

        this.dataSource.data = this.listOfFlows;

        if (resetPage) {
          this.paginator?.firstPage();
        } else {
          const maxPageIndex = Math.max(Math.ceil(this.listOfFlows.length / oldPageSize) - 1, 0);
          this.paginator.pageIndex = Math.min(oldPageIndex, maxPageIndex);
          this.paginator._changePageSize(this.paginator.pageSize);
        }

        this.showSpinnerforSearch = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.showSpinnerforSearch = false;
      },
    });
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.flowsToDisplay = this.listOfFlows.slice(startIndex, startIndex + event.pageSize);
  }

}
