import { Component, OnInit } from '@angular/core';
import { CustomerProductCatalog, HistoryCustomerProductCatalog, PrepareItemData } from '../models/models';
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
import {MatAutocompleteModule, MatAutocompleteSelectedEvent} from '@angular/material/autocomplete';
import { EditCustomerProductCatalogDialogComponent } from './edit-customer-product-catalog-dialog/edit-customer-product-catalog-dialog.component';
import { HistoryCustomerProductCatalogDialogComponent } from './history-customer-product-catalog-dialog/history-customer-product-catalog-dialog.component';

import { CustomerProductCatalogService } from '../services/customerProductCatalogDialog.service';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { ProductDataComponent } from './product-data/product-data.component';
import { LanguageService } from '../services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ApiService } from '../services/api.service';
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
  selector: 'customer-product-catalog',
  templateUrl: './customer-product-catalog.component.html',
  styleUrls: ['./customer-product-catalog.component.scss'],
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
    MatAutocompleteModule,
  ],
})
export class CustomerProductCatalogComponent {
  listOfCustomerProductCatalog: CustomerProductCatalog[] = [];
  customerProductCatalogToDisplay: CustomerProductCatalog[] = [];
  listofItemsPrepareData: PrepareItemData[] = [];

  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinnerforSearchData: boolean = false;
  showSpinnerforRefresData: boolean = false;
  showProcessProductPrices: boolean = false;

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
  userID: number = 0;
  isPrepareData: boolean = false;
  isPrepareDataDisable: boolean = false;
  isPrepareDataNameChange: boolean = false;
  isDownloadItemsData: boolean = false;
  showSpinnerforClear: boolean = false;
  isClearItemsData: boolean = false;
  dataSource = new MatTableDataSource<CustomerProductCatalog>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  columns: string[] = [
    'ProductId',
    'CustomerID',
    'ItemID',
    'UPC',
    'Status',
    'itemTypeName',
    'ParentID',
    'ListPrice',
    'MapPrice',
    'CreatedDate',
    'Edit',
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
      this.getCustomerProductCatalog();
    }

    this.getERPCustomer();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
  }

  getFilteredItemTypes(): any {

    const filterValue = (this.itemTypeFilter ?? '').toString().trim().toLowerCase();

    if (filterValue === '') {
      return this.itemTypesOptions;
    }

    return this.itemTypesOptions?.filter(p =>
      p.item_Type.toLocaleLowerCase().includes(filterValue)
    );
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
    this.getItemTypes(this.erpCustomerID);
  }

  oncustomerSelectionChange(event: MatSelectChange) {
    this.customerID = event.value;
  }

  onItemTypesChange(event: MatAutocompleteSelectedEvent) {
    this.itemTypes = event.option.value.item_Type_Id;
    this.itemTypeName = event.option.value.item_Type;

  }

  openEditDialog(connectorData: any) {
    const dialogRef = this.dialog.open(EditCustomerProductCatalogDialogComponent, {
      width: '900px',
      data: connectorData,
      disableClose: true,
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getCustomerProductCatalog();
      }
    });
  }

  openHistoryDialog(data: any[]): void {
    const dialogRef = this.dialog.open(HistoryCustomerProductCatalogDialogComponent, {
      width: '900px',
      disableClose: true,
      data: { historyData: data }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('Dialog closed with result:', result);
    });
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

  processErrorResolve() {
    this.showProcessProductPrices = true;

    this.api.processResolveError(1).subscribe({
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

    this.api.getCustomerProductCatalog(this.selectedOption, this.searchValue).subscribe({
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

  uploadCustomerProductCatalogFile() {
    if (this.selectedFile == null) {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('choosefileWarning'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      return;
    }

    if (this.erpCustomerID == null || this.erpCustomerID == "") {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('eRPCustID'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      return;
    }

    if (!this.itemTypes) {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('itemTypeWarning'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      return;
    }


    if (this.selectedFile) {
      this.isButtonDisabled = true;
      this.showSpinner = true;

      this.api.uploadCustomerProductCatalogFile(this.selectedFile, this.erpCustomerID, this.itemTypes).subscribe(
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
              this.toast.warning({ detail: "ERROR", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
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

  showHistory(): void {
    if (!this.erpCustomerID) {
      this.showInfoToast(this.languageService.getTranslation('eRPCustID'));
      return;
    }

    this.api.getHistoryCustomerProductCatalog(this.erpCustomerID).subscribe({
      next: (res: any) => {
        if (res.code === 200) {
          this.openHistoryDialog(res.customerProductCatalog_Log);
        } else {
          this.showInfoToast(this.languageService.getTranslation('historyNoData'));
        }
      },
      error: (err: any) => {
        this.showErrorToast(this.languageService.getTranslation('historyError'));
      }
    });
  }

  downloadSampleFile(customerID: any, itemTypes: any) {
    if (!customerID) {
      this.showInfoToast(this.languageService.getTranslation('eRPCustID'));
      return;
    }

    if (!itemTypes) {
      this.showInfoToast(this.languageService.getTranslation('itemTypeWarning'));
      return;
    }

    this.api.downloadSampleFile(customerID, itemTypes).subscribe({
      next: (data: any) => {

        const filename = "ProductCatalog.csv";
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

  openRouteDataDialog(data: any[]): void {
    const dialogRef = this.dialog.open(ProductDataComponent, {
      width: '900px',
      disableClose: true,
      data: { historyData: data }
    });
  }

  showProductsData(productData: any) {
    this.api.getProductsData(productData.productId).subscribe({
      next: (res: any) => {
        if (res.code === 200 && res.customerProductCatalog.length > 0) {
          this.openRouteDataDialog(res.customerProductCatalog);
        } else {
          this.showInfoToast(this.languageService.getTranslation('routeNoData'));
        }
      },
      error: (err: any) => {
        this.showErrorToast(this.languageService.getTranslation('routeError'));
      }

    });
  }

  downloadRejectProductCSV(customerID: any) {
    if (!customerID) {
      this.showInfoToast(this.languageService.getTranslation('eRPCustID'));
      return;
    }

    this.api.downloadRejectedCSV(customerID).subscribe({
      next: (data: any) => {

        const filename = "RejectedProductCatalog.csv";
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


  prepraeItemsData(customerID: any, itemType: any) {
    if (!customerID) {
      this.showInfoToast(this.languageService.getTranslation('eRPCustID'));
      return;
    }

    if (!itemType)
    {
      this.showInfoToast(this.languageService.getTranslation('itemTypeID'));
      return;
    }
    this.userID = this.userApi.getTokenUserInfo()?.id || 0;

    this.api.insertPrepareItemData(this.userID,customerID, itemType).subscribe({
      next: (res: any) =>
      {
          this.msg = res.message;
          this.code = res.code;
          if (this.code === 200) {
            this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
            this.isPrepareDataDisable = true;
            this.isPrepareDataNameChange = true;
            this.isPrepareData = true;
          }
          else if (this.code === 400) {
            this.toast.warning({ detail: "ERROR", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
          } else {
            this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
          }
        },
        error: (err: any) => {
          this.toast.error({ detail: "ERROR", summary: err, duration: 5000, sticky: true, position: 'topRight' });
          this.isButtonDisabled = false;
          this.showSpinner = false;
      }
    });
  }

  downloadItemsDataCSV(customerID: any, itemType: any) {
    if (!customerID)
    {
      this.showInfoToast(this.languageService.getTranslation('eRPCustID'));
      return;
    }

    if (!itemType)
    {
      this.showInfoToast(this.languageService.getTranslation('itemTypeID'));
      return;
    }

    this.userID = this.userApi.getTokenUserInfo()?.id || 0;
    this.showSpinnerforSearch = true;

    this.api.downloadItemsDataCSV(customerID, itemType, this.userID).subscribe({
      next: (res: any) => {
        
        const binaryData = atob(res.data);


        if (binaryData == null || binaryData == "") {
          this.showInfoToast(this.languageService.getTranslation('prepareData'));
          this.showSpinnerforSearch = false;
          return;
        }
        else {
          const arrayBuffer = new Uint8Array(binaryData.length);
          for (let i = 0; i < binaryData.length; i++) {
            arrayBuffer[i] = binaryData.charCodeAt(i);
          }

          // Create a Blob from the typed array
          const blob = new Blob([arrayBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

          // Create a URL for the Blob
          const url = window.URL.createObjectURL(blob);

          // Create a link element to trigger the download
          const a = document.createElement('a');
          a.href = url;
          a.download = customerID + "-" + itemType + ".xlsx"; // Set the desired file name
          a.click();
          this.showSpinnerforSearch = false;

          // Revoke the object URL to free up resources
          window.URL.revokeObjectURL(url);
        }

        //if (data.body == null)
        //{
        //  this.showInfoToast(this.languageService.getTranslation('prepareData'));
        //  this.showSpinnerforSearch = false;
        //  return;
        //}

        //const filename = customerID +"-"+ itemType + ".csv" ;
        //const contentType = data.headers.get('content-type');

        //const linkElement = document.createElement('a');
        //try {
        //  const blob = new Blob([data.body], { type: contentType });
        //  const url = window.URL.createObjectURL(blob);

        //  linkElement.setAttribute('href', url);
        //  linkElement.setAttribute('download', filename);

        //  const clickEvent = new MouseEvent('click', {
        //    view: window,
        //    bubbles: true,
        //    cancelable: false
        //  });
        //  linkElement.dispatchEvent(clickEvent);
        //  this.showSpinnerforSearch = false;

        //  //this.api.deleteItemData(customerID, itemType, this.userID).subscribe({
        //  //  next: (res: any) => {
        //  //    this.msg = res.message;
        //  //    this.code = res.code;
        //  //    if (this.code === 200) {
        //  //      this.isDownloadItemsData = false;
        //  //    }
        //  //  }
        //  //});

        //} catch (ex) {
        //  console.log(ex);
        //}
      }
    });
  }


  clearItemsData(customerID: any, itemType: any) {
    if (!customerID) {
      this.showInfoToast(this.languageService.getTranslation('eRPCustID'));
      return;
    }

    if (!itemType) {
      this.showInfoToast(this.languageService.getTranslation('itemTypeID'));
      return;
    }
    this.userID = this.userApi.getTokenUserInfo()?.id || 0;
    this.showSpinnerforClear = true;

      this.api.deleteItemData(customerID, itemType, this.userID).subscribe({
            next: (res: any) => {
              this.msg = res.message;
              this.code = res.code;
              this.showSpinnerforClear = false;

            if (this.code === 200)
            {
                this.isPrepareData = true;
                this.isDownloadItemsData = false;
                this.isPrepareDataNameChange = false;
                this.isPrepareDataDisable = false;
              }
            }
          });
  }

  refreshItemsData(customerID: any) {
    if (!customerID) {
      this.showInfoToast(this.languageService.getTranslation('eRPCustID'));
      return;
    }

    this.userID = this.userApi.getTokenUserInfo()?.id || 0;
    this.showSpinnerforRefresData = true;


    this.api.getPrepareItemData(this.userID, this.erpCustomerID, "Empty").subscribe({
      next: (res: any) => {
        this.listofItemsPrepareData = res.itemDataResponseDatatable;
        this.msg = res.message;
        this.code = res.code;
        this.isClearItemsData = true;
        this.showSpinnerforRefresData = false;

        if (this.listofItemsPrepareData)
        {
          if (this.listofItemsPrepareData[0].itemTypeID) {
            this.itemTypes = this.listofItemsPrepareData[0].itemTypeID;
          }
        }

        if (this.listofItemsPrepareData == null || this.listofItemsPrepareData.length === 0) {
          this.isPrepareData = true;
          this.isDownloadItemsData = false;
          return;
        }

        if (this.listofItemsPrepareData && this.listofItemsPrepareData[0].status === "NEW") {

          //this.toast.info({ detail: "INFO", summary: "We are processing the items' data. Once this process is complete, you will be able to download it.!", duration: 5000, /*sticky: true,*/ position: 'topRight' });
          //this.showSpinnerforSearchData = false;

          this.isPrepareDataDisable = true;
          this.isPrepareDataNameChange = true;
          this.isPrepareData = true;
          this.isDownloadItemsData = false;
          return;
        }

        if (this.listofItemsPrepareData && this.listofItemsPrepareData[0].status === "COMPLETED" && this.listofItemsPrepareData[0].fileName != null) {
          this.isPrepareDataDisable = false;
          this.isDownloadItemsData = true;
          this.isPrepareData = false;
          return;
        }
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinnerforSearchData = false;
      },
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
      case 'SUSPENDED':
        return { key: 'CPCSUSPENDED' };
      case 'UNLISTED':
        return { key: 'CPCUNLISTED' };
      default:
        return { key: 'CPCUnknown' };
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
    } else if (status.toUpperCase() === 'REJECTED') {
      return 'rejected-status';
    } else if (status.toUpperCase() === 'SUSPENDED') {
      return 'suspended-status';
    } else if (status.toUpperCase() === 'UNLISTED') {
      return 'unlisted-status';
    } else {
      return 'invedi-status';
    }
  }

  displayItemType(itemType: any): string {
    return itemType ? `${itemType.item_Type}` : '';
  }

}

