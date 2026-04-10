import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ApiService } from '../services/api.service';
import { InventoryService } from '../services/inventory.service';
import { CustomerProductCatalogService } from '../services/customerProductCatalogDialog.service';
import { TranslateModule } from '@ngx-translate/core';
import { MatDialog } from '@angular/material/dialog';
import { OrdersDrilldownDialogComponent } from './orders-drilldown-dialog/orders-drilldown-dialog.component';

interface CustomerOption {
  erpCustomerID: string;
}

interface CustomerStat {
  customerName: string;
  erpCustomerID: string;
  orderCount: number;
}

interface StatusStat {
  status: string;
  statusCount: number;
}

interface InventoryCustomerStat {
  customerID: string;
  fullReceived: number;
  fullUploaded: number;
  diffReceived: number;
  partialUploaded: number;
  total: number;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTooltipModule,
    MatTabsModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    TranslateModule
  ],
})
export class DashboardComponent implements OnInit, OnDestroy {
  today = new Date();
  yesterday = new Date(new Date().setDate(new Date().getDate() - 1));
  filterFrom: Date = new Date(new Date().setDate(new Date().getDate() - 1));
  filterTo: Date = new Date();
  activePreset: string = '24h';
  expandedPartner: string | null = null;
  partnerStatuses: { [key: string]: StatusStat[] } = {};
  partnerStatusLoading: { [key: string]: boolean } = {};
  totalOrders = 0;
  customerStats: CustomerStat[] = [];
  statusStats: StatusStat[] = [];
  statusTiles: { key: string; label: string; icon: string; color: string; bg: string; count: number }[] = [];
  loading = true;
  refreshing = false;
  private refreshInterval: any;

  // Customer filter
  selectedCustomerIDs: string[] = [];
  customersOptions: CustomerOption[] = [];
  filteredCustomerOptions: CustomerOption[] = [];
  customerSearchText: string = '';
  customerDropdownOpen: boolean = false;

  // Status config for tiles (only statuses shown as KPI tiles)
  statusConfig: { [key: string]: { icon: string; color: string; bg: string } } = {
    'NEW': { icon: 'fiber_new', color: '#0d47a1', bg: '#e3f2fd' },
    'SYNCED': { icon: 'check_circle', color: '#2e7d32', bg: '#e8f5e9' },
    'SHIPPED': { icon: 'local_shipping', color: '#3f51b5', bg: '#e8eaf6' },
    'ERROR': { icon: 'warning_amber', color: '#c62828', bg: '#ffebee' },
    'ACKERROR': { icon: 'report_problem', color: '#bf360c', bg: '#fbe9e7' },
    'ASNERROR': { icon: 'warning_amber', color: '#e65100', bg: '#fff3e0' },
    'CANCELLED': { icon: 'cancel', color: '#4e342e', bg: '#efebe9' },
    'Partially Shipped': { icon: 'local_shipping', color: '#0277bd', bg: '#e1f5fe' },
    'Partially Cancelled': { icon: 'cancel', color: '#d84315', bg: '#fbe9e7' },
  };

  // Full status config (used for partner breakdown icons/colors)
  allStatusConfig: { [key: string]: { icon: string; color: string; bg: string } } = {
    ...{
      'NEW': { icon: 'fiber_new', color: '#0d47a1', bg: '#e3f2fd' },
      'SYNCED': { icon: 'check_circle', color: '#2e7d32', bg: '#e8f5e9' },
      'SHIPPED': { icon: 'local_shipping', color: '#3f51b5', bg: '#e8eaf6' },
      'PROCESSED': { icon: 'done_all', color: '#1b5e20', bg: '#e8f5e9' },
      'ERROR': { icon: 'warning_amber', color: '#c62828', bg: '#ffebee' },
      'SYNCERROR': { icon: 'sync_problem', color: '#c62828', bg: '#ffebee' },
      'ACKERROR': { icon: 'report_problem', color: '#bf360c', bg: '#fbe9e7' },
      'ASNERROR': { icon: 'warning_amber', color: '#e65100', bg: '#fff3e0' },
      'CANCELLED': { icon: 'cancel', color: '#4e342e', bg: '#efebe9' },
      'INPROGRESS': { icon: 'hourglass_empty', color: '#01579b', bg: '#e1f5fe' },
      'ACKNOWLEDGED': { icon: 'thumb_up', color: '#01579b', bg: '#e1f5fe' },
      'INVOICED': { icon: 'receipt', color: '#6a1b9a', bg: '#f3e5f5' },
      'ASNGEN': { icon: 'inventory', color: '#e65100', bg: '#fff3e0' },
      'ASNMARK': { icon: 'bookmark', color: '#37474f', bg: '#eceff1' },
      'DUPLICATE': { icon: 'content_copy', color: '#424242', bg: '#f5f5f5' },
      'Partially Shipped': { icon: 'local_shipping', color: '#0277bd', bg: '#e1f5fe' },
      'Partially Cancelled': { icon: 'cancel', color: '#d84315', bg: '#fbe9e7' },
    }
  };

  // Inventory stats
  totalBatches = 0;
  totalDownloadBatches = 0;
  totalUploadBatches = 0;
  totalDiffDownloadBatches = 0;
  totalDiffUploadBatches = 0;
  inventoryProcessing = 0;
  inventoryCompleted = 0;
  inventoryError = 0;
  inventoryLoading = true;
  inventoryCustomerStats: InventoryCustomerStat[] = [];

  customerColumns = ['customerName', 'erpCustomerID', 'orderCount'];

  constructor(private api: ApiService, private inventoryApi: InventoryService, private customerApi: CustomerProductCatalogService, private dialog: MatDialog) {}

  ngOnInit(): void {
    this.loadCustomers();
    this.loadStats();
    this.loadInventoryStats();
    this.refreshInterval = setInterval(() => {
      this.loadStats();
      this.loadInventoryStats();
    }, 900000); // 15 minutes
  }

  loadCustomers(): void {
    this.customerApi.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customersOptions = res.customers || [];
        this.filteredCustomerOptions = this.customersOptions;
      }
    });
  }

  filterCustomerOptions(): void {
    const search = (this.customerSearchText || '').toLowerCase();
    this.filteredCustomerOptions = this.customersOptions.filter(c =>
      c.erpCustomerID.toLowerCase().includes(search)
    );
  }

  onCustomerSelectOpened(opened: boolean): void {
    if (opened) {
      this.customerSearchText = '';
      this.filteredCustomerOptions = this.customersOptions;
    }
  }

  onCustomerFilterChange(): void {
    this.applyDateFilter();
  }

  toggleCustomer(id: string): void {
    const idx = this.selectedCustomerIDs.indexOf(id);
    if (idx >= 0) {
      this.selectedCustomerIDs = this.selectedCustomerIDs.filter(c => c !== id);
    } else {
      this.selectedCustomerIDs = [...this.selectedCustomerIDs, id];
    }
    this.applyDateFilter();
  }

  isCustomerSelected(id: string): boolean {
    return this.selectedCustomerIDs.includes(id);
  }

  removeCustomer(id: string): void {
    this.selectedCustomerIDs = this.selectedCustomerIDs.filter(c => c !== id);
    this.applyDateFilter();
  }

  clearAllCustomers(): void {
    this.selectedCustomerIDs = [];
    this.applyDateFilter();
  }

  manualRefresh(): void {
    this.loadStats();
    this.loadInventoryStats();
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  loadStats(): void {
    // First load: show full loading screen. Subsequent: show subtle spinner overlay
    const isInitial = this.loading && this.customerStats.length === 0;
    if (isInitial) {
      this.loading = true;
    } else {
      this.refreshing = true;
    }

    const from = this.formatDate(this.filterFrom);
    const to = this.formatDate(this.filterTo);
    const customerFilter = this.selectedCustomerIDs.length > 0 ? this.selectedCustomerIDs.join(',') : '';
    this.api.getDashboardStats(from, to, customerFilter).subscribe({
      next: (res: any) => {
        if (res.code === 200) {
          this.totalOrders = res.totalOrders || 0;
          this.customerStats = res.customerWise || [];
          this.statusStats = res.statusWise || [];

          // Load partner-wise status breakdown from same response
          this.partnerStatuses = {};
          if (res.partnerStatusWise) {
            for (const key of Object.keys(res.partnerStatusWise)) {
              this.partnerStatuses[key] = res.partnerStatusWise[key] || [];
            }
          }
        }
        this.buildStatusTiles();
        this.loading = false;
        this.refreshing = false;
      },
      error: () => {
        this.buildStatusTiles();
        this.loading = false;
        this.refreshing = false;
      }
    });
  }

  setPreset(preset: string): void {
    this.activePreset = preset;
    const now = new Date();
    this.filterTo = new Date(now);

    switch (preset) {
      case '24h':
        this.filterFrom = new Date(new Date().setDate(now.getDate() - 1));
        break;
      case '7d':
        this.filterFrom = new Date(new Date().setDate(now.getDate() - 7));
        break;
      case '30d':
        this.filterFrom = new Date(new Date().setDate(now.getDate() - 30));
        break;
    }

    this.applyDateFilter();
  }

  onCustomDate(): void {
    this.activePreset = 'custom';
  }

  onRangeClose(): void {
    if (this.filterFrom && this.filterTo) {
      this.activePreset = 'custom';
      this.applyDateFilter();
    }
  }

  applyDateFilter(): void {
    this.expandedPartner = null;
    this.partnerStatuses = {};
    this.loadStats();
    this.loadInventoryStats();
  }

  formatDate(date: Date): string {
    if (!date) return '';
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
  }

  getStatusConfig(status: string): { icon: string; color: string; bg: string } {
    // Case-insensitive lookup: try exact match first, then uppercase match
    if (this.allStatusConfig[status]) return this.allStatusConfig[status];
    const key = Object.keys(this.allStatusConfig).find(k => k.toUpperCase() === (status || '').toUpperCase());
    return key ? this.allStatusConfig[key] : { icon: 'info', color: '#424242', bg: '#f5f5f5' };
  }

  getStatusIcon(status: string): string {
    return this.getStatusConfig(status).icon;
  }

  getStatusColor(status: string): string {
    return this.getStatusConfig(status).color;
  }

  getStatusBg(status: string): string {
    return this.getStatusConfig(status).bg;
  }

  loadInventoryStats(): void {
    this.inventoryLoading = true;
    const now = new Date();
    const yesterday = new Date(now);
    yesterday.setDate(yesterday.getDate() - 1);
    const pad = (n: number) => n.toString().padStart(2, '0');
    const fromDate = `${yesterday.getFullYear()}-${pad(yesterday.getMonth() + 1)}-${pad(yesterday.getDate())}`;
    const toDate = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;

    this.inventoryApi.getInventory('EMPTY', fromDate, toDate, 'EMPTY', 'EMPTY', 'EMPTY').subscribe({
      next: (res: any) => {
        const batches = res.inventory || [];
        this.totalBatches = batches.length;
        this.inventoryProcessing = batches.filter((b: any) => b.status === 'PROCESSING').length;
        this.inventoryCompleted = batches.filter((b: any) => b.status === 'COMPLETED').length;
        this.inventoryError = batches.filter((b: any) => b.status === 'ERROR').length;

        // Customer-wise breakdown with time-based Full Feed Upload detection
        // Logic: Sort by time. After a Full Feed Download, the NEXT upload = Full Feed Upload.
        // All other uploads = Partial Upload (from differential updates).
        const sortedBatches = [...batches].sort((a: any, b: any) =>
          new Date(a.startDate || a.createdDate || 0).getTime() - new Date(b.startDate || b.createdDate || 0).getTime()
        );

        // Route type classification — handles both enum names AND numeric IDs
        const isFullDownload = (rt: string) => {
          const v = rt.toLowerCase().trim();
          return v === 'scsfullinventoryfeed' || v === 'inventoryfeed'
            || v === '7' || v === '1'
            || v.includes('full') && !v.includes('upload');
        };

        const isDiffDownload = (rt: string) => {
          const v = rt.toLowerCase().trim();
          return v === 'scsdifferentialinventoryfeed'
            || v === '8'
            || v.includes('differential') && !v.includes('upload');
        };

        const customerMap = new Map<string, InventoryCustomerStat>();
        const customerFullFeedPending = new Map<string, boolean>();

        sortedBatches.forEach((b: any) => {
          const cid = b.customerID || 'Unknown';
          if (!customerMap.has(cid)) {
            customerMap.set(cid, { customerID: cid, fullReceived: 0, fullUploaded: 0, diffReceived: 0, partialUploaded: 0, total: 0 });
          }
          const stat = customerMap.get(cid)!;
          stat.total++;
          const rt = (b.routeType || '').toString();

          if (isFullDownload(rt)) {
            stat.fullReceived++;
            customerFullFeedPending.set(cid, true);
          } else if (isDiffDownload(rt)) {
            stat.diffReceived++;
          } else {
            // Upload
            if (customerFullFeedPending.get(cid)) {
              stat.fullUploaded++;
              customerFullFeedPending.set(cid, false);
            } else {
              stat.partialUploaded++;
            }
          }
        });
        this.inventoryCustomerStats = Array.from(customerMap.values()).sort((a, b) => b.total - a.total);

        // Calculate totals
        this.totalDownloadBatches = this.inventoryCustomerStats.reduce((s, c) => s + c.fullReceived, 0);
        this.totalUploadBatches = this.inventoryCustomerStats.reduce((s, c) => s + c.fullUploaded, 0);
        this.totalDiffDownloadBatches = this.inventoryCustomerStats.reduce((s, c) => s + c.diffReceived, 0);
        this.totalDiffUploadBatches = this.inventoryCustomerStats.reduce((s, c) => s + c.partialUploaded, 0);

        this.inventoryLoading = false;
      },
      error: () => {
        this.inventoryLoading = false;
      }
    });
  }

  getPartnerName(customerID: string): string {
    const map: { [key: string]: string } = {
      'AMA1005': 'Amazon', 'KNO8068': 'Knot', 'LOW2221MP': 'Lowes',
      'MAC0149M': 'Macys', 'MIC1300MP': 'Michaels',
      'TAR6266P': 'Target', 'TAR6266PAH': 'Target SEI', 'WAL4001MP': 'Walmart'
    };
    return map[customerID] || customerID;
  }

  buildStatusTiles(): void {
    // Always show the same statusConfig statuses, with count from API or 0
    // Case-insensitive match to handle DB returning 'New' vs config 'NEW'
    this.statusTiles = Object.keys(this.statusConfig).map(key => {
      const config = this.statusConfig[key];
      const label = key;
      const keyUpper = key.toUpperCase();
      const found = this.statusStats.find(s => (s.status || '').toUpperCase() === keyUpper);
      return {
        key,
        label,
        icon: config.icon,
        color: config.color,
        bg: config.bg,
        count: found ? found.statusCount : 0
      };
    });
  }

  getPartnerAllStatuses(erpCustomerID: string): StatusStat[] {
    // Always show the same statusConfig statuses per partner, with count or 0
    // Case-insensitive match
    const existing = this.partnerStatuses[erpCustomerID] || [];
    return Object.keys(this.statusConfig).map(key => {
      const keyUpper = key.toUpperCase();
      const found = existing.find(s => (s.status || '').toUpperCase() === keyUpper);
      return { status: key, statusCount: found ? found.statusCount : 0 };
    });
  }

  openStatusDrilldown(status: string, customerID: string = ''): void {
    const config = this.getStatusConfig(status);
    const label = status;
    const partnerLabel = customerID ? ` — ${customerID}` : '';

    this.dialog.open(OrdersDrilldownDialogComponent, {
      width: '85%',
      maxWidth: '1000px',
      maxHeight: '85vh',
      disableClose: false,
      data: {
        status: status,
        customerID: customerID,
        fromDate: this.formatDate(this.filterFrom),
        toDate: this.formatDate(this.filterTo),
        title: `${label} Orders${partnerLabel}`,
        color: config.color,
        icon: config.icon
      }
    });
  }

  openTotalDrilldown(customerID: string = ''): void {
    const partnerLabel = customerID ? ` — ${customerID}` : '';
    this.dialog.open(OrdersDrilldownDialogComponent, {
      width: '85%',
      maxWidth: '1000px',
      maxHeight: '85vh',
      disableClose: false,
      data: {
        status: '',
        customerID: customerID,
        fromDate: this.formatDate(this.filterFrom),
        toDate: this.formatDate(this.filterTo),
        title: `All Orders${partnerLabel}`,
        color: '#3f51b5',
        icon: 'shopping_cart'
      }
    });
  }

  getErrorCount(): number {
    return this.statusStats
      .filter(s => ['ERROR', 'SYNCERROR', 'ACKERROR', 'ASNERROR'].includes(s.status))
      .reduce((sum, s) => sum + s.statusCount, 0);
  }

  getStatusCount(status: string): number {
    return this.statusStats.find(s => s.status === status)?.statusCount || 0;
  }

  togglePartnerDetail(erpCustomerID: string): void {
    this.expandedPartner = this.expandedPartner === erpCustomerID ? null : erpCustomerID;
  }

  getPartnerStatusTotal(erpCustomerID: string): number {
    return (this.partnerStatuses[erpCustomerID] || []).reduce((sum: number, s: StatusStat) => sum + s.statusCount, 0);
  }
}
