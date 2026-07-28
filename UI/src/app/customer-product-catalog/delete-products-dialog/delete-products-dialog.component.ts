import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { NgToastService } from 'ng-angular-popup';

import { CustomerProductCatalogService } from '../../services/customerProductCatalogDialog.service';

@Component({
  selector: 'delete-products-dialog',
  templateUrl: './delete-products-dialog.component.html',
  styleUrls: ['./delete-products-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule, MatDialogModule, MatButtonModule, MatIconModule, MatTableModule,
    MatTooltipModule, MatProgressBarModule, TranslateModule
  ],
})
export class DeleteProductsDialogComponent {
  selectedFile: File | null = null;

  isLoading = false;
  isDeleting = false;

  // Filled by the preview call — deletion is only possible once this exists
  previewLoaded = false;
  found: any[] = [];
  notFound: string[] = [];
  totalItemIDs = 0;
  totalProducts = 0;

  confirmed = false;

  previewColumns: string[] = ['itemID', 'productCount', 'customers'];

  constructor(
    public dialogRef: MatDialogRef<DeleteProductsDialogComponent>,
    private api: CustomerProductCatalogService,
    private toast: NgToastService
  ) {}

  /**
   * Header only — never example item ids. A sample carrying real ids could be uploaded
   * as-is and delete products nobody meant to touch.
   */
  downloadSample(): void {
    const blob = new Blob(['ItemID\r\n'], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = 'Delete-Products-Sample.csv';
    link.click();
    URL.revokeObjectURL(link.href);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      this.resetPreview();
    }
  }

  clearFile(): void {
    this.selectedFile = null;
    this.resetPreview();
  }

  private resetPreview(): void {
    this.previewLoaded = false;
    this.found = [];
    this.notFound = [];
    this.totalItemIDs = 0;
    this.totalProducts = 0;
    this.confirmed = false;
  }

  loadPreview(): void {
    if (!this.selectedFile || this.isLoading) return;

    this.isLoading = true;

    this.api.previewDeleteProducts(this.selectedFile).subscribe({
      next: (res: any) => {
        this.isLoading = false;

        if (res.code !== 200) {
          this.toast.error({ detail: 'ERROR', summary: res.message, duration: 6000, position: 'topRight' });
          return;
        }

        this.found = res.found || [];
        this.notFound = res.notFound || [];
        this.totalItemIDs = res.totalItemIDs || 0;
        this.totalProducts = res.totalProducts || 0;
        this.previewLoaded = true;

        if (this.found.length === 0) {
          this.toast.info({ detail: 'INFO', summary: 'None of these Item IDs exist in the catalog.', duration: 6000, position: 'topRight' });
        }
      },
      error: (err: any) => {
        this.isLoading = false;
        this.toast.error({ detail: 'ERROR', summary: err.message, duration: 5000, position: 'topRight' });
      }
    });
  }

  get canDelete(): boolean {
    return this.previewLoaded && this.found.length > 0 && this.confirmed && !this.isDeleting;
  }

  onDelete(): void {
    if (!this.canDelete) return;

    this.isDeleting = true;
    const itemIDs = this.found.map(f => f.itemID);

    this.api.deleteProducts(itemIDs).subscribe({
      next: (res: any) => {
        this.isDeleting = false;

        if (res.code !== 200) {
          this.toast.error({ detail: 'ERROR', summary: res.message, duration: 6000, position: 'topRight' });
          return;
        }

        this.toast.success({ detail: 'SUCCESS', summary: res.message, duration: 6000, position: 'topRight' });
        this.dialogRef.close('deleted');
      },
      error: (err: any) => {
        this.isDeleting = false;
        this.toast.error({ detail: 'ERROR', summary: err.message, duration: 5000, position: 'topRight' });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
