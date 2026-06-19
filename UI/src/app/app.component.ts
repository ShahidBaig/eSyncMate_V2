import { InactivityService } from './services/InactivityService';
import { Component, ViewChild, OnInit } from '@angular/core';
import { ApiService } from './services/api.service';
import { MatSidenav } from '@angular/material/sidenav';
import { LanguageService } from './services/language.service';
import { DialogDraggableService } from './services/dialog-draggable.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  @ViewChild('sideNav') sideNav: MatSidenav | undefined;

  get isLoggedIn(): boolean {
    return this.api.isLoggedIn();
  }

  constructor(
    public api: ApiService,
    private languageService: LanguageService,
    private inactivityService: InactivityService,
    private dialogDraggable: DialogDraggableService
  ) {}

  ngOnInit(): void {
    this.inactivityService.setup();
    // Make every Material dialog in the app draggable (by its header)
    this.dialogDraggable.init();
  }

  title = 'UI';

  toggleSideNav() {
    if (this.sideNav) {
      this.sideNav.toggle();
    }
  }
}
