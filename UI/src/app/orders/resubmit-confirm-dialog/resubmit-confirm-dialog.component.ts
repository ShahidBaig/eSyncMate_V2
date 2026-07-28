import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'resubmit-confirm-dialog',
    standalone: true,
    imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
    template: `
        <div class="confirm-dialog">
            <div class="confirm-icon-container">
                <mat-icon class="confirm-icon">report_problem</mat-icon>
            </div>
            <h2 class="confirm-title">Resubmit Order to ERP</h2>
            <p class="confirm-message">
                Post order <strong>{{ data.orderNumber }}</strong> to the ERP again?
            </p>
            <p class="confirm-details">
                The ERP is checked first. The order is posted again <strong>only if it does not exist
                in SPARS, or exists with a Cancelled / Void status</strong>. Previous logs are kept.
            </p>
            <div class="confirm-actions">
                <button mat-button class="cancel-btn" (click)="onCancel()">Cancel</button>
                <button mat-raised-button class="resubmit-btn" (click)="onConfirm()">
                    <mat-icon>cloud_sync</mat-icon>
                    Resubmit
                </button>
            </div>
        </div>
    `,
    styles: [`
        .confirm-dialog { padding: 24px; text-align: center; max-width: 440px; }
        .confirm-icon-container { display: flex; justify-content: center; margin-bottom: 16px; }
        .confirm-icon {
            font-size: 56px; width: 56px; height: 56px;
            color: #E8834A; background: #fff7ed; border-radius: 50%;
            padding: 12px; box-sizing: content-box;
        }
        .confirm-title { margin: 0 0 8px; font-size: 20px; font-weight: 600; color: #333; }
        .confirm-message { margin: 0 0 8px; font-size: 15px; color: #555; }
        .confirm-details { margin: 0 0 24px; font-size: 13px; color: #888; line-height: 1.5; }
        .confirm-actions { display: flex; justify-content: center; gap: 12px; }
        .cancel-btn { color: #666; min-width: 100px; }
        .resubmit-btn {
            display: flex; align-items: center; gap: 4px; min-width: 140px;
            background: #0891b2 !important; color: #fff !important;
            &:hover { background: #0e7490 !important; }
        }
    `]
})
export class ResubmitConfirmDialogComponent {
    constructor(
        public dialogRef: MatDialogRef<ResubmitConfirmDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: { orderNumber: string }
    ) {}

    onCancel(): void { this.dialogRef.close(false); }
    onConfirm(): void { this.dialogRef.close(true); }
}
