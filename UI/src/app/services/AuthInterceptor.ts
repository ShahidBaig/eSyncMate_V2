import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable, EMPTY  } from 'rxjs';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private router: Router){}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = String(localStorage.getItem('access_token'));

    if (!token) {
      return next.handle(req);
    }

    const expiryTime = localStorage.getItem('tokenExpiry');
    if (expiryTime && new Date(expiryTime) <= new Date())
    {
      this.router.navigate(['login']);
      localStorage.setItem('tokenExpiry', '');
      localStorage.setItem('access_token','');
      localStorage.setItem('sessionExpiryMessage', 'Session expired, please log in.')
      return EMPTY;
    }

    const req1 = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`),
    });

    return next.handle(req1);
  }

}
