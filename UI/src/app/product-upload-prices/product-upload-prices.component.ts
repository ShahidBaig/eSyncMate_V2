import { Component, OnInit } from '@angular/core';
import { ProductUploadPrices } from '../models/models';
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

import { ProductUploadPricesService } from '../services/ProductUploadPrices.service';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { ApiService } from '../services/api.service';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { ViewChild } from '@angular/core';
interface Customers {
  erpCustomerID: string;
}

@Component({
  selector: 'product-upload-prices',
  templateUrl: './product-upload-prices.component.html',
  styleUrls: ['./product-upload-prices.component.scss'],
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
export class ProductUploadPricesComponent {
  listOfProductUploadPrices: ProductUploadPrices[] = [];
  productUploadPricesToDisplay: ProductUploadPrices[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Product Upload Prices', 'Id', 'ERP CustomerID', 'ItemID', 'Created Date', 'Promo StartDate', 'Promo EndDate'];
  selectedOption: string = 'Select Product Upload Prices';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  showDataColumn: boolean = true;
  selectedFile: File | null = null;
  isButtonDisabled: boolean = false;
  erpCustomerID: string = '';
  customersOptions: Customers[] | undefined;
  isAdminUser: boolean = false;
  dataSource = new MatTableDataSource<ProductUploadPrices>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  columns: string[] =
    [
      'id',
      'CustomerID',
      'ItemID',
      'ListPrice',
      'OffPrice',
      'MAPPrice',
      'PromoStartDate',
      'PromoEndDate',
      'CreatedDate',
      'OldListPrice',
      'OldPromoPrice',
      'OldMAPPrice'
      //'Edit',
  ];

    constructor(private api: ProductUploadPricesService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, private userApi: ApiService, public languageService: LanguageService) {
    this.isAdminUser = ["ADMIN", "WRITER"].includes(this.userApi.getTokenUserInfo()?.userType || '');
  }

  ngOnInit(): void {
    if (this.selectedOption === 'Select Product Upload Prices') {
      this.getCustomerProductCatalog(true);
    }

    this.getERPCustomer();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
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
    this.startDate = '';
    this.endDate = '';
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.productUploadPricesToDisplay = this.listOfProductUploadPrices.slice(startIndex, startIndex + event.pageSize);
  }

  getCustomerProductCatalog(resetPage: boolean = false) {
    this.showSpinnerforSearch = false;
    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Product Upload Prices') {
      this.searchValue = 'ALL';
    }

    if (this.selectedOption === 'Created Date' || this.selectedOption === 'Promo StartDate' || this.selectedOption === 'Promo EndDate' && this.startDate.toLocaleString().length > 10) {
      stringFromDate = this.getFormattedDate(this.startDate);
    }
    if (this.selectedOption === 'Created Date' || this.selectedOption === 'Promo StartDate' || this.selectedOption === 'Promo EndDate' && this.endDate.toLocaleString().length > 10) {
      stringToDate = this.getFormattedDate(this.endDate);
    }
    if (this.selectedOption === 'Created Date' || this.selectedOption === 'Promo StartDate' || this.selectedOption === 'Promo EndDate' && this.startDate.toLocaleString().length > 10 && this.endDate.toLocaleString().length > 10) {
      this.searchValue = stringFromDate + '/' + stringToDate;
    }

    this.api.getProductUploadPrices(this.selectedOption, this.searchValue).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        const oldPageIndex = this.paginator?.pageIndex ?? 0;
        const oldPageSize = this.paginator?.pageSize ?? 10;

        this.listOfProductUploadPrices = res.productUploadPrices ?? [];

        if (this.listOfProductUploadPrices == null || this.listOfProductUploadPrices.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.productUploadPricesToDisplay = [];
          this.dataSource.data = [];

          return;
        }

        this.dataSource.data = this.listOfProductUploadPrices;

        if (resetPage) {
          this.paginator?.firstPage();
        } else {
          const maxPageIndex = Math.max(Math.ceil(this.listOfProductUploadPrices.length / oldPageSize) - 1, 0);
          this.paginator.pageIndex = Math.min(oldPageIndex, maxPageIndex);

          this.paginator._changePageSize(this.paginator.pageSize);
        }

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

  downloadSampleFile() {
    this.api.downloadSampleFile().subscribe({
      next: (data: any) => {

        const filename = "ProductPromotion.csv";
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

  uploadCustomerProductCatalogFile() {
    if (this.selectedFile == null) {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('choosefileWarning'), duration: 2000, /*sticky: true,*/ position: 'topRight' });
      return;
    }

    if (this.erpCustomerID == null || this.erpCustomerID == "") {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('eRPCustID'), duration: 2000, /*sticky: true,*/ position: 'topRight' });
      return;
    }

    if (this.selectedFile) {
      this.isButtonDisabled = true;
      this.showSpinner = true;

      this.api.uploadProductUploadPricesFile(this.selectedFile, this.erpCustomerID).subscribe(
        {
          next: (res: any) => {
            this.msg = res.message;
            this.code = res.code;
            if (this.code === 200) {
              this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
              this.getCustomerProductCatalog(true);
            }
            else if (this.code === 201) {
              this.toast.warning({ detail: "Warning", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
              this.getCustomerProductCatalog(true);
            }
            else if (this.code === 400) {
              this.toast.warning({ detail: "ERROR", summary: this.msg, duration: 2000, sticky: true, position: 'topRight' });
            } else {
              this.toast.info({ detail: "INFO", summary: this.msg, duration: 2000, sticky: true, position: 'topRight' });
            }

            this.clearFile();
          },
          error: (err: any) => {
            this.toast.error({ detail: "ERROR", summary: err, duration: 2000, sticky: true, position: 'topRight' });
            this.isButtonDisabled = false;
            this.showSpinner = false;
          }
        });
    }
  }

  priceDescripencies(customerID: any) {
    if (!customerID) {
      this.showInfoToast(this.languageService.getTranslation('eRPCustID'));
      return;
    }

    this.api.priceDescripencies(customerID).subscribe({
      next: (data: any) => {

        const filename = "PriceDescripencies.csv";
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
      duration: 2000,
      position: 'topRight'
    });
  }

  private showErrorToast(message: string): void {
    this.toast.error({
      detail: message,
      summary: 'ERROR',
      duration: 2000,
      sticky: true,
      position: 'topRight'
    });
  }

}
