import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface AppNotification {
    id: number;
    routeId: number;
    routeName: string;
    type: string;
    status: string;
    message: string;
    isRead: boolean;
    createdDate: string;
    completedDate: string | null;
}

@Injectable({
    providedIn: 'root',
})
export class NotificationService {
    private apiUrl = environment.apiUrl;
    private pollInterval: any;

    notifications$ = new BehaviorSubject<AppNotification[]>([]);
    unreadCount$ = new BehaviorSubject<number>(0);
    serverTimeUtc$ = new BehaviorSubject<string>('');

    constructor(private http: HttpClient) {}

    startPolling(): void {
        this.fetchNotifications();
        this.pollInterval = setInterval(() => this.fetchNotifications(), 10000);
    }

    stopPolling(): void {
        if (this.pollInterval) {
            clearInterval(this.pollInterval);
            this.pollInterval = null;
        }
    }

    fetchNotifications(): void {
        this.http.get<any>(`${this.apiUrl}api/Flows/notifications`).subscribe({
            next: (res) => {
                if (res.code === 200) {
                    this.notifications$.next(res.notifications || []);
                    this.unreadCount$.next(res.unreadCount || 0);
                    this.serverTimeUtc$.next(res.serverTimeUtc || '');
                }
            },
            error: () => {}
        });
    }

    markAsRead(id: number): void {
        this.http.post<any>(`${this.apiUrl}api/Flows/notifications/markRead/${id}`, {}).subscribe({
            next: () => this.fetchNotifications()
        });
    }

    markAllAsRead(): void {
        this.http.post<any>(`${this.apiUrl}api/Flows/notifications/markAllRead`, {}).subscribe({
            next: () => this.fetchNotifications()
        });
    }
}
