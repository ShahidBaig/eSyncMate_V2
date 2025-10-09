import { Component, EventEmitter, Output } from '@angular/core';
import { ApiService } from '../services/api.service';
import { TitleCasePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, } from '@angular/router';
import { UserType } from '../models/models';
import { CommonModule } from '@angular/common';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

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
        RouterLink,
        TitleCasePipe,
        CommonModule,
        TranslateModule 
    ],
})
export class PageHeaderComponent {
  @Output() menuClicked = new EventEmitter<boolean>();
  isAdminUser: boolean = ["ADMIN"].includes(this.api.getTokenUserInfo()?.userType || '');
  isGeckotec: boolean = ["GECKOTECH"].includes(this.api.getTokenUserInfo()?.company || '');

  constructor(public api: ApiService, private route: Router) {
}

  logOut() {
    this.api.deleteToken();
    this.route.navigate(['login']);
  }
}
