import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTabsModule } from '@angular/material/tabs';
import { ApiService } from '../services/api.service';
import { InventoryService } from '../services/inventory.service';
import { TranslateModule } from '@ngx-translate/core';

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
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTooltipModule,
    MatTabsModule,
    TranslateModule
  ],
})
export class DashboardComponent implements OnInit, OnDestroy {
  totalOrders = 0;
  customerStats: CustomerStat[] = [];
  statusStats: StatusStat[] = [];
  loading = true;
  private refreshInterval: any;

  // Status config for cards
  statusConfig: { [key: string]: { icon: string; color: string; bg: string } } = {
    'NEW': { icon: 'fiber_new', color: '#0d47a1', bg: '#e3f2fd' },
    'SYNCED': { icon: 'check_circle', color: '#2e7d32', bg: '#e8f5e9' },
    'SHIPPED': { icon: 'local_shipping', color: '#33691e', bg: '#f1f8e9' },
    'PROCESSED': { icon: 'done_all', color: '#1b5e20', bg: '#e8f5e9' },
    'ERROR': { icon: 'error', color: '#c62828', bg: '#ffebee' },
    'SYNCERROR': { icon: 'sync_problem', color: '#c62828', bg: '#ffebee' },
    'ACKERROR': { icon: 'report_problem', color: '#bf360c', bg: '#fbe9e7' },
    'ASNERROR': { icon: 'warning', color: '#e65100', bg: '#fff3e0' },
    'CANCELLED': { icon: 'cancel', color: '#4e342e', bg: '#efebe9' },
    'INPROGRESS': { icon: 'hourglass_empty', color: '#01579b', bg: '#e1f5fe' },
    'ACKNOWLEDGED': { icon: 'thumb_up', color: '#01579b', bg: '#e1f5fe' },
    'INVOICED': { icon: 'receipt', color: '#6a1b9a', bg: '#f3e5f5' },
    'ASNGEN': { icon: 'inventory', color: '#e65100', bg: '#fff3e0' },
    'ASNMARK': { icon: 'bookmark', color: '#37474f', bg: '#eceff1' },
    'DUPLICATE': { icon: 'content_copy', color: '#424242', bg: '#f5f5f5' },
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

  constructor(private api: ApiService, private inventoryApi: InventoryService) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadInventoryStats();
    this.refreshInterval = setInterval(() => {
      this.loadStats();
      this.loadInventoryStats();
    }, 900000); // 15 minutes
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
    this.loading = true;
    this.api.getDashboardStats().subscribe({
      next: (res: any) => {
        if (res.code === 200) {
          this.totalOrders = res.totalOrders || 0;
          this.customerStats = res.customerWise || [];
          this.statusStats = res.statusWise || [];
        }
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  getStatusIcon(status: string): string {
    return this.statusConfig[status]?.icon || 'info';
  }

  getStatusColor(status: string): string {
    return this.statusConfig[status]?.color || '#424242';
  }

  getStatusBg(status: string): string {
    return this.statusConfig[status]?.bg || '#f5f5f5';
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

  getErrorCount(): number {
    return this.statusStats
      .filter(s => ['ERROR', 'SYNCERROR', 'ACKERROR', 'ASNERROR'].includes(s.status))
      .reduce((sum, s) => sum + s.statusCount, 0);
  }

  getStatusCount(status: string): number {
    return this.statusStats.find(s => s.status === status)?.statusCount || 0;
  }
}
