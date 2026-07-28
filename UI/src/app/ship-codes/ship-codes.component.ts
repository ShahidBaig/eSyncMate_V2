import { Component, OnInit } from '@angular/core';
import { CommonModule, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule } from '@ngx-translate/core';
import { NgToastService } from 'ng-angular-popup';

import { ApiService } from '../services/api.service';
import { CustomerProductCatalogService } from '../services/customerProductCatalogDialog.service';
import { LanguageService } from '../services/language.service';
import { ShipCodesService } from '../services/ship-codes.service';
import { AddShipCodeDialogComponent } from './add-ship-code-dialog/add-ship-code-dialog.component';
import { EditShipCodeDialogComponent } from './edit-ship-code-dialog/edit-ship-code-dialog.component';
import { DeleteShipCodeDialogComponent } from './delete-ship-code-dialog/delete-ship-code-dialog.component';
import { ShipCodesHelpDialogComponent } from './ship-codes-help-dialog/ship-codes-help-dialog.component';

@Component({
  selector: 'ship-codes',
  templateUrl: './ship-codes.component.html',
  styleUrls: ['./ship-codes.component.scss'],
  standalone: true,
  imports: [
    CommonModule, NgIf, FormsModule, MatButtonModule, MatButtonToggleModule, MatCardModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressBarModule,
    MatProgressSpinnerModule, MatSelectModule, MatTableModule, MatTooltipModule, TranslateModule
  ],
})
export class ShipCodesComponent implements OnInit {
  columns: string[] = [];

  listOfShipCodes: any[] = [];
  shipCodesToDisplay: any[] = [];

  // '*' (generic) + every ERP customer, both for the filter and the dialogs
  customersOptions: any[] = [];
  filterCustomers: string[] = [];
  filteredCustomerOptions: string[] = [];
  customerSearchText: string = '';
  selectedCustomer: string = 'EMPTY';
  searchValue: string = '';

  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  isLoading = false;

  canAdd = false;
  canEdit = false;
  canDelete = false;

  constructor(
    private api: ShipCodesService,
    private customerApi: CustomerProductCatalogService,
    private userApi: ApiService,
    private toast: NgToastService,
    private dialog: MatDialog,
    public languageService: LanguageService
  ) {
    const permissions = this.userApi.getMenuPermissions('edi/shipCodes');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
    } else {
      const isAdmin = ['ADMIN', 'WRITER'].includes(this.userApi.getTokenUserInfo()?.userType || '');
      this.canAdd = this.canEdit = this.canDelete = isAdmin;
    }
  }

  ngOnInit(): void {
    this.buildColumns();
    this.loadCustomers();
    this.getShipCodes();
  }

  buildColumns(): void {
    const cols = ['customerID', 'sourceMethod', 'matchType', 'levelOfService', 'shippingMethod', 'isDefault'];
    if (this.canEdit || this.canDelete) cols.push('Actions');
    this.columns = cols;
  }

  loadCustomers(): void {
    this.customerApi.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customersOptions = res.customers || [];
        this.filterCustomers = this.customersOptions.map((c: any) => c.erpCustomerID);
        this.filteredCustomerOptions = this.filterCustomers;
      }
    });
  }

  filterCustomerOptions(): void {
    const search = (this.customerSearchText || '').toLowerCase();
    this.filteredCustomerOptions = this.filterCustomers.filter(c => c.toLowerCase().includes(search));
  }

  onCustomerSelectOpened(opened: boolean): void {
    if (opened) {
      this.customerSearchText = '';
      this.filteredCustomerOptions = this.filterCustomers;
    }
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.getShipCodes();
  }

  getShipCodes(resetPage: boolean = false): void {
    if (resetPage) this.pageNumber = 1;
    this.isLoading = true;

    const search = this.searchValue?.trim() ? this.searchValue.trim() : 'ALL';

    this.api.getShipCodes(this.selectedCustomer, search, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.listOfShipCodes = res.shipCodes ?? [];
        this.shipCodesToDisplay = this.listOfShipCodes;
        this.totalCount = res.totalCount ?? 0;

        if (this.listOfShipCodes.length === 0 && this.pageNumber === 1) {
          this.toast.info({
            detail: 'INFO',
            summary: this.languageService.getTranslation('noFilterDataMessage'),
            duration: 5000, position: 'topRight'
          });
        }

        if (res.code === 400 || res.code === 500) {
          this.toast.error({ detail: 'ERROR', summary: res.message, duration: 5000, position: 'topRight' });
        }

        this.isLoading = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: 'ERROR', summary: err.message, duration: 5000, position: 'topRight' });
        this.isLoading = false;
      }
    });
  }

  openHelp(): void {
    this.dialog.open(ShipCodesHelpDialogComponent, { width: '90%', maxWidth: '1200px', maxHeight: '90vh' });
  }

  openAddDialog(): void {
    const dialogRef = this.dialog.open(AddShipCodeDialogComponent, {
      width: '760px',
      disableClose: true,
      data: { customers: this.customersOptions }
    });

    dialogRef.afterClosed().subscribe(r => { if (r === 'saved') this.getShipCodes(true); });
  }

  openEditDialog(shipCode: any): void {
    const dialogRef = this.dialog.open(EditShipCodeDialogComponent, {
      width: '760px',
      disableClose: true,
      data: { customers: this.customersOptions, shipCode: shipCode }
    });

    dialogRef.afterClosed().subscribe(r => { if (r === 'updated') this.getShipCodes(); });
  }

  openDeleteDialog(shipCode: any): void {
    const dialogRef = this.dialog.open(DeleteShipCodeDialogComponent, {
      width: '460px',
      disableClose: true,
      data: { shipCode: shipCode }
    });

    dialogRef.afterClosed().subscribe(r => { if (r === 'deleted') this.getShipCodes(); });
  }
}
