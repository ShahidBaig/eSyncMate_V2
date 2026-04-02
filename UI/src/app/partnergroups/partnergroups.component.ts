import { Component, OnInit } from '@angular/core';
import { PartnerGroup } from '../models/models';
import { ApiService } from '../services/api.service';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
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
import { AddPartnerGroupDialogComponent } from './add-partnergroup-dialog/add-partnergroup-dialog.component';
import { EditPartnerGroupDialogComponent } from './edit-partnergroup-dialog/edit-partnergroup-dialog.component';
import { MatPaginatorModule } from '@angular/material/paginator';
import { PageEvent } from '@angular/material/paginator';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'partnergroups',
  templateUrl: './partnergroups.component.html',
  styleUrls: ['./partnergroups.component.scss'],
  standalone: true,
  imports: [
    MatButtonToggleModule,
    MatTableModule,
    DatePipe,
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
    MatPaginatorModule,
    TranslateModule
  ],
})
export class PartnerGroupsComponent implements OnInit {
  isLoading: boolean = false;
  listOfPartnerGroups: PartnerGroup[] = [];
  partnergroupsToDisplay: PartnerGroup[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  options = ['Select Partner Group', 'Id', 'Description','Source Party', 'Destination Party', 'Created Date'];
  selectedOption: string = 'Select Partner Group';
  searchValue: string = '';
  startDate: string = '';
  endDate: string = '';
  showDataColumn: boolean = true;
  isAdminUser: boolean = false;
  canAdd = false;
  canEdit = false;
  canDelete = false;
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;

  columns: string[] = [
    'id',
    'Description',
    'SourceParty',
    'DestinationParty',
    'CreatedDate',
    'Edit'
  ];

    constructor(private api: ApiService, private fb: FormBuilder, private toast: NgToastService, private dialog: MatDialog, public languageService: LanguageService) {
    const permissions = this.api.getMenuPermissions('edi/partnergroups');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
    } else {
      const isAdmin = ["ADMIN", "WRITER"].includes(this.api.getTokenUserInfo()?.userType || '');
      this.canAdd = isAdmin;
      this.canEdit = isAdmin;
      this.canDelete = isAdmin;
      this.isAdminUser = isAdmin;
    }
  }

  ngOnInit(): void {
    if (!this.canEdit) {
      const editIndex = this.columns.indexOf('Edit');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }

    if (this.selectedOption === 'Select Partner Group') {
      this.getPartnerGroups();
    }
  }

  openAddPartnerGroupDialog(): void {
    const dialogRef = this.dialog.open(AddPartnerGroupDialogComponent, {
      width: '800px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.getPartnerGroups();
      }
    });
  }

  openEditDialog(connectorData: any) {
    const dialogRef = this.dialog.open(EditPartnerGroupDialogComponent, {
      width: '800px',
      data: connectorData,
      disableClose: true,
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getPartnerGroups();
      }
    });
  }

  get label(): string {
    return this.selectedOption === 'Select Partner Group' ? 'Select Partner Group' : this.selectedOption;
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
    this.getPartnerGroups();
  }

  getPartnerGroups(resetPage: boolean = false) {
    if (resetPage) {
      this.pageNumber = 1;
    }

    let stringFromDate = '';
    let stringToDate = '';

    if (this.selectedOption === 'Select Partner Group') {
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
    this.api.getPartnerGroups(this.selectedOption, this.searchValue, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.listOfPartnerGroups = res.partnerGroup ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.partnergroupsToDisplay = this.listOfPartnerGroups;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfPartnerGroups.length === 0 && this.pageNumber === 1) {
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
