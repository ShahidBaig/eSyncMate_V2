import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { NgToastService } from 'ng-angular-popup';

import { ShipNodesService } from '../../services/ship-nodes.service';

@Component({
  selector: 'delete-ship-node-dialog',
  templateUrl: './delete-ship-node-dialog.component.html',
  styleUrls: ['./delete-ship-node-dialog.component.scss'],
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, TranslateModule],
})
export class DeleteShipNodeDialogComponent {
  isDeleting = false;

  constructor(
    public dialogRef: MatDialogRef<DeleteShipNodeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { source: string; shipNode: any },
    private api: ShipNodesService,
    private toast: NgToastService
  ) { }

  onCancel(): void {
    this.dialogRef.close();
  }

  onDelete(): void {
    if (this.isDeleting) return;

    this.isDeleting = true;

    this.api.deleteShipNode(this.data.shipNode.id, this.data.source).subscribe({
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
