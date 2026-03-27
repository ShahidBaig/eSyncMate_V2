import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'confirm-delete-dialog',
    standalone: true,
    imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
    template: `
        <div class="confirm-dialog">
            <div class="confirm-icon-container">
                <mat-icon class="confirm-icon">warning_amber</mat-icon>
            </div>
            <h2 class="confirm-title">Delete Flow</h2>
            <p class="confirm-message">
                Are you sure you want to delete <strong>"{{ data.title }}"</strong>?
            </p>
            <p class="confirm-details">
                This flow and its <strong>{{ data.routeCount }}</strong> configured route(s) will be removed from the list.
            </p>
            <div class="confirm-actions">
                <button mat-button class="cancel-btn" (click)="onCancel()">
                    Cancel
                </button>
                <button mat-raised-button color="warn" class="delete-btn" (click)="onConfirm()">
                    <mat-icon>delete</mat-icon>
                    Delete Flow
                </button>
            </div>
        </div>
    `,
    styles: [`
        .confirm-dialog {
            padding: 24px;
            text-align: center;
            max-width: 420px;
        }

        .confirm-icon-container {
            display: flex;
            justify-content: center;
            margin-bottom: 16px;
        }

        .confirm-icon {
            font-size: 56px;
            width: 56px;
            height: 56px;
            color: #f44336;
            background: #ffebee;
            border-radius: 50%;
            padding: 12px;
            box-sizing: content-box;
        }

        .confirm-title {
            margin: 0 0 8px;
            font-size: 20px;
            font-weight: 600;
            color: #333;
        }

        .confirm-message {
            margin: 0 0 8px;
            font-size: 15px;
            color: #555;
        }

        .confirm-details {
            margin: 0 0 24px;
            font-size: 13px;
            color: #888;
            line-height: 1.5;
        }

        .confirm-actions {
            display: flex;
            justify-content: center;
            gap: 12px;
        }

        .cancel-btn {
            color: #666;
            min-width: 100px;
        }

        .delete-btn {
            display: flex;
            align-items: center;
            gap: 4px;
            min-width: 140px;
        }
    `]
})
export class ConfirmDeleteDialogComponent {
    constructor(
        public dialogRef: MatDialogRef<ConfirmDeleteDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: { title: string; routeCount: number }
    ) {}

    onCancel(): void {
        this.dialogRef.close(false);
    }

    onConfirm(): void {
        this.dialogRef.close(true);
    }
}
