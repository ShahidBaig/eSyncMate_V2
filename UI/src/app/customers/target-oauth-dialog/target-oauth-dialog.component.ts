import { Component, HostListener, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NgToastService } from 'ng-angular-popup';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-target-oauth-dialog',
  standalone: true,
  templateUrl: './target-oauth-dialog.component.html',
  styleUrls: ['./target-oauth-dialog.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatTooltipModule
  ]
})
export class TargetOauthDialogComponent implements OnInit {

  loadingStatus = false;
  savingCreds = false;
  authorizing = false;

  useNewAuthentication = false;
  authorized = false;
  hasCredentials = false;
  hasSecret = false;
  refreshExpiry: string | null = null;
  accessExpiry: string | null = null;
  lastUpdated: string | null = null;

  creds = {
    clientId: '',
    clientSecret: '',
    authUrl: 'https://oauth.plus.iam.partnersonline.com/auth/oauth/v2/tgt/authorize/nla/1',
    tokenUrl: 'https://oauth.plus.iam.partnersonline.com/auth/oauth/v2/token'
  };

  // result panel after popup finishes
  resultSuccess: boolean | null = null;
  resultMessage = '';
  resultInfo: any = null;

  private popup: Window | null = null;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: { customerId: number; customerName: string },
    private dialogRef: MatDialogRef<TargetOauthDialogComponent>,
    private api: ApiService,
    private toast: NgToastService
  ) { }

  ngOnInit(): void {
    this.loadStatus();
  }

  loadStatus(): void {
    this.loadingStatus = true;
    this.api.getTargetOAuthStatus(this.data.customerId).subscribe({
      next: (res: any) => {
        this.useNewAuthentication = !!res.useNewAuthentication;
        this.authorized = !!res.authorized;
        this.hasCredentials = !!res.hasCredentials;
        this.hasSecret = !!res.hasSecret;
        // Prefill saved (non-secret) values so credentials aren't re-entered every time.
        if (res.clientId) { this.creds.clientId = res.clientId; }
        if (res.authUrl) { this.creds.authUrl = res.authUrl; }
        if (res.tokenUrl) { this.creds.tokenUrl = res.tokenUrl; }
        this.refreshExpiry = res.refreshExpiry ?? null;
        this.accessExpiry = res.accessExpiry ?? null;
        this.lastUpdated = res.lastUpdated ?? null;
        this.loadingStatus = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: 'ERROR', summary: err?.error?.message || err.message, duration: 5000, position: 'topRight' });
        this.loadingStatus = false;
      }
    });
  }

  onToggleFlag(): void {
    this.api.setUseNewAuthentication(this.data.customerId, this.useNewAuthentication).subscribe({
      next: () => {
        this.toast.success({ detail: 'SUCCESS', summary: `New authentication ${this.useNewAuthentication ? 'enabled' : 'disabled'}.`, duration: 4000, position: 'topRight' });
      },
      error: (err: any) => {
        this.useNewAuthentication = !this.useNewAuthentication; // revert
        this.toast.error({ detail: 'ERROR', summary: err?.error?.message || err.message, duration: 5000, position: 'topRight' });
      }
    });
  }

  saveCredentials(): void {
    if (!this.creds.clientId) {
      this.toast.warning({ detail: 'WARNING', summary: 'Client ID is required.', duration: 4000, position: 'topRight' });
      return;
    }
    this.savingCreds = true;
    this.api.saveTargetOAuthCredentials(this.data.customerId, this.creds).subscribe({
      next: (res: any) => {
        this.toast.success({ detail: 'SUCCESS', summary: res?.message || 'Credentials saved.', duration: 4000, position: 'topRight' });
        this.savingCreds = false;
        this.hasCredentials = true;
      },
      error: (err: any) => {
        this.toast.error({ detail: 'ERROR', summary: err?.error?.message || err.message, duration: 5000, position: 'topRight' });
        this.savingCreds = false;
      }
    });
  }

  authorize(): void {
    this.resultSuccess = null;
    this.resultMessage = '';
    this.resultInfo = null;
    this.authorizing = true;

    this.api.buildTargetAuthUrl(this.data.customerId).subscribe({
      next: (res: any) => {
        if (!res?.authUrl) {
          this.authorizing = false;
          this.toast.error({ detail: 'ERROR', summary: 'Could not build authorization URL.', duration: 5000, position: 'topRight' });
          return;
        }
        this.popup = window.open(res.authUrl, 'targetOAuth', 'width=600,height=750');
        if (!this.popup) {
          this.authorizing = false;
          this.toast.error({ detail: 'ERROR', summary: 'Popup blocked. Please allow pop-ups and try again.', duration: 6000, position: 'topRight' });
        }
      },
      error: (err: any) => {
        this.authorizing = false;
        this.toast.error({ detail: 'ERROR', summary: err?.error?.message || err.message, duration: 5000, position: 'topRight' });
      }
    });
  }

  @HostListener('window:message', ['$event'])
  onMessage(event: MessageEvent): void {
    const d = event?.data;
    if (!d || d.source !== 'target-oauth') { return; }

    this.authorizing = false;
    this.resultSuccess = !!d.success;
    this.resultMessage = d.message || '';
    this.resultInfo = d.info || null;

    if (d.success) {
      this.toast.success({ detail: 'AUTHORIZED', summary: 'Target authorization successful.', duration: 5000, position: 'topRight' });
      this.loadStatus();
    } else {
      this.toast.error({ detail: 'FAILED', summary: this.resultMessage, duration: 7000, position: 'topRight' });
    }

    try { this.popup?.close(); } catch { /* ignore */ }
  }

  close(): void {
    this.dialogRef.close();
  }
}
