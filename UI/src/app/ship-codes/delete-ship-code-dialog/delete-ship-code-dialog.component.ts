import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { NgToastService } from 'ng-angular-popup';

import { ShipCodesService } from '../../services/ship-codes.service';

@Component({
  selector: 'delete-ship-code-dialog',
  templateUrl: './delete-ship-code-dialog.component.html',
  styleUrls: ['./delete-ship-code-dialog.component.scss'],
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, TranslateModule],
})
export class DeleteShipCodeDialogComponent {
  isDeleting = false;

  constructor(
    public dialogRef: MatDialogRef<DeleteShipCodeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { shipCode: any },
    private api: ShipCodesService,
    private toast: NgToastService
  ) { }

  onCancel(): void {
    this.dialogRef.close();
  }

  onDelete(): void {
    if (this.isDeleting) return;

    this.isDeleting = true;

    this.api.deleteShipCode(this.data.shipCode.id).subscribe({
      next: (res: any) => {
        this.isDeleting = false;

        if (res.code === 400 || res.code === 404 || res.code === 500) {
          this.toast.error({ detail: 'ERROR', summary: res.description || res.message, duration: 5000, position: 'topRight' });
          return;
        }

        this.toast.success({ detail: 'SUCCESS', summary: res.description, duration: 5000, position: 'topRight' });
        this.dialogRef.close('deleted');
      },
      error: (err: any) => {
        this.isDeleting = false;
        this.toast.error({ detail: 'ERROR', summary: err.message, duration: 5000, position: 'topRight' });
      }
    });
  }
}
