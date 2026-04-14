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

  columns: string[] = ['Expand', 'CustomerID', 'Status', 'StartDate', 'FinishDate', 'File'];

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
      // Collapse
      this.expandedBatchID = null;
      this.downloadBatches = [];
      return;
    }

    // Expand — find previous upload's start date
    this.expandedBatchID = element.batchID;
    this.downloadBatches = [];
    this.downloadLoading = true;

    const currentIndex = this.inventoryToDisplay.findIndex((r: any) => r.batchID === element.batchID);
    const prevUpload: any = currentIndex < this.inventoryToDisplay.length - 1
      ? this.inventoryToDisplay[currentIndex + 1]
      : null;

    const fromDate = prevUpload ? this.formatDateISO(prevUpload.startDate || prevUpload.StartDate) : '2000-01-01';
    const toDate = this.formatDateISO(element.startDate || element.StartDate);
    const customerID = element.customerID || element.CustomerID;

    // Store prev date for display
    element._prevUploadDate = prevUpload ? (prevUpload.startDate || prevUpload.StartDate) : null;

    this.api.getDownloadBatches(customerID, fromDate, toDate).subscribe({
      next: (res: any) => {
        const rawBatches = res.inventory || [];
        this.downloadBatches = this.buildMergedDownloadRow(rawBatches, customerID);
        this.downloadLoading = false;
      },
      error: () => {
        this.downloadBatches = [];
        this.downloadLoading = false;
      }
    });
  }

  /**
   * Collapse multiple download batches into ONE merged row.
   * - earliest StartDate, latest FinishDate
   * - status hardcoded to 'Completed' (no merging of statuses)
   * - keeps all underlying batchIDs for item-level merge
   */
  private buildMergedDownloadRow(rawBatches: any[], customerID: string): any[] {
    if (!rawBatches || rawBatches.length === 0) return [];

    const sorted = [...rawBatches].sort((a, b) => {
      const da = new Date(a.startDate || a.StartDate || 0).getTime();
      const db = new Date(b.startDate || b.StartDate || 0).getTime();
      return da - db;
    });

    const first = sorted[0];
    const last = sorted[sorted.length - 1];
    const batchIDs = sorted.map((b: any) => b.batchID || b.BatchID).filter((x: any) => !!x);

    // Build a meaningful label describing the consolidated inventory snapshot
    const typeSet = new Set<string>(sorted.map((b: any) => (b.routeType || b.RouteType || '').toString()));
    const typeLabel = typeSet.size === 1
      ? Array.from(typeSet)[0]
      : `Latest Inventory Snapshot (consolidated from ${sorted.length} feeds)`;

    // Per-type breakdown with friendly display names
    // (e.g. SCSFullInventoryFeed → "Full Inventory Feed Received")
    const typeCounts = new Map<string, number>();
    for (const b of sorted) {
      const t = (b.routeType || b.RouteType || 'Unknown').toString();
      typeCounts.set(t, (typeCounts.get(t) || 0) + 1);
    }
    const typeBreakdown = Array.from(typeCounts.entries()).map(([type, count]) => ({
      type,
      displayName: this.getFeedDisplayName(type),
      count
    }));

    return [{
      batchID: batchIDs[batchIDs.length - 1], // representative ID for any single-batch fallback
      mergedBatchIDs: batchIDs,
      typeBreakdown: typeBreakdown,
      isMerged: true,
      mergedCount: sorted.length,
      routeType: typeLabel,
      status: 'Completed', // hardcoded — no status merging
      startDate: first.startDate || first.StartDate,
      finishDate: last.finishDate || last.FinishDate,
      customerID: customerID
    }];
  }

  /**
   * Map internal route type names to friendly display names for the
   * consolidated feed breakdown chips.
   */
  private getFeedDisplayName(routeType: string): string {
    const t = (routeType || '').toLowerCase();
    if (t.includes('full')) return 'Full Inventory Feed Received';
    if (t.includes('differential')) return 'Differential Inventory Feed Received';
    if (t.includes('portal')) return 'Portal Inventory Feed Received';
    return routeType; // fallback — show raw name
  }

  private formatDateISO(date: any): string {
    if (!date) return '';
    const d = new Date(date);
    // Format as local time (YYYY-MM-DDTHH:mm:ss) — do NOT convert to UTC
    // The DB stores dates in local time, so we must send local time not UTC
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
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

    if (itemID == '') { itemID = 'EMPTY' }
    if (status == '' || status.toLocaleLowerCase() == 'select status') { status = 'EMPTY' }
    if (customerID == '') { customerID = 'EMPTY' }

    // Route type fixed to upload types only
    let routeType = 'EMPTY';

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
