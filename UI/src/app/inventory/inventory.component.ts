import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { BatchWiseInventory, Inventory, Order } from '../models/models';
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
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { InventoryService } from '../services/inventory.service';
import { InventorypopupComponent } from './inventory-popup/inventory-popup.component';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
import { InventoryBatchwiseComponent } from './inventory-batchwise/inventory-batchwise.component';

interface Customers {
  erpCustomerID: string;
}

interface RouteTypes {
  routeType: string;
}

@Component({
  selector: 'inventory',
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.scss'],
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
    MatPaginatorModule,
    TranslateModule,
    FormsModule
  ],
})
export class InventoryComponent implements OnInit {
  isLoading: boolean = false;
  mydate = environment.date;

  listOfInventory: Inventory[] = [];
  listOfInventoryFiles: Inventory[] = []
  listOfBatchWiseInventory: BatchWiseInventory[] = []
  inventoryToDisplay: Inventory[] = [];
  customersOptions: Customers[] | undefined;
  filteredCustomerOptions: Customers[] = [];
  customerSearchText = '';
  routeTypeOptions: RouteTypes[] | undefined;
  filteredRouteTypeOptions: RouteTypes[] = [];
  routeTypeSearchText = '';
  InventoryForm: FormGroup;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  statusOptions = ['Select Status', 'PROCESSING', 'COMPLETED','ERROR'];
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;

  columns: string[] = [
    'CustomerID',
    'Status',
    'ItemCount',
    'StartDate',
    'FinishDate',
    'PageCount',
    'Type',
    'File',
  ];

  constructor(private translate: TranslateService, private api: InventoryService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, public languageService: LanguageService) {
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.InventoryForm = this.fb.group({
      //customerIDId: fb.control(''),
      itemID: fb.control(''),
      startDate : new FormControl(formatDate(sevenDaysAgo, "yyyy-MM-dd", "en")),
      finishDate: new FormControl(formatDate(today, "yyyy-MM-dd", "en")),
      status: fb.control(''),
      customerID: fb.control(''),
      routeType: fb.control(''),
    });
  }

  ngOnInit(): void {
    this.getInventory();
    this.getERPCustomer();
    this.getRouteTypes();
  }

  onPageChange(event: PageEvent) {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.getInventory();
  }

  getERPCustomer() {
    this.api.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customersOptions = res.customers;
        this.filteredCustomerOptions = this.customersOptions || [];
      },
    });
  }

  getRouteTypes() {
    this.api.getRouteTypes().subscribe({
      next: (res: any) => {
        this.routeTypeOptions = res.routeType;
        this.filteredRouteTypeOptions = this.routeTypeOptions || [];
      },
    });
  }

  filterCustomerOptions() {
    const search = this.customerSearchText.toLowerCase();
    this.filteredCustomerOptions = (this.customersOptions || []).filter(c =>
      c.erpCustomerID.toLowerCase().includes(search)
    );
  }

  onCustomerSelectOpened(opened: boolean) {
    if (opened) {
      this.customerSearchText = '';
      this.filteredCustomerOptions = this.customersOptions || [];
    }
  }

  filterRouteTypeOptions() {
    const search = this.routeTypeSearchText.toLowerCase();
    this.filteredRouteTypeOptions = (this.routeTypeOptions || []).filter(r =>
      r.routeType.toLowerCase().includes(search)
    );
  }

  onRouteTypeSelectOpened(opened: boolean) {
    if (opened) {
      this.routeTypeSearchText = '';
      this.filteredRouteTypeOptions = this.routeTypeOptions || [];
    }
  }

  getStatusTooltip(status: string, batchID: string): any {
    switch (status) {
      case 'PROCESSING':
        return { key: 'Batch Processing' };
      case 'COMPLETED':
        return { key: 'Batch Completed' };
      case 'ERROR':
        return { key: 'Batch Error', params: { batchID: batchID.toUpperCase() } };
      default:
        return { key: '' };
    }
  }

  getTooltipWithTranslation(element: any): string {
    const tooltipData = this.getStatusTooltip(element.status.toUpperCase(), element.batchID);
    return this.translate.instant(tooltipData.key, tooltipData.params);
  }


  getStatusClass(status: string): string {
    if (status.toLocaleUpperCase() === 'PROCESSING') {
      return 'new-status';
    } else if (status.toLocaleUpperCase() === 'ERROR') {
      return 'syncerror-status';
    } else if (status.toLocaleUpperCase() === 'COMPLETED') {
      return 'sysced-status';
    } else {
      return '';
    }
  }

  getInventoryFiles(element: any) {
    let customerId = element.customerID;
    let itemId = element.itemId;

    this.showSpinner = false;

    this.api.getInventoryFiles(customerId, itemId).subscribe({
      next: (res: any) => {
        this.listOfInventoryFiles = res.files;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfInventoryFiles.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noInventoryDataMsg'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;

          return;
        }

        const dialogRef = this.dialog.open(InventorypopupComponent, {
          width: '70%',
          disableClose: true,
          data: this.listOfInventoryFiles,
        });

        dialogRef.afterClosed().subscribe(result => {
          console.log('The dialog was closed');
        });

        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }



  getBatchiWiseInventory(element: any) {
    let itemID = (this.InventoryForm.get('itemID') as FormControl).value;

    const dialogRef = this.dialog.open(InventoryBatchwiseComponent, {
      width: '95vw',
      maxWidth: '95vw',
      height: '85vh',
      panelClass: 'batch-wise-dialog-panel',
      disableClose: true,
      data: {
        batchID: element.batchID,
        itemID: itemID ?? null,
        batchStatus: element.status ?? '',
        routeType: element.routeType ?? '',
      },
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('The dialog was closed');
    });
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  getInventory(resetPage: boolean = false) {
    let itemID = (this.InventoryForm.get('itemID') as FormControl).value;
    let startDate = (this.InventoryForm.get('startDate') as FormControl).value;
    let finishDate = (this.InventoryForm.get('finishDate') as FormControl).value;
    let status = (this.InventoryForm.get('status') as FormControl).value;
    let customerID = (this.InventoryForm.get('customerID') as FormControl).value;
    let routeType =  (this.InventoryForm.get('routeType') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';

    if (resetPage) {
      this.pageNumber = 1;
    }

    if (((startDate == '' || startDate == null) && (finishDate == '' || finishDate == undefined) && (itemID == '' || itemID == 'EMPTY') && (status == '' || status == 'Select Status') && (customerID == '' || customerID.toUpperCase().includes('SELECT')) && (routeType == '' || routeType.toUpperCase().includes('SELECT')))) {
      this.toast.info({ detail: "inventory", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, position: 'topRight' });
      return;
    }

    if (itemID == '') { itemID = 'EMPTY' }
    if (status == '' || status.toLocaleLowerCase() == 'select status') { status = 'EMPTY' }
    if (customerID == '') { customerID = 'EMPTY' }
    if (routeType == '') { routeType = 'EMPTY' }

    if (startDate !== null) {
      stringFromDate = startDate.toLocaleString();
      if (stringFromDate.length > 10) { stringFromDate = this.getFormattedDate(startDate); }
    } else {
      stringFromDate = '1999-01-01';
    }

    if (finishDate !== null) {
      stringToDate = finishDate.toLocaleString();
      if (stringToDate.length > 10) { stringToDate = this.getFormattedDate(finishDate); }
    } else {
      stringToDate = '1999-01-01';
    }

    this.isLoading = true;
    this.api.getInventory(itemID, stringFromDate, stringToDate, status, customerID, routeType, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        this.listOfInventory = res.inventory ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.inventoryToDisplay = this.listOfInventory;

        if (this.listOfInventory.length === 0 && this.pageNumber === 1) {
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
