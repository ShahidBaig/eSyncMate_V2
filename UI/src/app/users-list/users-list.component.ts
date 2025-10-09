import { Component, OnInit } from '@angular/core';
import { User } from '../models/models';
import { ApiService } from '../services/api.service';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'users-list',
    templateUrl: './users-list.component.html',
    styleUrls: ['./users-list.component.scss'],
    standalone: true,
    imports: [MatTableModule, MatButtonModule, TranslateModule],
})
export class UsersListComponent implements OnInit {
  users: User[] = [];
  columnsToDisplay: string[] = [
    'id',
    'name',
    'email',
    'mobile',
    'fine',
    'blocked',
    'active',
    'created on',
    'action',
  ];

  constructor(private api: ApiService, public languageService: LanguageService) {}

  ngOnInit(): void {
    this.api.getAllUsers().subscribe({
      next: (res: User[]) => {
        this.users = [];
        this.users = res;
      },
      error: (err: any) => console.log(err),
    });
  }

  blockUser(user: User) {
    var A1c = true;
    if (A1c) {
      this.api.unblockUser(user.id).subscribe({
        next: (res: any) => {
          if (res === 'success') A1c = false;
        },
        error: (err: any) => console.log(err),
      });
    } else {
      this.api.blockUser(user.id).subscribe({
        next: (res: any) => {
          if (res === 'success') A1c = true;
        },
        error: (err: any) => console.log(err),
      });
    }
  }

  enableUser(user: User) {
    var Ac = true;
    if (Ac) {
      this.api.disableUser(user.id).subscribe({
        next: (res: any) => {
          if (res === 'success') Ac = false;
        },
        error: (err: any) => console.log(err),
      });
    } else {
      this.api.enableUser(user.id).subscribe({
        next: (res: any) => {
          if (res === 'success') Ac = true;
        },
        error: (err: any) => console.log(err),
      });
    }
  }
}
