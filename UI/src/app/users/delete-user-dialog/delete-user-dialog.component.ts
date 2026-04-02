import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'delete-user-dialog',
  templateUrl: './delete-user-dialog.component.html',
  styleUrls: ['./delete-user-dialog.component.scss'],
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    TranslateModule
  ],
})
export class DeleteUserDialogComponent {
  userName: string;

  constructor(
    public dialogRef: MatDialogRef<DeleteUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.userName = data.userID;
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
