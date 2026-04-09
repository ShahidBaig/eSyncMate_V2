import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService } from '../../services/user.service';
import { NgToastService } from 'ng-angular-popup';

@Component({
  selector: 'reset-mfa-dialog',
  templateUrl: './reset-mfa-dialog.component.html',
  styleUrls: ['./reset-mfa-dialog.component.scss'],
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
})
export class ResetMfaDialogComponent {
  isLoading = false;
  isSuccess = false;
  isError = false;

  constructor(
    public dialogRef: MatDialogRef<ResetMfaDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private userService: UserService,
    private toast: NgToastService
  ) {}

  onCancel(): void {
    this.dialogRef.close(this.isSuccess);
  }

  onConfirm(): void {
    this.isLoading = true;
    this.isError = false;

    this.userService.resetMFA(this.data.id).subscribe({
      next: (res: any) => {
        this.isLoading = false;
        if (res.code === 200) {
          this.isSuccess = true;
        } else {
          this.isError = true;
          this.toast.error({ detail: "ERROR", summary: res.message, duration: 5000, position: 'topRight' });
        }
      },
      error: () => {
        this.isLoading = false;
        this.isError = true;
        this.toast.error({ detail: "ERROR", summary: 'Failed to reset MFA', duration: 5000, position: 'topRight' });
      }
    });
  }
}
