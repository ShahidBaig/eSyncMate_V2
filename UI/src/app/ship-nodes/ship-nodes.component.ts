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
import { ShipNodesService } from '../services/ship-nodes.service';
import { AddShipNodeDialogComponent } from './add-ship-node-dialog/add-ship-node-dialog.component';
import { EditShipNodeDialogComponent } from './edit-ship-node-dialog/edit-ship-node-dialog.component';
import { DeleteShipNodeDialogComponent } from './delete-ship-node-dialog/delete-ship-node-dialog.component';
import { ShipNodesHelpDialogComponent } from './ship-nodes-help-dialog/ship-nodes-help-dialog.component';

@Component({
  selector: 'ship-nodes',
  templateUrl: './ship-nodes.component.html',
  styleUrls: ['./ship-nodes.component.scss'],
  standalone: true,
  imports: [
    CommonModule, NgIf, FormsModule, MatButtonModule, MatButtonToggleModule, MatCardModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressBarModule,
    MatProgressSpinnerModule, MatSelectModule, MatTableModule, MatTooltipModule, TranslateModule
  ],
})
export class ShipNodesComponent implements OnInit {
  // Empty source = both tables listed together
  columns: string[] = [];

  listOfShipNodes: any[] = [];
  shipNodesToDisplay: any[] = [];

  // Filter dropdown: only customers that already have ship nodes
  filterCustomers: string[] = [];
  filteredCustomerOptions: string[] = [];
  // Add/Edit dialogs: every ERP customer, so a first mapping can be created
  customersOptions: any[] = [];
  customerSearchText: string = '';
  selectedCustomer: string = 'EMPTY';
  searchValue: string = '';

  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  isLoading = false;
  showSpinner = false;

  canAdd = false;
  canEdit = false;
  canDelete = false;

  constructor(
    private api: ShipNodesService,
    private customerApi: CustomerProductCatalogService,
    private userApi: ApiService,
    private toast: NgToastService,
    private dialog: MatDialog,
    public languageService: LanguageService
  ) {
    const permissions = this.userApi.getMenuPermissions('edi/shipNodes');
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
    this.loadFilterCustomers();
    this.getShipNodes();
  }

  /** Actions only when the user may change something. */
  buildColumns(): void {
    const cols = ['customerID', 'whsid', 'shipNode'];
    if (this.canEdit || this.canDelete) cols.push('Actions');
    this.columns = cols;
  }

  loadCustomers(): void {
    this.customerApi.getERPCustomers().subscribe({
      next: (res: any) => {
        this.customersOptions = res.customers || [];
      }
    });
  }

  loadFilterCustomers(): void {
    this.api.getCustomers('').subscribe({
      next: (res: any) => {
        this.filterCustomers = res.customers || [];
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
    this.getShipNodes();
  }

  getShipNodes(resetPage: boolean = false): void {
    if (resetPage) this.pageNumber = 1;
    this.isLoading = true;

    const search = this.searchValue?.trim() ? this.searchValue.trim() : 'ALL';

    this.api.getShipNodes('', this.selectedCustomer, search, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.listOfShipNodes = res.shipNodes ?? [];
        this.shipNodesToDisplay = this.listOfShipNodes;
        this.totalCount = res.totalCount ?? 0;

        if (this.listOfShipNodes.length === 0 && this.pageNumber === 1) {
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
    this.dialog.open(ShipNodesHelpDialogComponent, { width: '90%', maxWidth: '1200px', maxHeight: '90vh' });
  }

  openAddDialog(): void {
    const dialogRef = this.dialog.open(AddShipNodeDialogComponent, {
      width: '800px',
      disableClose: true,
      data:{ customers: this.customersOptions }
    });

    dialogRef.afterClosed().subscribe(r => { if (r === 'saved') this.getShipNodes(true); });
  }

  openEditDialog(shipNode: any): void {
    const dialogRef = this.dialog.open(EditShipNodeDialogComponent, {
      width: '800px',
      disableClose: true,
      data:{ source: shipNode.source, customers: this.customersOptions, shipNode: shipNode }
    });

    dialogRef.afterClosed().subscribe(r => { if (r === 'updated') this.getShipNodes(); });
  }

  openDeleteDialog(shipNode: any): void {
    const dialogRef = this.dialog.open(DeleteShipNodeDialogComponent, {
      width: '460px',
      disableClose: true,
      data:{ source: shipNode.source, shipNode: shipNode }
    });

    dialogRef.afterClosed().subscribe(r => { if (r === 'deleted') this.getShipNodes(); });
  }
}
