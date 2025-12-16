import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { BatchWiseInventory, Inventory, Order } from '../models/models';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
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
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { InventoryService } from '../services/inventory.service';
import { InventorypopupComponent } from './inventory-popup/inventory-popup.component';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
import { InventoryBatchwiseComponent } from './inventory-batchwise/inventory-batchwise.component';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { ViewChild } from '@angular/core';

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
    CommonModule,
    MatSelectModule,
    MatPaginatorModule,
    TranslateModule
  ],
})
export class InventoryComponent implements OnInit {
  mydate = environment.date;

  listOfInventory: Inventory[] = [];
  listOfInventoryFiles: Inventory[] = []
  listOfBatchWiseInventory: BatchWiseInventory[] = []
  inventoryToDisplay: Inventory[] = [];
  customersOptions: Customers[] | undefined;
  routeTypeOptions: RouteTypes[] | undefined;
  InventoryForm: FormGroup;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  statusOptions = ['Select Status', 'PROCESSING', 'COMPLETED','ERROR'];
  dataSource = new MatTableDataSource<Inventory>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

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

  getRouteTypes() {
    this.api.getRouteTypes().subscribe({
      next: (res: any) => {
        this.routeTypeOptions = res.routeType;
      },
    });
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
    let batchID = element.batchID;

    this.showSpinner = false;

    this.api.getbatchWise(batchID).subscribe({
      next: (res: any) => {
        this.listOfBatchWiseInventory = res.batchWiseInventory;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfBatchWiseInventory.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noInventoryDataMsg'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;

          return;
        }

        let itemID = (this.InventoryForm.get('itemID') as FormControl).value;

        const dialogRef = this.dialog.open(InventoryBatchwiseComponent, {
          width: '100%',
          disableClose: true,
          data: {listofInventoryFiles: this.listOfBatchWiseInventory,
                itemID: itemID ?? null, // Add itemID only if it exists, otherwise set as null
          },
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

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.inventoryToDisplay = this.listOfInventory.slice(startIndex, startIndex + event.pageSize);
  }

  getInventory(resetPage: boolean = false) {
    //let customerID = (this.InventoryForm.get('customerID') as FormControl).value;
    let itemID = (this.InventoryForm.get('itemID') as FormControl).value;
    let startDate = (this.InventoryForm.get('startDate') as FormControl).value;
    let finishDate = (this.InventoryForm.get('finishDate') as FormControl).value;
    let status = (this.InventoryForm.get('status') as FormControl).value;
    let customerID = (this.InventoryForm.get('customerID') as FormControl).value;
    let routeType =  (this.InventoryForm.get('routeType') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';
    this.showSpinnerforSearch = true;

    if (((startDate == '' || startDate == null) && (finishDate == '' || finishDate == undefined) && (itemID == '' || itemID == 'EMPTY') && (status == '' || status == 'Select Status') && (customerID == '' || customerID.toUpperCase().includes('SELECT')) && (routeType == '' || routeType.toUpperCase().includes('SELECT')))) {
      this.toast.info({ detail: "inventory", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      this.showSpinnerforSearch = false;
      return;
    }

    if (itemID == '') {
      itemID = 'EMPTY'
    }

    if (status == '' || status.toLocaleLowerCase() == 'select status') {
      status = 'EMPTY'
    }

    if (customerID == '') {
      customerID = 'EMPTY'
    }

    if (routeType == '') {
      routeType = 'EMPTY'
    }

    if (startDate !== null) {
      stringFromDate = startDate.toLocaleString();

      if (stringFromDate.length > 10) {
        stringFromDate = this.getFormattedDate(startDate);
      }
    } else {
      stringFromDate = '1999-01-01';
    }

    if (finishDate !== null) {
      stringToDate = finishDate.toLocaleString();

      if (stringToDate.length > 10) {
        stringToDate = this.getFormattedDate(finishDate);
      }
    } else {
      stringToDate = '1999-01-01';
    }

    this.api.getInventory(itemID, stringFromDate, stringToDate, status, customerID, routeType).subscribe({
      next: (res: any) => {
        
        this.msg = res.message;
        this.code = res.code;

        const oldPageIndex = this.paginator?.pageIndex ?? 0;
        const oldPageSize = this.paginator?.pageSize ?? 10;

        this.listOfInventory = res.inventory ?? [];

        if (this.listOfInventory == null || this.listOfInventory.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.inventoryToDisplay = [];
          this.dataSource.data = [];

          return;
        }

        this.dataSource.data = this.listOfInventory;

        if (resetPage) {
          this.paginator?.firstPage();
        } else {
          const maxPageIndex = Math.max(Math.ceil(this.listOfInventory.length / oldPageSize) - 1, 0);
          this.paginator.pageIndex = Math.min(oldPageIndex, maxPageIndex);

          this.paginator._changePageSize(this.paginator.pageSize);
        }

        if (this.code === 200) {
          //this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
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
