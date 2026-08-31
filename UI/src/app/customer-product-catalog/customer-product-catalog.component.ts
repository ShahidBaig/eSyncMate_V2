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
import { ProductCatalogHelpDialogComponent } from './product-catalog-help-dialog/product-catalog-help-dialog.component';
import { DeleteProductsDialogComponent } from './delete-products-dialog/delete-products-dialog.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
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
    MatProgressBarModule,
    CommonModule,
    MatSelectModule,
    FormsModule,
    MatPaginatorModule,
    TranslateModule,
    MatAutocompleteModule,
  ],
})
export class CustomerProductCatalogComponent {
  isLoading: boolean = false;
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;
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
  // Upload section
  erpCustomerID: string = '';
  itemTypes: string = '';
  itemTypeName: string = '';
  itemTypeFilter: string = '';
  itemTypesOptions: ItemTypes[] | undefined;

  // Actions section
  actionsErpCustomerID: string = '';
  actionsItemTypes: string = '';
  actionsItemTypeName: string = '';
  actionsItemTypeFilter: string = '';
  actionsItemTypesOptions: ItemTypes[] | undefined;

  // Download section
  downloadErpCustomerID: string = '';
  downloadItemTypes: string = '';
  downloadItemTypeName: string = '';
  downloadItemTypeFilter: string = '';
  downloadItemTypesOptions: ItemTypes[] | undefined;

  customerID: string = '';
  listOfHistoryCustomerProductCatalog: HistoryCustomerProductCatalog[] = [];
  historyCustomerProductCatalogToDisplay: HistoryCustomerProductCatalog[] = [];
  customersOptions: Customers[] | undefined;
  filteredCustomersOptions: Customers[] = [];
  customerSearchText: string = '';
  isAdminUser: boolean = false;
  canAdd = false;
  canEdit = false;
  canDelete = false;
  panelsCollapsed = false;
  activeMode: 'upload' | 'download' = 'upload';
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
    const permissions = this.userApi.getMenuPermissions('edi/customerProductCatalog');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
    } else {
      const isAdmin = ["ADMIN", "WRITER"].includes(this.userApi.getTokenUserInfo()?.userType || '');
      this.canAdd = isAdmin;
      this.canEdit = isAdmin;
      this.isAdminUser = isAdmin;

      // Delete Products is destructive and irreversible — it is granted only by the role's
      // Delete permission in Role Management, never by the legacy userType fallback.
      this.canDelete = false;
    }
  }

  ngOnInit(): void {
    if (!this.canEdit) {
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
        this.filteredCustomersOptions = this.customersOptions || [];
      },
    });
  }

  filterCustomers(): void {
    const search = (this.customerSearchText || '').toLowerCase();
    this.filteredCustomersOptions = (this.customersOptions || []).filter(c =>
      c.erpCustomerID.toLowerCase().includes(search)
    );
  }

  onCustomerDropdownOpened(opened: boolean): void {
    if (opened) {
      this.customerSearchText = '';
      this.filteredCustomersOptions = this.customersOptions || [];
    }
  }

  // Upload section handlers
  onCustomerSelectionChange(event: MatSelectChange) {
    this.erpCustomerID = event.value;
    this.getItemTypes(this.erpCustomerID);
  }

  onItemTypesChange(event: MatAutocompleteSelectedEvent) {
    this.itemTypes = event.option.value.item_Type_Id;
    this.itemTypeName = event.option.value.item_Type;
  }

  // Actions section handlers
  onActionsCustomerChange(event: MatSelectChange) {
    this.actionsErpCustomerID = event.value;
    this.api.getItemTypes(this.actionsErpCustomerID).subscribe({
      next: (res: any) => { this.actionsItemTypesOptions = res.itemTypes; },
    });
  }

  onActionsItemTypeChange(event: MatAutocompleteSelectedEvent) {
    this.actionsItemTypes = event.option.value.item_Type_Id;
    this.actionsItemTypeName = event.option.value.item_Type;
  }

  getFilteredActionsItemTypes(): any {
    const filterValue = (this.actionsItemTypeFilter ?? '').toString().trim().toLowerCase();
    if (filterValue === '') return this.actionsItemTypesOptions;
    return this.actionsItemTypesOptions?.filter(p => p.item_Type.toLocaleLowerCase().includes(filterValue));
  }

  // Download section handlers
  onDownloadCustomerChange(event: MatSelectChange) {
    this.downloadErpCustomerID = event.value;
    this.api.getItemTypes(this.downloadErpCustomerID).subscribe({
      next: (res: any) => { this.downloadItemTypesOptions = res.itemTypes; },
    });
  }

  onDownloadItemTypeChange(event: MatAutocompleteSelectedEvent) {
    this.downloadItemTypes = event.option.value.item_Type_Id;
    this.downloadItemTypeName = event.option.value.item_Type;
  }

  getFilteredDownloadItemTypes(): any {
    const filterValue = (this.downloadItemTypeFilter ?? '').toString().trim().toLowerCase();
    if (filterValue === '') return this.downloadItemTypesOptions;
    return this.downloadItemTypesOptions?.filter(p => p.item_Type.toLocaleLowerCase().includes(filterValue));
  }

  oncustomerSelectionChange(event: MatSelectChange) {
    this.customerID = event.value;
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
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.getCustomerProductCatalog();
  }

  getCustomerProductCatalog(resetPage: boolean = false) {
    this.showSpinnerforSearchData = true;
    let stringFromDate = '';
    let stringToDate = '';

    if (resetPage) {
      this.pageNumber = 1;
    }

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

    // A filter chosen without a value is not a search, so ask for the value instead of calling the API
    if (this.selectedOption !== 'Select Customer Product Catalog' && !this.searchValue?.trim()) {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('enterSearchValueMessage'), duration: 4000, position: 'topRight' });
      this.showSpinnerforSearchData = false;
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    this.api.getCustomerProductCatalog(this.selectedOption, this.searchValue, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        this.listOfCustomerProductCatalog = res.customerProductCatalogDatatable ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.customerProductCatalogToDisplay = this.listOfCustomerProductCatalog;

        if (this.listOfCustomerProductCatalog.length === 0 && this.pageNumber === 1) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, position: 'topRight' });
        }

        if (this.code === 200) {
          this.showSpinnerforSearchData = false;
        } else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, position: 'topRight' });
          this.showSpinnerforSearchData = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, position: 'topRight' });
          this.showSpinnerforSearchData = false;
        }

        this.showSpinnerforSearchData = false;
        this.isLoading = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinnerforSearchData = false;
        this.isLoading = false;
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

  openHelp(): void {
    this.dialog.open(ProductCatalogHelpDialogComponent, {
      width: '90%',
      maxWidth: '1200px',
      maxHeight: '90vh',
    });
  }

  openDeleteProductsDialog(): void {
    const dialogRef = this.dialog.open(DeleteProductsDialogComponent, {
      width: '900px',
      disableClose: true,
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'deleted') this.getCustomerProductCatalog();
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
    // A failed item shows what actually went wrong instead of the generic status text
    const errorText = this.getCatalogErrorText(element);
    if (errorText) return errorText;

    const tooltipData = this.getStatusTooltip(element.syncStatus.toUpperCase(), element.customerID);
    return this.translate.instant(tooltipData.key, tooltipData.params);
  }

  // SCS_CustomerProductCatalogData.Type -> tooltip heading.
  // PRD/UNL/STA/LOG-* are the per-operation types; REQ-ERR/RSP-ERR/RSP-JSON are their predecessors
  // and stay mapped so items logged before the split still read correctly.
  private catalogErrorHeadings: { [key: string]: string } = {
    'PRD-ERR': 'PRODUCT SYNC ERROR',
    'UNL-ERR': 'UNLIST ERROR',
    'STA-ERR': 'LISTING STATUS ERROR',
    'LOG-ERR': 'LOGISTICS UPDATE ERROR',
    'RSP-ERR': 'MARKETPLACE ERROR',
    'REQ-ERR': 'REQUEST ERROR',
    'Internal': 'VALIDATION ERROR',
    'STA-RSP': 'REJECTED BY MARKETPLACE',
    'RSP-JSON': 'REJECTED BY MARKETPLACE'
  };

  /**
   * RSP-JSON is the status payload, not an error row. On a REJECTED item it is the rejection itself,
   * but a PENDING (or other non-final) item can carry errors from its previous listing attempt.
   */
  private getCatalogErrorHeading(element: any): string {
    if ((element?.errorType === 'STA-RSP' || element?.errorType === 'RSP-JSON') && !this.isRejected(element)) {
      return 'MARKETPLACE VALIDATION ERROR';
    }
    return this.catalogErrorHeadings[element?.errorType] || 'ERROR';
  }

  private isRejected(element: any): boolean {
    return String(element?.syncStatus || '').toUpperCase() === 'REJECTED';
  }

  /**
   * A rejection response is an array of products, each carrying product_statuses[] with the
   * listing errors. Returns the distinct reasons, worst case falling back to a text scan when
   * the payload was truncated and no longer parses.
   */
  private readRejectionReasons(raw: string, parsed: any): string[] {
    const reasons: string[] = [];
    const seen = new Set<string>();

    const push = (text: string) => {
      const value = (text || '').trim();
      if (value && !seen.has(value)) { seen.add(value); reasons.push(value); }
    };

    if (Array.isArray(parsed)) {
      parsed.forEach((product: any) => {
        (product?.product_statuses || []).forEach((status: any) => {
          (status?.errors || []).forEach((err: any) => {
            const code = err?.error_code ? `${err.error_code} — ` : '';
            push(`${code}${err?.reason || err?.category || ''}`);
          });
        });
      });
    }

    // Truncated JSON still contains readable "reason" values
    if (reasons.length === 0) {
      const matches = raw.match(/"reason"\s*:\s*"((?:[^"\\]|\\.)*)"/g) || [];
      matches.forEach(m => {
        const value = m.replace(/^"reason"\s*:\s*"/, '').replace(/"$/, '').replace(/\\"/g, '"').replace(/\\\\/g, '\\');
        push(value);
      });
    }

    return reasons;
  }

  hasCatalogError(element: any): boolean {
    return !!this.getCatalogErrorText(element);
  }

  /** Formats the stored error payload into readable tooltip lines. */
  getCatalogErrorText(element: any): string {
    const raw = element?.errorData;
    if (!raw) return '';

    const label = this.getCatalogErrorHeading(element);
    const when = element?.errorDate ? formatDate(element.errorDate, 'MM/dd/yyyy hh:mm a', 'en-US') : '';
    const heading = when ? `${label}  ·  ${when}` : label;

    let message = '';
    const detailItems: string[] = [];

    let parsed: any = null;
    try {
      parsed = JSON.parse(raw);
    } catch {
      // Not JSON — show the payload as-is under the heading
    }

    if (element?.errorType === 'RSP-JSON' || Array.isArray(parsed)) {
      // Rejection response — reasons live inside product_statuses[].errors[]
      const reasons = this.readRejectionReasons(String(raw), parsed);

      // A successful status response has the same shape but an empty errors[]. With nothing to
      // report there is no error, so say nothing rather than print a heading over no reasons.
      // A REJECTED item still reports, since its status alone means the listing failed.
      if (reasons.length === 0 && !this.isRejected(element)) return '';

      message = reasons.length === 1
        ? ''
        : (this.isRejected(element) ? 'The marketplace rejected this listing:' : 'The marketplace reported these errors on this listing:');
      reasons.forEach(r => detailItems.push(r));
    } else if (parsed) {
      // Error payloads use either "message" or "Message"
      message = String(parsed.message || parsed.Message || '');

      const errors = parsed.errors || parsed.Errors || [];
      if (Array.isArray(errors)) {
        errors.forEach((e: any) => {
          const text = typeof e === 'string' ? e : (e?.description || e?.Description || JSON.stringify(e));
          if (text) detailItems.push(String(text));
        });
      }
    }

    if (!message && detailItems.length === 0) message = String(raw).trim();

    const parts = [heading];

    if (message) {
      parts.push('');
      parts.push(message);
    }

    if (detailItems.length > 0) {
      parts.push('');
      parts.push(detailItems.map(l => `•  ${l}`).join('\n'));
    }

    return parts.join('\n');
  }

  getStatusClass(status: string): string {
    if (!status) return '';
    switch (status.toUpperCase()) {
      case 'NEW':           return 'new-status';
      case 'UPDATED':       return 'updated-status';
      case 'PENDING':       return 'pending-status';
      case 'APPROVED':      return 'approved-status';
      case 'APPROVED_PR':   return 'approved-status';
      case 'PUBLISHED':     return 'published-status';
      case 'SYNCED':        return 'synced-status';
      case 'REJECTED':      return 'rejected-status';
      case 'ERROR':         return 'error-status';
      case 'SUSPENDED':     return 'suspended-status';
      case 'UNLISTED':      return 'unlisted-status';
      default:              return 'synced-status';
    }
  }

  displayItemType(itemType: any): string {
    return itemType ? `${itemType.item_Type}` : '';
  }

}

