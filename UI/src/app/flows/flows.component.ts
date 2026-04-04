import { Component, OnInit, ViewChild } from '@angular/core';
import { ApiService } from '../services/api.service';
import { DatePipe, NgIf, CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { LanguageService } from '../services/language.service';
import { FlowsService } from '../services/flows.service';
import { CustomerProductCatalogService } from '../services/customerProductCatalogDialog.service';
import { AddFlowDialogComponent } from './add-flow-dialog/add-flow-dialog.component';
import { EditFlowDialogComponent } from './edit-flow-dialog/edit-flow-dialog.component';
import { ViewFlowDialogComponent } from './view-flow-dialog/view-flow-dialog.component';
import { ConfirmDeleteDialogComponent } from './confirm-delete-dialog/confirm-delete-dialog.component';

export interface Flows {
  id: number;
  customerID: string;
  title: string;
  description: string;
  status: string;
  createdDate: string;
  modifiedDate: string;
  flowDetails: any[];
}

interface Customers {
  erpCustomerID: string;
  name: string;
  id: any;
}

@Component({
  selector: 'app-flows',
  templateUrl: './flows.component.html',
  styleUrls: ['./flows.component.scss'],
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
export class FlowsComponent implements OnInit {
  isLoading: boolean = false;
  listOfFlows: Flows[] = [];
  flowsToDisplay: Flows[] = [];
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  showSpinner: boolean = false;
  selectedCustomer: string = 'EMPTY';
  showDataColumn: boolean = true;
  isAdminUser: boolean = false;
  canAdd = false;
  canEdit = false;
  canDelete = false;
  customerOptions: Customers[] | undefined;
  filteredCustomerOptions: Customers[] = [];
  customerSearchText: string = '';

  dataSource = new MatTableDataSource<Flows>([]);
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  columns: string[] = [
    'CustomerID',
    'Title',
    'Status',
    'ActivityDate',
    'Edit'
  ];

  constructor(
    private flowsApi: FlowsService,
    private toast: NgToastService,
    private dialog: MatDialog,
    public token: ApiService,
    public languageService: LanguageService,
    private ERPApi: CustomerProductCatalogService,
    private translate: TranslateService
  ) { }

  ngOnInit(): void {
    this.getERPCustomer();
    const permissions = this.token.getMenuPermissions('edi/flows');
    if (permissions) {
      this.canAdd = permissions.canAdd;
      this.canEdit = permissions.canEdit;
      this.canDelete = permissions.canDelete;
    } else {
      const isAdmin = ["ADMIN", "WRITER"].includes(this.token.getTokenUserInfo()?.userType || '');
      this.canAdd = isAdmin;
      this.canEdit = isAdmin;
      this.canDelete = isAdmin;
      this.isAdminUser = isAdmin;
    }

    if (!this.canEdit) {
      const editIndex = this.columns.indexOf('Edit');
      if (editIndex !== -1) {
        this.columns.splice(editIndex, 1);
      }
    }
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
  }

  openAddFlowDialog(): void {
    const dialogRef = this.dialog.open(AddFlowDialogComponent, {
      width: '1300px',
      disableClose: true
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result === 'saved') {
        this.getFlows(false);
      }
    });
  }

  openEditDialog(flowData: any) {
    const dialogRef = this.dialog.open(EditFlowDialogComponent, {
      width: '1300px',
      disableClose: true,
      data: flowData
    });
    dialogRef.afterClosed().subscribe(result => {
      if (result === 'updated') {
        this.getFlows(false);
      }
    });
  }

  openViewDialog(flowData: any) {
    const dialogRef = this.dialog.open(ViewFlowDialogComponent, {
      width: '1300px',
      height: '80vh',
      disableClose: true,
      data: flowData,
      panelClass: 'view-flow-dialog-panel'
    });
  }

  getERPCustomer() {
    this.ERPApi.getERPCustomers().subscribe({
      next: (res: any) => {
        const allCustomers = res.customers || [];
        const hiddenPartners = ['esyncmate', 'spars'];
        this.customerOptions = allCustomers.filter((c: any) =>
          !hiddenPartners.includes(c.name?.toLowerCase())
        );
        this.filteredCustomerOptions = this.customerOptions ? [...this.customerOptions] : [];

        if (this.customerOptions && this.customerOptions.length === 1) {
          this.selectedCustomer = this.customerOptions[0].erpCustomerID;
          this.getFlows(true);
        } else {
          this.selectedCustomer = 'EMPTY';
          this.dataSource.data = [];
          this.listOfFlows = [];
        }
      },
    });
  }

  filterCustomerOptions() {
    if (!this.customerOptions) {
      this.filteredCustomerOptions = [];
      return;
    }
    const search = this.customerSearchText.trim().toLowerCase();
    if (!search) {
      this.filteredCustomerOptions = [...this.customerOptions];
    } else {
      this.filteredCustomerOptions = this.customerOptions.filter(
        c => c.name.toLowerCase().includes(search) || c.erpCustomerID.toLowerCase().includes(search)
      );
    }
  }

  onCustomerSelectOpened(opened: boolean) {
    if (opened) {
      this.customerSearchText = '';
      this.filterCustomerOptions();
    }
  }

  getFlows(resetPage: boolean = false) {
    if (!this.selectedCustomer || this.selectedCustomer === 'EMPTY') {
      this.toast.warning({ detail: "WARNING", summary: "Please select a Partner first before searching.", duration: 3000, position: 'topRight' });
      return;
    }

    this.showSpinnerforSearch = true;

    let searchOption = 'Customer ID';
    let searchValue = this.selectedCustomer;

    this.isLoading = true;
    this.flowsApi.getFlows(searchOption, searchValue).subscribe({
      next: (res: any) => {
        this.msg = res.message;
        this.code = res.code;

        const oldPageIndex = this.paginator?.pageIndex ?? 0;
        const oldPageSize = this.paginator?.pageSize ?? 10;

        this.listOfFlows = res.flows ?? [];

        if (this.listOfFlows == null || this.listOfFlows.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, position: 'topRight' });
          this.showSpinnerforSearch = false;
          this.flowsToDisplay = [];
          this.dataSource.data = [];
          return;
        }

        this.dataSource.data = this.listOfFlows;

        if (resetPage) {
          this.paginator?.firstPage();
        } else {
          const maxPageIndex = Math.max(Math.ceil(this.listOfFlows.length / oldPageSize) - 1, 0);
          this.paginator.pageIndex = Math.min(oldPageIndex, maxPageIndex);
          this.paginator._changePageSize(this.paginator.pageSize);
        }

        this.showSpinnerforSearch = false;
        this.isLoading = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.showSpinnerforSearch = false;
        this.isLoading = false;
      },
    });
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.flowsToDisplay = this.listOfFlows.slice(startIndex, startIndex + event.pageSize);
  }

  onCustomerChange() {
    if (this.customerOptions && this.customerOptions.length > 1) {
      this.listOfFlows = [];
      this.flowsToDisplay = [];
      this.dataSource.data = [];
      this.paginator?.firstPage();
    }
  }

  deleteFlow(element: any) {
    const routeCount = element.flowDetails?.length || 0;
    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '460px',
      disableClose: true,
      data: { title: element.title, routeCount: routeCount }
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;

      this.showSpinner = true;
      this.flowsApi.deleteFlow(element.id).subscribe({
        next: (res: any) => {
          this.showSpinner = false;
          if (res.code === 200) {
            this.toast.success({ detail: "SUCCESS", summary: res.message || "Flow deleted successfully", duration: 5000, position: 'topRight' });

            // Remove from local arrays
            this.listOfFlows = this.listOfFlows.filter((f: any) => f.id !== element.id);
            this.dataSource.data = this.listOfFlows;

            // Trigger paginator refresh
            if (this.paginator) {
              const maxPageIndex = Math.max(Math.ceil(this.listOfFlows.length / this.paginator.pageSize) - 1, 0);
              this.paginator.pageIndex = Math.min(this.paginator.pageIndex, maxPageIndex);
              this.paginator._changePageSize(this.paginator.pageSize);
            }
          } else {
            this.toast.error({ detail: "ERROR", summary: res.message || "Failed to delete flow", duration: 5000, position: 'topRight' });
          }
        },
        error: (err: any) => {
          this.showSpinner = false;
          let errMsg = "An error occurred while deleting the flow";
          if (err && err.message) {
            errMsg += ": " + err.message;
          } else if (err && err.status) {
            errMsg += ": HTTP " + err.status;
          }
          this.toast.error({ detail: "ERROR", summary: errMsg, duration: 10000, position: 'topRight' });
        }
      });
    });
  }

  getStatusTooltip(status: string, customerIdentifier: string): any {
    switch (status) {
      case 'NEW':
        return { key: 'OrderStatusNew', params: { customerName: customerIdentifier.toUpperCase() } };
      case 'SHIPPED':
        return { key: 'OrderStatusShipped', params: { customerName: customerIdentifier.toUpperCase() } };
      case 'SYNCED':
        return { key: 'OrderStatusSynced' };
      case 'INVOICED':
        return { key: 'OrderStatusInvoiced', params: { customerName: customerIdentifier.toUpperCase() } };
      case 'CANCELLED':
        return { key: 'OrderStatusCancelled' };
      case 'ERROR':
        return { key: 'OrderStatusError' };
      case 'ASNERROR':
        return { key: 'OrderStatusASNERROR' };
      case 'DUPLICATE':
        return { key: 'OrderStatusASNERROR' };
      case 'ACKERROR':
        return { key: 'OrderStatusACKERROR' };
      case 'ACKNOWLEDGED':
        return { key: 'OrderStatusACKNOWLEDGED' };
      case 'INPROGRESS':
        return { key: 'OrderStatusINPROGRESS' };
      case 'ACTIVE':
        return { key: 'OrderStatusActive', params: { customerName: customerIdentifier.toUpperCase() } };
      case 'INACTIVE':
      case 'IN-ACTIVE':
        return { key: 'OrderStatusInactive', params: { customerName: customerIdentifier.toUpperCase() } };
      default:
        return;
    }
  }

  getTooltipWithTranslation(element: any): string {
    const customerIdentifier = element.customerID ? element.customerID : '';
    const status = element.status ? element.status.toUpperCase() : '';
    const tooltipData = this.getStatusTooltip(status, customerIdentifier);
    return tooltipData ? this.translate.instant(tooltipData.key, tooltipData.params) : '';
  }

  getStatusClass(status: string): string {
    const safeStatus = status ? status.toUpperCase() : '';
    if (safeStatus === 'NEW') {
      return 'new-status';
    } else if (safeStatus === 'SYNCERROR') {
      return 'syncerror-status';
    } else if (safeStatus === 'ACTIVE') {
      return 'processed-status';
    } else if (safeStatus === 'INACTIVE' || safeStatus === 'IN-ACTIVE') {
      return 'syncerror-status';
    } else if (safeStatus === 'SYNCED') {
      return 'sysced-status';
    } else if (safeStatus === 'PROCESSED') {
      return 'processed-status';
    } else if (safeStatus === 'ACKNOWLEDGED') {
      return 'acknowledged-status';
    } else if (safeStatus === 'ASNGEN') {
      return 'asngen-status';
    } else if (safeStatus === 'ASNMARK') {
      return 'asnmark-status';
    } else if (safeStatus === 'INVOICED') {
      return 'invoiced-status';
    } else if (safeStatus === 'COMPLETE') {
      return 'complete-status';
    } else if (safeStatus === 'FINISHED') {
      return 'finished-status';
    } else if (safeStatus === 'SPLITED') {
      return 'splited-status';
    } else if (safeStatus === 'INVEDI') {
      return 'invedi-status';
    } else if (safeStatus === 'CANCELLED') {
      return 'splited-status';
    } else if (safeStatus === 'SHIPPED') {
      return 'finished-status';
    } else if (safeStatus === 'ERROR') {
      return 'syncerror-status';
    } else if (safeStatus === 'DUPLICATE') {
      return 'sysced-status';
    } else if (safeStatus === 'ACKERROR') {
      return 'syncerror-status';
    } else if (safeStatus === 'INPROGRESS') {
      return 'sysced-status';
    } else if (safeStatus === 'ASNERROR') {
      return 'syncerror-status';
    } else {
      return '';
    }
  }

  isValidDate(value: any): boolean {
    if (!value) return false;
    const date = new Date(value);
    return !isNaN(date.getTime()) && date.getFullYear() > 1900;
  }

}
