import { Component, OnInit } from '@angular/core';
import { UserMenuModule, UserMenuItem } from '../models/models';
import { RouterLinkActive, RouterLink } from '@angular/router';
import { NgFor, TitleCasePipe, CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { Router, NavigationEnd } from '@angular/router';
import { environment } from '../../environments/environment';
import { ApiService } from '../services/api.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { filter } from 'rxjs/operators';

interface SearchResult extends UserMenuItem {
  moduleName: string;
}

@Component({
  selector: 'side-nav',
  templateUrl: './side-nav.component.html',
  styleUrls: ['./side-nav.component.scss'],
  standalone: true,
  imports: [
    MatListModule,
    NgFor,
    RouterLinkActive,
    RouterLink,
    TitleCasePipe,
    CommonModule,
    TranslateModule,
    MatExpansionModule,
    MatIconModule,
    FormsModule
  ],
})
export class SideNavComponent implements OnInit {
  constructor(
    public api: ApiService,
    private router: Router,
    private translate: TranslateService
  ) {}

  apiUrl = environment.apiUrl;
  modules: UserMenuModule[] = [];
  expandedModules = new Set<number>();
  currentRoute = '';
  searchTerm = '';
  filteredMenus: SearchResult[] = [];

  ngOnInit(): void {
    this.loadMenus();
    this.currentRoute = this.router.url;
    this.expandActiveModule();

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.currentRoute = event.urlAfterRedirects || event.url;
    });
  }

  loadMenus(): void {
    const userMenus = this.api.getUserMenus();
    if (userMenus && userMenus.modules) {
      this.modules = userMenus.modules;
    }
  }

  toggleModule(moduleId: number): void {
    if (this.expandedModules.has(moduleId)) {
      this.expandedModules.delete(moduleId);
    } else {
      this.expandedModules.add(moduleId);
    }
  }

  isActive(route: string): boolean {
    if (!route) return false;
    const normalizedRoute = route.startsWith('/') ? route : '/' + route;
    return this.currentRoute === normalizedRoute;
  }

  goToLink(route: string, isExternalLink: boolean, externalUrl: string) {
    if (isExternalLink) {
      const url = externalUrl || this.apiUrl + 'dashboard';
      window.open(url, '_blank');
    } else {
      this.router.navigate([route]);
    }
    // Clear search after navigation
    if (this.searchTerm) {
      this.clearSearch();
    }
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

  clearSearch(): void {
    this.searchTerm = '';
    this.filteredMenus = [];
  }

  private expandActiveModule(): void {
    for (const mod of this.modules) {
      for (const item of (mod.menuItems || [])) {
        if (this.isActive(item.route)) {
          this.expandedModules.add(mod.moduleId);
          return;
        }
      }
    }
    if (this.modules.length > 0) {
      this.expandedModules.add(this.modules[0].moduleId);
    }
  }
}
