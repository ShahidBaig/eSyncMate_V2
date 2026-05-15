import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { Observable, EMPTY, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { Router } from '@angular/router';
import { TokenStoreService } from './token-store.service';
import { ApiService } from './api.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  private isRefreshing = false;
  private refreshDone$ = new BehaviorSubject<boolean>(false);

  constructor(
    private router: Router,
    private tokenStore: TokenStoreService,
    private api: ApiService
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Skip auth for public endpoints
    if (this.isPublic(req.url)) {
      return next.handle(req);
    }

    const token = this.tokenStore.get();

    const authReq = token
      ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`), withCredentials: true })
      : req.clone({ withCredentials: true });

    return next.handle(authReq).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 401) {
          return this.handle401(req, next);
        }
        return throwError(() => err);
      })
    );
  }

  private handle401(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (this.isRefreshing) {
      // Wait for ongoing refresh to finish, then retry
      return this.refreshDone$.pipe(
        filter(done => done),
        take(1),
        switchMap(() => {
          const newToken = this.tokenStore.get();
          if (!newToken) return EMPTY;
          return next.handle(this.addToken(req, newToken));
        })
      );
    }

    this.isRefreshing = true;
    this.refreshDone$.next(false);

    return this.api.refreshToken().pipe(
      switchMap((res: any) => {
        this.isRefreshing = false;
        if (res?.token && res.token !== 'Invalid') {
          this.api.saveToken(res.token);
          if (res.menus) this.api.saveUserMenus(res.menus);
          this.refreshDone$.next(true);
          return next.handle(this.addToken(req, res.token));
        }
        this.logout();
        return EMPTY;
      }),
      catchError(() => {
        this.isRefreshing = false;
        this.logout();
        return EMPTY;
      })
    );
  }

  private addToken(req: HttpRequest<any>, token: string): HttpRequest<any> {
    return req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`),
      withCredentials: true
    });
  }

  private logout(): void {
    this.tokenStore.clear();
    sessionStorage.setItem('sessionExpiryMessage', 'Session expired, please log in.');
    this.router.navigate(['login']);
  }

  private isPublic(url: string): boolean {
    return url.includes('Login') ||
           url.includes('VerifyMFA') ||
           url.includes('RefreshToken');
  }
}
