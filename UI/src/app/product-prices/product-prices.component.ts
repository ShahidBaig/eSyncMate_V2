  import { Component, OnInit } from '@angular/core';
import { CustomerProductCatalog, HistoryCustomerProductCatalog } from '../models/models';
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
import { MatSelectChange, MatSelectModule } from '@angular/material/select';

import { CustomerProductCatalogService } from '../services/customerProductCatalogDialog.service';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ApiService } from '../services/api.service';
import * as XLSX from 'xlsx';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { ViewChild } from '@angular/core';

interface ItemTypes {
  item_Type_Id: string;
  item_Type: string;
}

interface Customers {
  erpCustomerID: string;
}

@Component({
  selector: 'product-prices',
  templateUrl: './product-prices.component.html',
  styleUrls: ['./product-prices.component.scss'],
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
    TranslateModule,
  ],
})
export class ProductPricesComponent {
  listOfCustomerProductCatalog: CustomerProductCatalog[] = [];
  customerProductCatalogToDisplay: CustomerProductCatalog[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinnerforSearchData: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Customer Product Catalog', 'ProductId', 'UPC', 'ERP CustomerID', 'ItemID', 'Item Type Name', 'Parent ID', 'Status', 'Created Date'];
  selectedOption: string = 'Select Customer Product Catalog';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  showDataColumn: boolean = true;
  selectedFile: File | null = null;
  isButtonDisabled: boolean = false;
  erpCustomerID: string = '';
  customerID: string = '';
  listOfHistoryCustomerProductCatalog: HistoryCustomerProductCatalog[] = [];
  historyCustomerProductCatalogToDisplay: HistoryCustomerProductCatalog[] = [];
  itemTypesOptions: ItemTypes[] | undefined;
  customersOptions: Customers[] | undefined;
  itemTypes: string = '';
  itemTypeName: string = '';
  isAdminUser: boolean = false;
  itemTypeFilter: string = '';
  desc: string = '';
  showProcessProductPrices: boolean = false;
  dataSource = new MatTableDataSource<CustomerProductCatalog>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  columns: string[] = [
    'ProductId',
    'CustomerID',
    'ItemID',
    'Status',
    'ListPrice',
    'MapPrice',
    'OffPrice',
    'ActivityDate',
  ];

  constructor(private api: CustomerProductCatalogService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, private userApi: ApiService, public languageService: LanguageService, private translate: TranslateService,) {
    this.isAdminUser = ["ADMIN", "WRITER"].includes(this.userApi.getTokenUserInfo()?.userType || '');
  }

  ngOnInit(): void {
    if (!this.isAdminUser) {
      const editIndex = this.columns.indexOf('Edit');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }

    if (this.selectedOption === 'Select Customer Product Catalog') {
      this.getCustomerProductCatalog(true);
    }

    this.getERPCustomer();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
  }

  getFilteredItemTypes(): any {
    if (this.itemTypeFilter == '')
      return this.itemTypesOptions;

    return this.itemTypesOptions?.filter(p => p.item_Type.includes(this.itemTypeFilter));
  }

  exportToExcel() {
    const ws: XLSX.WorkSheet = XLSX.utils.json_to_sheet(this.customerProductCatalogToDisplay);

    const wb: XLSX.WorkBook = XLSX.utils.book_new();

    XLSX.utils.book_append_sheet(wb, ws, 'ProductPrices');

    XLSX.writeFile(wb, 'ProductPrices.xlsx');  
  }


  getItemTypes(erpCustomerID: any) {
    this.api.getItemTypes(erpCustomerID).subscribe({
      next: (res: any) => {
        this.itemTypesOptions = res.itemTypes;
      },
    });
  }

  getERPCustomer() {
    this.api.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customersOptions = res.customers;
      },
    });
  }

  onCustomerSelectionChange(event: MatSelectChange) {
    this.erpCustomerID = event.value;
    /*this.getItemTypes(this.erpCustomerID);*/
  }

  oncustomerSelectionChange(event: MatSelectChange) {
    this.customerID = event.value;
  }

  onItemTypesChange(event: MatSelectChange) {
    this.itemTypes = event.value;
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

  get label(): string {
    return this.selectedOption === 'Select Customer Product Catalog' ? 'Select Customer Product Catalog' : this.selectedOption;
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
    this.customerProductCatalogToDisplay = this.listOfCustomerProductCatalog.slice(startIndex, startIndex + event.pageSize);
  }

  getCustomerProductCatalog(resetPage: boolean = false) {
    this.showSpinnerforSearchData = true;
    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Customer Product Catalog') {
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

    this.api.getSCSBulkUploadPrice(this.selectedOption, this.searchValue).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;
        const oldPageIndex = this.paginator?.pageIndex ?? 0;
        const oldPageSize = this.paginator?.pageSize ?? 10;

        this.listOfCustomerProductCatalog = res.customerProductCatalogDatatable ?? [];

        if (this.listOfCustomerProductCatalog == null || this.listOfCustomerProductCatalog.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearchData = false;
          this.customerProductCatalogToDisplay = [];
          this.dataSource.data = [];

          return;
        }

        this.dataSource.data = this.listOfCustomerProductCatalog;

        if (resetPage) {
          this.paginator?.firstPage();
        } else {
          const maxPageIndex = Math.max(Math.ceil(this.listOfCustomerProductCatalog.length / oldPageSize) - 1, 0);
          this.paginator.pageIndex = Math.min(oldPageIndex, maxPageIndex);

          this.paginator._changePageSize(this.paginator.pageSize);
        }


        if (this.code === 200) {
          this.showSpinnerforSearchData = false;
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearchData = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearchData = false;
        }

        this.showSpinnerforSearchData = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinnerforSearchData = false;
      },
    });
  }

  processCustomerProductPrices(erpCustomerID: string) {
    this.showProcessProductPrices = true;
    if (this.erpCustomerID === '' || this.erpCustomerID === 'SPARS Customer' || this.erpCustomerID === undefined) {
      this.toast.warning({ detail: "INFO", summary: 'Please select a Customer ID', duration: 5000, /*sticky: true,*/ position: 'topRight' });
      return;
    }

    this.api.processCustomerProductPrices(1, this.erpCustomerID).subscribe({
      next: (res: any) => {

        if (res.code == 200) {
          this.toast.success({ detail: "success", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showProcessProductPrices = false;
        }

        if (res.code == 400) {
          this.toast.warning({ detail: "warning", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showProcessProductPrices = false;
        }
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showProcessProductPrices = false;
      },
    });
  }

  uploadCustomerProductCatalogFile() {
    if (this.selectedFile == null) {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('choosefileWarning'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      return;
    }

    if (this.erpCustomerID == null || this.erpCustomerID == "") {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('eRPCustID'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      return;
    }

    if (this.selectedFile) {
      this.isButtonDisabled = true;
      this.showSpinner = true;

      this.api.uploadProductPricesFile(this.selectedFile, this.erpCustomerID).subscribe(
        {
          next: (res: any) => {
            this.msg = res.message;
            this.code = res.code;
            this.desc = res.description

            if (this.code === 200) {
              this.toast.success({ detail: "SUCCESS", summary: this.languageService.getTranslation('file') + ': [ ' + this.selectedFile?.name + ' ] ' + this.languageService.getTranslation('successfully'), duration: 5000, sticky: true, position: 'topRight' });
              this.getCustomerProductCatalog(true);
            }
            else if (this.code === 400) {
              this.toast.warning({ detail: "ERROR", summary: this.msg + this.desc, duration: 5000, sticky: true, position: 'topRight' });
            } else {
              this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
            }

            this.clearFile();
          },
          error: (err: any) => {
            this.toast.error({ detail: "ERROR", summary: err, duration: 5000, sticky: true, position: 'topRight' });
            this.isButtonDisabled = false;
            this.showSpinner = false;
          }
        });
    }
  }

  downloadSampleFile(customerID: any) {
    if (!customerID) {
      this.showInfoToast(this.languageService.getTranslation('eRPCustID'));
      return;
    }

    this.api.downloadProductPricesSampleFile(customerID).subscribe({
      next: (data: any) => {

        const filename = "ProductPrices.csv";
        const contentType = data.headers.get('content-type');

        const linkElement = document.createElement('a');
        try {
          const blob = new Blob([data.body], { type: contentType });
          const url = window.URL.createObjectURL(blob);

          linkElement.setAttribute('href', url);
          linkElement.setAttribute('download', filename);

          const clickEvent = new MouseEvent('click', {
            view: window,
            bubbles: true,
            cancelable: false
          });
          linkElement.dispatchEvent(clickEvent);
        } catch (ex) {
          console.log(ex);
        }
      }
    });
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


  getStatusTooltip(status: string, customerName: string): any {
    switch (status) {
      case 'NEW':
        return { key: 'CPCNEW' };
      case 'APPROVED':
        return { key: 'CPCAPPROVED' };
      case 'SYNCED':
        return { key: 'CPCSYNCED', params: { customerName: customerName.toUpperCase() } };
      case 'APPROVED_PR':
        return { key: 'CPCAPPROVED_PR' };
      case 'ERROR':
        return { key: 'CPCERROR' };
      case 'UPDATED':
        return { key: 'CPCUPDATED' };
      case 'PENDING':
        return { key: 'CPCPENDING' };
      case 'REJECTED':
        return { key: 'CPCREJECTED' };
      case 'DELETED':
        return { key: 'CPCAPPROVED_PR' };
      default:
        return '';
    }
  }

  getTooltipWithTranslation(element: any): string {
    const tooltipData = this.getStatusTooltip(element.syncStatus.toUpperCase(), element.customerID);
    return this.translate.instant(tooltipData.key, tooltipData.params);
  }

  getStatusClass(status: string): string {
    if (status.toUpperCase() === 'NEW') {
      return 'new-status';
    } else if (status.toUpperCase() === 'SYNCED') {
      return 'sysced-status';
    } else if (status.toUpperCase() === 'APPROVED') {
      return 'processed-status';
    } else if (status.toUpperCase() === 'APPROVED_PR') {
      return 'acknowledged-status';
    } else if (status.toUpperCase() === 'ERROR') {
      return 'syncerror-status';
    } else if (status.toUpperCase() === 'UPDATED') {
      return 'finished-status';
    } else if (status.toUpperCase() === 'PENDING') {
      return 'splited-status';
    } else if (status.toUpperCase() === 'DELETED') {
      return 'processed-status';
    } else if (status.toUpperCase() === 'REJECTED') {
      return 'invedi-status';

    } else {
      return '';
    }
  }

}

