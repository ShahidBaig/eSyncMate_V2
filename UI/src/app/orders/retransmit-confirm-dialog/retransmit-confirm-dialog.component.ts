import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'retransmit-confirm-dialog',
    standalone: true,
    imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
    template: `
        <div class="confirm-dialog">
            <div class="confirm-icon-container">
                <mat-icon class="confirm-icon">forward_to_inbox</mat-icon>
            </div>
            <h2 class="confirm-title">Re-Transmit The ASN</h2>
            <p class="confirm-message">
                Re-transmit the ASN for order <strong>{{ data.orderNumber }}</strong>?
            </p>
            <p class="confirm-details">
                The shipment (ASN) data is fetched from the ERP again and then re-sent to the
                marketplace. The order status stays <strong>Shipped</strong>.
            </p>
            <div class="confirm-actions">
                <button mat-button class="cancel-btn" (click)="onCancel()">Cancel</button>
                <button mat-raised-button class="retransmit-btn" (click)="onConfirm()">
                    <mat-icon>forward_to_inbox</mat-icon>
                    Re-Transmit
                </button>
            </div>
        </div>
    `,
    styles: [`
        .confirm-dialog { padding: 24px; text-align: center; max-width: 440px; }
        .confirm-icon-container { display: flex; justify-content: center; margin-bottom: 16px; }
        .confirm-icon {
            font-size: 56px; width: 56px; height: 56px;
            color: #0d9488; background: #f0fdfa; border-radius: 50%;
            padding: 12px; box-sizing: content-box;
        }
        .confirm-title { margin: 0 0 8px; font-size: 20px; font-weight: 600; color: #333; }
        .confirm-message { margin: 0 0 8px; font-size: 15px; color: #555; }
        .confirm-details { margin: 0 0 24px; font-size: 13px; color: #888; line-height: 1.5; }
        .confirm-actions { display: flex; justify-content: center; gap: 12px; }
        .cancel-btn { color: #666; min-width: 100px; }
        .retransmit-btn {
            display: flex; align-items: center; gap: 4px; min-width: 150px;
            background: #0d9488 !important; color: #fff !important;
            &:hover { background: #0f766e !important; }
        }
    `]
})
export class ReTransmitConfirmDialogComponent {
    constructor(
        public dialogRef: MatDialogRef<ReTransmitConfirmDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: { orderNumber: string }
    ) {}

    onCancel(): void { this.dialogRef.close(false); }
    onConfirm(): void { this.dialogRef.close(true); }
}
