import { Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { ApiService } from '../services/api.service';
import { TitleCasePipe } from '@angular/common';
import { RouterLink, Router, NavigationEnd } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LanguageService } from '../services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { UserMenuModule, UserMenuItem } from '../models/models';
import { NotificationService, AppNotification } from '../services/notification.service';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

interface SearchResult extends UserMenuItem {
  moduleName: string;
}

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
        MatDividerModule,
        RouterLink,
        TitleCasePipe,
        CommonModule,
        FormsModule,
        TranslateModule
    ],
})
export class PageHeaderComponent implements OnInit, OnDestroy {
  @Output() menuClicked = new EventEmitter<boolean>();
  currentRoute = '';
  currentPageTitle = '';
  modules: UserMenuModule[] = [];
  openModule: number | null = null;

  // Search
  searchOpen = false;
  searchTerm = '';
  filteredMenus: SearchResult[] = [];

  // Notifications (preserved from V2)
  isEsyncmate: boolean = ['ESYNCMATE', 'REPAINTSTUDIOS'].includes(this.api.getTokenUserInfo()?.company?.toUpperCase() || '');
  notifications: AppNotification[] = [];
  unreadCount = 0;
  serverTimeUtc = '';
  private notifSub?: Subscription;
  private countSub?: Subscription;
  private serverTimeSub?: Subscription;

  get isAdminUser(): boolean {
    const userMenus = this.api.getUserMenus();
    if (userMenus && userMenus.modules) {
      for (const mod of userMenus.modules) {
        for (const item of mod.menuItems) {
          if (item.route === 'edi/users' || item.route === 'edi/roles') return true;
        }
      }
    }
    return this.isSuperAdmin;
  }

  get isSuperAdmin(): boolean {
    return (this.api.getTokenUserInfo()?.roleName || '').toLowerCase() === 'superadmin';
  }

  constructor(
    public api: ApiService,
    private router: Router,
    public languageService: LanguageService,
    private translate: TranslateService,
    private notifService: NotificationService
  ) {
    this.currentRoute = this.router.url;
    this.loadMenus();
    this.updatePageTitle();

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.currentRoute = event.urlAfterRedirects || event.url;
      this.openModule = null;
      this.updatePageTitle();
    });
  }

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

  loadMenus(): void {
    const userMenus = this.api.getUserMenus();
    if (userMenus && userMenus.modules) {
      this.modules = userMenus.modules;
    }
  }

  updatePageTitle(): void {
    this.currentPageTitle = '';
    for (const mod of this.modules) {
      for (const menu of (mod.menuItems || [])) {
        if (this.isActive(menu.route)) {
          this.currentPageTitle = this.translate.instant(menu.menuTranslationKey) || menu.menuName;
          return;
        }
      }
    }
  }

  isActive(route: string): boolean {
    if (!route) return false;
    const normalized = route.startsWith('/') ? route : '/' + route;
    return this.currentRoute === normalized;
  }

  isModuleActive(module: UserMenuModule): boolean {
    return (module.menuItems || []).some(item => this.isActive(item.route));
  }

  goToLink(route: string, isExternalLink: boolean, externalUrl: string) {
    this.openModule = null;
    this.closeSearch();
    if (isExternalLink) {
      const url = externalUrl || this.api.apiUrl + 'dashboard';
      window.open(url, '_blank');
    } else {
      this.router.navigate([route]);
    }
  }

  getUserInitials(): string {
    const user = this.api.getTokenUserInfo();
    if (!user) return '?';
    const first = user.firstName?.charAt(0) || '';
    const last = user.lastName?.charAt(0) || '';
    return (first + last).toUpperCase() || '?';
  }

  // Notification methods (preserved from V2)
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
    this.router.navigate(['login']);
  }

  // Search
  toggleSearch(): void {
    this.searchOpen = true;
    setTimeout(() => {
      const input = document.querySelector('.search-input') as HTMLInputElement;
      if (input) input.focus();
    }, 50);
  }

  closeSearch(): void {
    this.searchOpen = false;
    this.searchTerm = '';
    this.filteredMenus = [];
  }

  onSearchBlur(): void {
    setTimeout(() => {
      if (!this.searchTerm) {
        this.closeSearch();
      }
    }, 200);
  }

  onSearch(): void {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      this.filteredMenus = [];
      return;
    }

    this.filteredMenus = [];
    for (const mod of this.modules) {
      const moduleName = this.translate.instant(mod.moduleTranslationKey) || mod.moduleName;
      for (const menu of (mod.menuItems || [])) {
        if (!menu.canView) continue;
        const menuLabel = this.translate.instant(menu.menuTranslationKey) || menu.menuName;
        if (
          menuLabel.toLowerCase().includes(term) ||
          moduleName.toLowerCase().includes(term) ||
          (menu.route && menu.route.toLowerCase().includes(term))
        ) {
          this.filteredMenus.push({ ...menu, moduleName });
        }
      }
    }
  }
}
