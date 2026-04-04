import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiService } from './services/api.service';

@Injectable({
  providedIn: 'root',
})
export class AuthorizationGuard  {
  constructor(private api: ApiService, private router: Router) {}
  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree>
    | boolean
    | UrlTree {
    if (!this.api.isLoggedIn()) {
      this.router.navigate(['login']);
      return false;
    }

    // Check if route is allowed by user's menu permissions
    const userMenus = this.api.getUserMenus();
    if (!userMenus || !userMenus.modules || userMenus.modules.length === 0) {
      // Fallback: allow if logged in (backward compatibility for users without roles assigned yet)
      return true;
    }

    // Strip leading slash and query params for matching
    const requestedPath = state.url.replace(/^\//, '').split('?')[0].split('#')[0];
    if (this.api.isRouteAllowed(requestedPath)) {
      return true;
    }

    // Route not permitted - redirect to first allowed route (never redirect to login if authenticated)
    for (const mod of userMenus.modules) {
      if (mod.menuItems && mod.menuItems.length > 0) {
        const firstAllowed = mod.menuItems.find(m => m.canView);
        if (firstAllowed) {
          this.router.navigateByUrl('/' + firstAllowed.route);
          return false;
        }
      }
    }

    // No allowed routes found but user is authenticated - allow access (backward compat)
    return true;
  }
}
