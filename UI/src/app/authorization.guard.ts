import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { UserType } from './models/models';
import { ApiService } from './services/api.service';

@Injectable({
  providedIn: 'root',
})
export class AuthorizationGuard  {
  constructor(private api: ApiService) {}
  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree>
    | boolean
    | UrlTree {
    if (this.api.isLoggedIn()) {
      return true;
    }
    return false;
  }
}
