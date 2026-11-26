import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiService } from '../services/api.service';

@Injectable({
  providedIn: 'root',
})
export class AuthenticationGuard  {
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
      return this.router.parseUrl('/login');
    }

    // Task 00008: a user who must change the password can open only the Change Password page
    if (this.api.mustChangePassword() && !state.url.startsWith('/change-password')) {
      return this.router.parseUrl('/change-password');
    }

    return true;
  }
}
