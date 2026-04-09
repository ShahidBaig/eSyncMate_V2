import { Component } from '@angular/core';
import { DatePipe, NgIf } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormBuilder, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { RouteTypesService } from '../services/route-types.service';
import { RouteType, User } from '../models/models';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

import { HttpClientModule, HttpClient } from '@angular/common/http';
import { ApiService } from '../services/api.service';
import { UserService } from '../services/user.service';
import { EditUsersDialogComponent } from './edit-users-dialog/edit-users-dialog.component';
import { DeleteUserDialogComponent } from './delete-user-dialog/delete-user-dialog.component';
import { UsersHelpDialogComponent } from './users-help-dialog/users-help-dialog.component';
import { ResetMfaDialogComponent } from './reset-mfa-dialog/reset-mfa-dialog.component';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss'],
  standalone: true,
  imports: [
    MatButtonToggleModule,
    MatTableModule,
    DatePipe,
    RouterLink,
    MatCardModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    NgIf,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTooltipModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    CommonModule,
    MatSelectModule,
    FormsModule,
    HttpClientModule,
    MatPaginatorModule,
    TranslateModule
  ],
})
export class UsersComponent {
  isLoading: boolean = false;
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;
  listOfUsers: User[] = [];
  UserToDisplay: User[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Users', 'Id', 'User ID', 'First Name', 'Email','Status', 'Created Date'];
  selectedOption: string = 'Select Users';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  showDataColumn: boolean = true;
  isAdminUser: boolean = false;
  canAdd = false;
  canEdit = false;
  canDelete = false;

  columns: string[] = [
    'id',
    'UserID',
    'FirstName',
    'Role',
    'Email',
    'Status',
    'CreatedDate',
    'Edit',
  ];

  constructor(private api: UserService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, private userApi: ApiService, public languageService: LanguageService) {
    const permissions = this.userApi.getMenuPermissions('edi/users');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
    } else {
      this.isAdminUser = ["ADMIN"].includes(this.userApi.getTokenUserInfo()?.userType || '');
      this.canAdd = this.isAdminUser;
      this.canEdit = this.isAdminUser;
      this.canDelete = this.isAdminUser;
    }
  }

  ngOnInit(): void {
    if (!this.canEdit)
    {
      const editIndex = this.columns.indexOf('Edit');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }

    if (this.selectedOption === 'Select Users') {
      this.getUsers();
    }
  }



  confirmDeleteUser(user: any) {
    const dialogRef = this.dialog.open(DeleteUserDialogComponent, {
      width: '400px',
      disableClose: true,
      data: user
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === true) {
        this.deleteUser(user.id);
      }
    });
  }

  deleteUser(id: number) {
    this.showSpinner = true;
    this.api.deleteUser(id).subscribe({
      next: (res: any) => {
        this.showSpinner = false;
        if (res.code === 200) {
          this.toast.success({ detail: "SUCCESS", summary: "User deleted successfully!", duration: 5000, position: 'topRight' });
          this.getUsers();
        } else {
          this.toast.error({ detail: "ERROR", summary: res.message, duration: 5000, position: 'topRight' });
        }
      },
      error: (err: any) => {
        this.showSpinner = false;
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
      }
    });
  }

  openHelp(): void {
    this.dialog.open(UsersHelpDialogComponent, { width: '90%', maxWidth: '1200px', maxHeight: '90vh' });
  }

  resetMFA(user: any): void {
    const dialogRef = this.dialog.open(ResetMfaDialogComponent, {
      width: '460px',
      disableClose: true,
      data: user
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === true) {
        this.getUsers();
      }
    });
  }

  openEditDialog(connectorData: any) {
    const dialogRef = this.dialog.open(EditUsersDialogComponent, {
      width: '800px',
      disableClose: true,
      data: connectorData
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated')
      {
        this.getUsers();
      }
    });
  }

  get label(): string {
    return this.selectedOption === 'Select Users' ? 'Select Users' : this.selectedOption;
  }

  onSelectionChange() {
    this.searchValue = '';
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  onPageChange(event: PageEvent) {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.getUsers();
  }

  getUsers(resetPage: boolean = false) {
    if (resetPage) {
      this.pageNumber = 1;
    }

    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Users') {
      this.searchValue = 'ALL';
    }

    if (this.selectedOption === 'Created Date' && this.startDate.toLocaleString().length > 10) {
      stringFromDate = this.getFormattedDate(this.startDate);
    }
    if (this.selectedOption === 'Created Date' && this.endDate.toLocaleString().length > 10) {
      stringToDate = this.getFormattedDate(this.endDate);
    }
    if (this.selectedOption === 'Created Date' && this.startDate.toLocaleString().length > 10 && this.endDate.toLocaleString().length > 10) {
      this.searchValue = stringFromDate + '/' + stringToDate;
    }

    this.isLoading = true;
    this.api.getUsers(this.selectedOption, this.searchValue, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.listOfUsers = res.user ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.UserToDisplay = this.listOfUsers;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfUsers.length === 0 && this.pageNumber === 1) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, position: 'topRight' });
        }

        if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, position: 'topRight' });
        }

        this.isLoading = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.isLoading = false;
      },
    });
  }
}
