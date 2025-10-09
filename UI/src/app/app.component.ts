import { InactivityService } from './services/InactivityService';
import { Component, ViewChild, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';
import { MatSidenav } from '@angular/material/sidenav';
import { LanguageService } from './services/language.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  @ViewChild('sideNav') sideNav: MatSidenav | undefined;

  constructor(
    public api: ApiService,
    private languageService: LanguageService,
    private inactivityService: InactivityService
  ) {}

  ngOnInit(): void {
    if (!this.api.isLoggedIn()) {
      this.api.deleteToken();
    }
    // Initialize the inactivity service
    this.inactivityService.setup();
  }

  title = 'UI';

  toggleSideNav() {
    if (this.sideNav) {
      this.sideNav.toggle();
    }
  }
}
