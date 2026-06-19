import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { ProductUploadPricesService } from '../../services/ProductUploadPrices.service';
import { FileContentViewerDialogComponent } from '../../file-content-viewer-dialog/file-content-viewer-dialog.component';

@Component({
  selector: 'price-feed-log-dialog',
  templateUrl: './price-feed-log-dialog.component.html',
  styleUrls: ['./price-feed-log-dialog.component.scss'],
  standalone: true,
  imports: [CommonModule, DatePipe, MatButtonModule, MatIconModule, MatProgressBarModule, MatTableModule, MatTooltipModule, MatPaginatorModule],
})
export class PriceFeedLogDialogComponent implements OnInit {
  isLoading = false;
  entries: any[] = [];
  title = 'Price Feed Log';
  mode: string = 'product';

  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;

  columns: string[] = ['Actions', 'Type', 'ProductID', 'ActivityDate'];

  constructor(
    public dialogRef: MatDialogRef<PriceFeedLogDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { customerID: string; itemID: string; mode?: 'product' | 'promo' },
    private uploadApi: ProductUploadPricesService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.mode = this.data.mode === 'promo' ? 'promo' : 'product';
    this.title = this.mode === 'promo' ? 'Promotion Feed Log' : 'Price Feed Log';
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.uploadApi.getProductPriceFeedLog(this.data.customerID, this.data.itemID, this.mode, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        if (res.code === 200) {
          this.entries = res.entries || [];
          this.totalCount = res.totalCount || 0;
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }

  getTypeLabel(type: string): string {
    if (!type) return '';
    if (type.endsWith('-SNT')) return 'Request Sent';
    if (type.endsWith('-RVD')) return 'Response Received';
    return type;
  }

  isSent(type: string): boolean {
    return !!type && type.endsWith('-SNT');
  }

  viewData(entry: any): void {
    let parsed: any = entry?.data;
    let type = 'txt';
    try {
      parsed = JSON.parse(entry.data);
      type = 'json';
    } catch {
      // keep raw text
    }

    this.dialog.open(FileContentViewerDialogComponent, {
      data: { content: parsed, type: type, routeName: this.title },
      width: '800px',
    });
  }

  downloadData(entry: any): void {
    const blob = new Blob([entry?.data || ''], { type: 'text/plain' });
    const link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = `${this.data.customerID}_${this.data.itemID}_${entry?.type}.json`;
    link.click();
  }

  onClose(): void {
    this.dialogRef.close();
  }
}
