import { Component, OnInit } from '@angular/core';
import { ApiService } from '../services/api.service';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

export interface TableElement {
  name: string;
  value: string | undefined;
}

@Component({
    selector: 'profile',
    templateUrl: './profile.component.html',
    styleUrls: ['./profile.component.scss'],
    standalone: true,
    imports: [MatCardModule, MatTableModule, TranslateModule],
})
export class ProfileComponent implements OnInit {
  dataSource: TableElement[] = [];
  columns: string[] = ['name', 'value'];

  constructor(private api: ApiService, public languageService: LanguageService) {}

  ngOnInit() {
    let user = this.api.getTokenUserInfo();

    this.dataSource = [
      { name: 'Name', value: user?.firstName + ' ' + user?.lastName },
      { name: 'Email', value: user?.email ?? '' },
      { name: 'Mobile', value: user?.mobile },
      { name: 'Blocked', value: this.blockedStatus() },
      { name: 'Active', value: this.activeStatus() },
    ];
  }

  blockedStatus(): string {
    let bloked = 1;//this.api.getTokenUserInfo()!.blocked;
    return bloked ? this.languageService.getTranslation('blockedMsg') : this.languageService.getTranslation('notBlockedMsg');
  }

  activeStatus(): string {
    let active = 1; //this.api.getTokenUserInfo()!.active;
    return active
      ? this.languageService.getTranslation('activeMsg_')
      : this.languageService.getTranslation('notActiveMsg');
  }
}
