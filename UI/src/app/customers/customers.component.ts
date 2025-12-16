import { Component, OnInit } from '@angular/core';
import { Customer } from '../models/models';
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
import { AddCustomerDialogComponent } from './add-customer-dialog/add-customer-dialog.component';
import { CustomersService } from '../services/customers.service';
import { EditCustomerPopupComponent } from './edit-customer-popup/edit-customer-popup.component';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { ApiService } from '../services/api.service';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { ViewChild } from '@angular/core';


@Component({
  selector: 'customers',
  templateUrl: './customers.component.html',
  styleUrls: ['./customers.component.scss'],
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
export class CustomersComponent implements OnInit {
  listOfCustomers: Customer[] = [];
  customersToDisplay: Customer[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Customer', 'Id', 'Customer Name', 'ERP Customer ID', 'ISA Customer ID', 'ISA 810 Receiver ID', 'Market Place', 'Created Date'];
  selectedOption: string = 'Select Customer';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  isAdminUser: boolean = false;
  dataSource = new MatTableDataSource<Customer>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  columns: string[] = [
    'id',
    'Name',
    'ERPCustomerID',
    'ISACustomerID',
    'ISA810ReceiverId',
    'ISA856ReceiverId',
    'Marketplace',
    'CreatedDate',
    'Edit',
  ];

  constructor(private customersApi: CustomersService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, private Userapi: ApiService, public languageService: LanguageService) {
    this.isAdminUser = ["ADMIN"].includes(this.Userapi.getTokenUserInfo()?.userType || '');
  }

  ngOnInit(): void {

    if (!this.isAdminUser) {
      const editIndex = this.columns.indexOf('Edit');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }

    if (this.selectedOption === 'Select Customer') {
      this.getCustomers(true);

    }
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
  }

  openAddCustomerDialog(): void {
    const dialogRef = this.dialog.open(AddCustomerDialogComponent, {
      width: '800px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.getCustomers(false);
      }
    });
  }

  openEditDialog(customerData: any) {
    const dialogRef = this.dialog.open(EditCustomerPopupComponent, {
      width: '800px',
      data: customerData,
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getCustomers(false);
      }
    });
  }

  get label(): string {
    return this.selectedOption === 'Select Customer' ? 'Select Customer' : this.selectedOption;
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
    this.customersToDisplay = this.listOfCustomers.slice(startIndex, startIndex + event.pageSize);
  }

  getCustomers(resetPage: boolean = false) {
    this.showSpinnerforSearch = false;
    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Customer') {
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

    this.customersApi.getCustomers(this.selectedOption, this.searchValue).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        // ✅ old page info BEFORE updating data
        const oldPageIndex = this.paginator?.pageIndex ?? 0;
        const oldPageSize = this.paginator?.pageSize ?? 10;

        this.listOfCustomers = res.customers ?? [];

        if (this.listOfCustomers.length === 0) {
          this.toast.info({
            detail: "INFO",
            summary: this.languageService.getTranslation('noFilterDataMessage'),
            duration: 5000,
            position: 'topRight'
          });
          this.dataSource.data = [];
          this.customersToDisplay = [];
          this.showSpinnerforSearch = false;
          return;
        }

        // ✅ set data once
        this.dataSource.data = this.listOfCustomers;

        // ✅ resetPage ? first page : keep old page
        if (resetPage) {
          this.paginator?.firstPage();
        } else {
          const maxPageIndex = Math.max(Math.ceil(this.listOfCustomers.length / oldPageSize) - 1, 0);
          this.paginator.pageIndex = Math.min(oldPageIndex, maxPageIndex);

          // force re-render on same page
          this.paginator._changePageSize(this.paginator.pageSize);
        }

        if (this.code === 200) {
          this.showSpinnerforSearch = false;
        } else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, position: 'topRight' });
          this.showSpinnerforSearch = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, position: 'topRight' });
          this.showSpinnerforSearch = false;
        }
        this.showSpinnerforSearch = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.showSpinnerforSearch = false;
      },
    });
  }
}
