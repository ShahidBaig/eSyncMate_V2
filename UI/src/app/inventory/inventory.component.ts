import { Component, OnInit } from '@angular/core';
import { BatchWiseInventory, Inventory } from '../models/models';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, ReactiveFormsModule, FormsModule } from '@angular/forms';
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
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { InventoryService } from '../services/inventory.service';
import { InventorypopupComponent } from './inventory-popup/inventory-popup.component';
import { environment } from 'src/environments/environment';
import { InventoryBatchwiseComponent } from './inventory-batchwise/inventory-batchwise.component';
import { InventoryHelpDialogComponent } from './inventory-help-dialog/inventory-help-dialog.component';
import { trigger, state, style, transition, animate } from '@angular/animations';

interface Customers {
  erpCustomerID: string;
}

@Component({
  selector: 'inventory',
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.scss'],
  standalone: true,
  imports: [
    MatButtonToggleModule, MatTableModule, DatePipe, MatCardModule,
    ReactiveFormsModule, MatFormFieldModule, MatInputModule, NgIf,
    MatButtonModule, MatDatepickerModule, MatNativeDateModule,
    MatTooltipModule, MatIconModule, MatProgressSpinnerModule,
    MatProgressBarModule, CommonModule, MatSelectModule,
    MatPaginatorModule, TranslateModule, FormsModule
  ],
  animations: [
    // State-based expand — content stays in DOM, animates between two states.
    // This is the Angular Material recommended pattern for expandable table rows
    // and gives the smoothest possible result.
    trigger('detailExpand', [
      state('collapsed, void', style({ height: '0px', minHeight: '0', opacity: 0 })),
      state('expanded', style({ height: '*', opacity: 1 })),
      transition('expanded <=> collapsed',
        animate('400ms cubic-bezier(0.4, 0, 0.2, 1)'))
    ])
  ]
})
export class InventoryComponent implements OnInit {
  isLoading = false;
  mydate = environment.date;

  listOfInventory: Inventory[] = [];
  listOfInventoryFiles: Inventory[] = [];
  listOfBatchWiseInventory: BatchWiseInventory[] = [];
  inventoryToDisplay: Inventory[] = [];
  customersOptions: Customers[] | undefined;
  filteredCustomerOptions: Customers[] = [];
  customerSearchText = '';
  InventoryForm: FormGroup;
  msg = '';
  code = 0;
  showSpinnerforSearch = false;
  showSpinner = false;
  statusOptions = ['Select Status', 'PROCESSING', 'COMPLETED', 'ERROR'];
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;

  // Expandable row state
  expandedBatchID: string | null = null;
  downloadBatches: any[] = [];
  downloadLoading = false;

  columns: string[] = ['CustomerID', 'Status', 'StartDate', 'FinishDate', 'File'];

  constructor(
    private translate: TranslateService,
    private api: InventoryService,
    private fb: FormBuilder,
    private toast: NgToastService,
    private dialog: MatDialog,
    public languageService: LanguageService
  ) {
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);
    const today = new Date();

    this.InventoryForm = this.fb.group({
      itemID: fb.control(''),
      startDate: new FormControl(formatDate(yesterday, "yyyy-MM-dd", "en")),
      finishDate: new FormControl(formatDate(today, "yyyy-MM-dd", "en")),
      status: fb.control(''),
      customerID: fb.control(''),
    });
  }

  ngOnInit(): void {
    this.getERPCustomer();
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
        if (this.customersOptions && this.customersOptions.length === 1) {
          this.InventoryForm.get('customerID')?.setValue(this.customersOptions[0].erpCustomerID);
          this.onCustomerChanged();
          this.getInventory(true);
        }
      },
    });
  }

  onCustomerChanged() {
    // Reset expanded row when customer changes
    this.expandedBatchID = null;
    this.downloadBatches = [];
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

  getStatusTooltip(status: string, batchID: string): any {
    switch (status) {
      case 'PROCESSING': return { key: 'Batch Processing' };
      case 'COMPLETED': return { key: 'Batch Completed' };
      case 'ERROR': return { key: 'Batch Error', params: { batchID: batchID.toUpperCase() } };
      default: return { key: '' };
    }
  }

  getTooltipWithTranslation(element: any): string {
    const tooltipData = this.getStatusTooltip(element.status.toUpperCase(), element.batchID);
    return this.translate.instant(tooltipData.key, tooltipData.params);
  }

  getStatusClass(status: string): string {
    const s = (status || '').toUpperCase();
    if (s === 'PROCESSING') return 'new-status';
    if (s === 'ERROR') return 'syncerror-status';
    if (s === 'COMPLETED') return 'sysced-status';
    if (s === 'ABORTED') return 'aborted-status';
    return '';
  }

  // ========== EXPANDABLE ROW LOGIC ==========

  toggleExpand(element: any) {
    if (this.expandedBatchID === element.batchID) {
      this.expandedBatchID = null;
      this.downloadBatches = [];
      return;
    }

    this.expandedBatchID = element.batchID;
    this.downloadBatches = [];
    this.downloadLoading = true;

    const filterItemID = (this.InventoryForm.get('itemID') as FormControl).value || '';

    this.api.getConsolidatedDownload(element.batchID, filterItemID).subscribe({
      next: (res: any) => {
        const mainRows = res?.mainRow || [];
        const breakdown = res?.typeBreakdown || [];

        if (mainRows.length === 0) {
          this.downloadBatches = [];
          this.downloadLoading = false;
          return;
        }

        const main = mainRows[0];
        element._prevUploadDate = main.previousUploadDate || null;

        const batchIDs = (main.batchIDs || '').split(',').filter((x: string) => !!x);

        const typeBreakdown = breakdown.map((b: any) => ({
          type: b.orignalRouteType,
          displayName: b.displayName,
          count: b.batchCount  // RouteType count, not itemCount
        }));

        this.downloadBatches = [{
          batchID: batchIDs[batchIDs.length - 1],
          mergedBatchIDs: batchIDs,
          typeBreakdown: typeBreakdown,
          isMerged: true,
          mergedCount: main.mergedCount,
          routeType: main.routeType,
          status: main.status,
          startDate: main.startDate,
          finishDate: main.finishDate,
          customerID: main.customerID
        }];
        this.downloadLoading = false;
      },
      error: () => {
        this.downloadBatches = [];
        this.downloadLoading = false;
      }
    });
  }

  // ========== EXISTING METHODS ==========

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
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noInventoryDataMsg'), duration: 5000, position: 'topRight' });
          this.showSpinner = false;
          return;
        }

        this.dialog.open(InventorypopupComponent, {
          width: '70%', disableClose: true, data: this.listOfInventoryFiles,
        });
        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }

  getBatchiWiseInventory(element: any) {
    let itemID = (this.InventoryForm.get('itemID') as FormControl).value;

    this.dialog.open(InventoryBatchwiseComponent, {
      width: '95vw', maxWidth: '95vw', height: '85vh',
      panelClass: 'batch-wise-dialog-panel', disableClose: true,
      data: {
        batchID: element.batchID,
        mergedBatchIDs: element.mergedBatchIDs ?? null,
        isMerged: element.isMerged ?? false,
        itemID: itemID ?? null,
        batchStatus: element.status ?? '',
        routeType: element.routeType ?? '',
        customerID: element.customerID ?? '',
      },
    });
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');
    return year + '-' + month + '-' + day;
  }

  openHelp(): void {
    this.dialog.open(InventoryHelpDialogComponent, { width: '90%', maxWidth: '1200px', maxHeight: '90vh' });
  }

  getInventory(resetPage: boolean = false) {
    let itemID = (this.InventoryForm.get('itemID') as FormControl).value;
    let startDate = (this.InventoryForm.get('startDate') as FormControl).value;
    let finishDate = (this.InventoryForm.get('finishDate') as FormControl).value;
    let status = (this.InventoryForm.get('status') as FormControl).value;
    let customerID = (this.InventoryForm.get('customerID') as FormControl).value;
    let stringFromDate = '';
    let stringToDate = '';

    if (resetPage) {
      this.pageNumber = 1;
      this.expandedBatchID = null;
      this.downloadBatches = [];
    }

    if (!customerID || customerID === '') {
      this.toast.warning({ detail: "INFO", summary: 'Please select a Customer', duration: 5000, position: 'topRight' });
      return;
    }

    if (status && status.toLocaleLowerCase() === 'select status') { status = ''; }

    if (startDate !== null) {
      stringFromDate = startDate.toLocaleString();
      if (stringFromDate.length > 10) { stringFromDate = this.getFormattedDate(startDate); }
    }

    if (finishDate !== null) {
      stringToDate = finishDate.toLocaleString();
      if (stringToDate.length > 10) { stringToDate = this.getFormattedDate(finishDate); }
    }

    this.isLoading = true;
    this.api.getInventory(itemID, stringFromDate, stringToDate, status, customerID, this.pageNumber, this.pageSize).subscribe({
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
