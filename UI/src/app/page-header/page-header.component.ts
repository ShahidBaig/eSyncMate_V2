import { Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { ApiService } from '../services/api.service';
import { TitleCasePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';
import { UserType } from '../models/models';
import { CommonModule } from '@angular/common';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { NotificationService, AppNotification } from '../services/notification.service';
import { Subscription } from 'rxjs';

@Component({
    selector: 'page-header',
    templateUrl: './page-header.component.html',
    styleUrls: ['./page-header.component.scss'],
    standalone: true,
    imports: [
        MatToolbarModule,
        MatButtonModule,
        MatIconModule,
        MatMenuModule,
        MatTooltipModule,
        RouterLink,
        TitleCasePipe,
        CommonModule,
        TranslateModule
    ],
})
export class PageHeaderComponent implements OnInit, OnDestroy {
    @Output() menuClicked = new EventEmitter<boolean>();
    isAdminUser: boolean = ["ADMIN"].includes(this.api.getTokenUserInfo()?.userType || '');
    isGeckotec: boolean = ["GECKOTECH"].includes(this.api.getTokenUserInfo()?.company || '');
    isEsyncmate: boolean = ['ESYNCMATE', 'REPAINTSTUDIOS'].includes(this.api.getTokenUserInfo()?.company?.toUpperCase() || '');
    company = this.api.getTokenUserInfo()?.company?.toUpperCase() || '';

    notifications: AppNotification[] = [];
    unreadCount = 0;
    serverTimeUtc = '';
    private notifSub?: Subscription;
    private countSub?: Subscription;
    private serverTimeSub?: Subscription;

    constructor(
        public api: ApiService,
        private route: Router,
        private notifService: NotificationService
    ) {}

    ngOnInit(): void {
        if (this.isEsyncmate) {
            this.notifService.startPolling();
            this.notifSub = this.notifService.notifications$.subscribe(n => this.notifications = n);
            this.countSub = this.notifService.unreadCount$.subscribe(c => this.unreadCount = c);
            this.serverTimeSub = this.notifService.serverTimeUtc$.subscribe(t => this.serverTimeUtc = t);
        }
    }

    ngOnDestroy(): void {
        this.notifService.stopPolling();
        this.notifSub?.unsubscribe();
        this.countSub?.unsubscribe();
        this.serverTimeSub?.unsubscribe();
    }

    goHome() {
        if (this.isEsyncmate) {
            this.route.navigate(['/edi/dashboard']);
        } else if (this.company === 'GECKOTECH') {
            this.route.navigate(['/edi/carrier']);
        } else if (this.company === 'SURGIMAC') {
            this.route.navigate(['/edi/purchaseOrder']);
        } else {
            this.route.navigate(['/edi/dashboard']);
        }
    }

    onNotifClick(n: AppNotification): void {
        if (!n.isRead) {
            this.notifService.markAsRead(n.id);
        }
    }

    markAllRead(): void {
        this.notifService.markAllAsRead();
    }

    getTimeAgo(dateStr: string): string {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        if (isNaN(date.getTime())) return '';

        // Use server time as "now" to avoid timezone mismatch
        const now = this.serverTimeUtc ? new Date(this.serverTimeUtc) : new Date();
        if (isNaN(now.getTime())) return '';

        const diff = Math.floor((now.getTime() - date.getTime()) / 1000);

        if (diff < 0 || diff < 60) return 'Just now';
        if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
        if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
        return `${Math.floor(diff / 86400)}d ago`;
    }

    logOut() {
        this.notifService.stopPolling();
        this.api.deleteToken();
        this.route.navigate(['login']);
    }
}
