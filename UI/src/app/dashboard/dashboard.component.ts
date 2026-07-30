import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
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
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MatDialog } from '@angular/material/dialog';
import { OrdersDrilldownDialogComponent } from './orders-drilldown-dialog/orders-drilldown-dialog.component';
import { DashboardHelpDialogComponent } from './dashboard-help-dialog/dashboard-help-dialog.component';

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
  lastOrderDate?: string | null;
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
  filterFrom: Date = new Date();
  filterTo: Date = new Date();
  activePreset: string = 'today';
  expandedPartner: string | null = null;
  partnerStatuses: { [key: string]: StatusStat[] } = {};
  // Stable per-partner tile lists — see getPartnerAllStatuses(). Cleared whenever
  // partnerStatuses is replaced so the tiles pick up fresh counts/dates.
  private partnerAllStatuses: { [key: string]: StatusStat[] } = {};
  partnerStatusLoading: { [key: string]: boolean } = {};
  totalOrders = 0;
  totalLastOrderDate: string | null = null;
  customerStats: CustomerStat[] = [];
  private rawCustomerWise: CustomerStat[] = [];
  statusStats: StatusStat[] = [];
  statusTiles: { key: string; label: string; icon: string; color: string; bg: string; count: number; lastOrderDate?: string | null }[] = [];
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

  constructor(private api: ApiService, private inventoryApi: InventoryService, private customerApi: CustomerProductCatalogService, private dialog: MatDialog, private translate: TranslateService) {}

  /**
   * Tooltip for the "last order" date on a KPI tile.
   * Built as heading / scope / timestamp / note lines — the tooltip class renders
   * them as a card with the first line styled as the heading.
   */
  getLastOrderTooltip(scope: string, date: string | null | undefined): string {
    // No date on the tile => no tooltip.
    if (!date) return '';

    // This runs on every change-detection pass. formatDate() throws on an
    // unparseable value, and a throw here would break the whole tile view
    // (tooltip AND click), so never let it escape.
    let when: string;
    try {
      when = formatDate(date, 'MM/dd/yyyy hh:mm a', 'en-US');
    } catch {
      return '';
    }

    const heading = this.translate.instant('dashboard.lastOrderReceived');
    const note = this.translate.instant('dashboard.lastOrderNote');

    return [heading, scope, when, note].filter(l => !!l).join('\n');
  }

  /**
   * Date shown on a tile footer. Same reason as getLastOrderTooltip: the `date`
   * pipe throws on an unparseable value and would take the tile's click handler
   * down with it, so format defensively and just render nothing on failure.
   */
  formatTileDate(date: string | null | undefined): string {
    if (!date) return '';
    try {
      return formatDate(date, 'MM/dd/yyyy hh:mm a', 'en-US');
    } catch {
      return '';
    }
  }

  /** Colour variant for the tooltip card — matches the tile's status colour. */
  getLastOrderTooltipClass(status: string): string {
    const slug = (status || '').toString().trim().toLowerCase().replace(/\s+/g, '-');
    return `lastorder-tooltip lastorder-tooltip--${slug || 'total'}`;
  }

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
        // Hide internal (non-trading-partner) customers
        this.customersOptions = (res.customers || []).filter((c: CustomerOption) => !this.isExcludedCustomer(c.erpCustomerID));
        this.filteredCustomerOptions = this.customersOptions;
        // Rebuild partner list so all customers appear (even with 0 orders)
        this.buildCustomerStats();
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

  openHelp(): void {
    this.dialog.open(DashboardHelpDialogComponent, { width: '90%', maxWidth: '1200px', maxHeight: '90vh' });
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
          this.totalLastOrderDate = res.totalLastOrderDate || null;
          this.rawCustomerWise = res.customerWise || [];
          this.buildCustomerStats();
          this.statusStats = res.statusWise || [];

          // Load partner-wise status breakdown from same response
          this.partnerStatuses = {};
          this.partnerAllStatuses = {};
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
      case 'today':
        this.filterFrom = new Date(now);
        break;
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
    this.partnerAllStatuses = {};
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
    const fromDate = this.formatDate(this.filterFrom);
    const toDate = this.formatDate(this.filterTo);

    this.inventoryApi.getInventory('', fromDate, toDate, '', '').subscribe({
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

  // Internal (non-trading-partner) customers hidden from the dashboard
  private excludedCustomerIDs = ['esyncmate', 'spars customer'];

  private isExcludedCustomer(id: string): boolean {
    return this.excludedCustomerIDs.includes((id || '').toLowerCase().trim());
  }

  buildCustomerStats(): void {
    // Map order counts from API by customer ID
    const countMap = new Map<string, CustomerStat>();
    this.rawCustomerWise.forEach(s => {
      if (s.erpCustomerID) countMap.set(s.erpCustomerID, s);
    });

    // Base list: selected customers when filtered, otherwise all customers
    const baseIds = (this.selectedCustomerIDs.length > 0
      ? this.selectedCustomerIDs
      : this.customersOptions.map(c => c.erpCustomerID))
      .filter(id => !!id && !this.isExcludedCustomer(id));

    let merged: CustomerStat[];
    if (baseIds.length > 0) {
      // Show every customer — with its order count or 0 when no orders match the criteria
      merged = baseIds.map(id => {
        const found = countMap.get(id);
        return found ? found : { customerName: id, erpCustomerID: id, orderCount: 0 };
      });
    } else {
      // Customers not loaded yet — fall back to API result as-is
      merged = this.rawCustomerWise.filter(s => !this.isExcludedCustomer(s.erpCustomerID));
    }

    merged.sort((a, b) => b.orderCount - a.orderCount);
    this.customerStats = merged;
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
        count: found ? found.statusCount : 0,
        lastOrderDate: found ? found.lastOrderDate ?? null : null
      };
    });
  }

  /**
   * Called straight from *ngFor, so it MUST return the same array instance for a
   * given partner until the data actually changes. Returning a freshly built array
   * makes ngFor (which tracks by object identity) tear down and rebuild every tile
   * on each change-detection pass — and since mouse events trigger change detection,
   * the tiles get replaced mid-interaction: hover never settles so the tooltip never
   * shows, and mousedown/mouseup land on different elements so no click ever fires.
   */
  getPartnerAllStatuses(erpCustomerID: string): StatusStat[] {
    const cached = this.partnerAllStatuses[erpCustomerID];
    if (cached) return cached;

    const built = this.buildPartnerStatusList(erpCustomerID);
    this.partnerAllStatuses[erpCustomerID] = built;
    return built;
  }

  trackByStatus(_index: number, stat: StatusStat): string {
    return stat.status;
  }

  private buildPartnerStatusList(erpCustomerID: string): StatusStat[] {
    // Always show the same statusConfig statuses per partner, with count or 0
    // Case-insensitive match
    const existing = this.partnerStatuses[erpCustomerID] || [];
    return Object.keys(this.statusConfig).map(key => {
      const keyUpper = key.toUpperCase();
      const found = existing.find(s => (s.status || '').toUpperCase() === keyUpper);
      return { status: key, statusCount: found ? found.statusCount : 0, lastOrderDate: found ? found.lastOrderDate ?? null : null };
    });
  }

  openStatusDrilldown(status: string, customerID: string = ''): void {
    const config = this.getStatusConfig(status);
    const label = status;
    const partnerLabel = customerID ? ` — ${customerID}` : '';

    this.dialog.open(OrdersDrilldownDialogComponent, {
      width: '92%',
      maxWidth: '1550px',
      maxHeight: '94vh',
      disableClose: false,
      panelClass: 'elevated-dialog-panel',
      backdropClass: 'elevated-dialog-backdrop',
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
      width: '92%',
      maxWidth: '1550px',
      maxHeight: '94vh',
      disableClose: false,
      panelClass: 'elevated-dialog-panel',
      backdropClass: 'elevated-dialog-backdrop',
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
