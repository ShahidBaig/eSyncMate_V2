import { Injectable, Inject, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { DOCUMENT } from '@angular/common';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class InactivityService {
  private timeoutId: any;
  private readonly logoutTime: number = 2 * 60 * 60 * 1000; // 6 hours in milliseconds
// private readonly logoutTime: number = 1 * 60 * 1000; 
  constructor(
    @Inject(DOCUMENT) private document: Document,
    private apiService: ApiService,
    private router: Router,
    private ngZone: NgZone
  ) {
    this.setup();
  }

  public setup() {
    this.ngZone.runOutsideAngular(() => {
      ['click', 'mousemove', 'keydown', 'scroll', 'touchstart']
        .forEach(event => this.document.addEventListener(event, () => this.reset()));
    });

    this.reset();
  }

  private reset() {
    clearTimeout(this.timeoutId);
    this.timeoutId = setTimeout(() => this.logout(), this.logoutTime);
  }

  private logout() {
    this.ngZone.run(() => {
      this.apiService.deleteToken();
      this.router.navigate(['/login']);
    });
  }
}
