import { Component, Inject, OnInit } from '@angular/core';
import { DatePipe, NgIf } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { BatchWiseInventory, Inventory } from '../../models/models';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import { environment } from 'src/environments/environment';
import { InventoryService } from '../../services/inventory.service';
import { InventorypopupComponent } from '../inventory-popup/inventory-popup.component';

@Component({
  selector: 'inventory-batchwise',
  templateUrl: './inventory-batchwise.component.html',
  styleUrls: ['./inventory-batchwise.component.scss'],
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
    MatPaginatorModule,
    TranslateModule
  ],
})
export class InventoryBatchwiseComponent implements OnInit {

  mydate = environment.date;
  displayedColumns: string[] = ['File', 'ItemID', 'SyncDate', 'Status', 'CustomerItemCode', 'ETADate', 'ETAQty', 'TotalATS', 'ATSL10', 'ATSL21', 'ATSL28', 'ATSL29', 'ATSL30', 'ATSL34', 'ATSL35', 'ATSL36', 'ATSL37', 'ATSL40', 'ATSL41', 'ATSL55', 'ATSL56', 'ATSL57', 'ATSL60', 'ATSL65', 'ATSL70', 'ATSL91'];
  dataSource: any[] = [];
  batchID: string = '';
  batchStatus: string = '';
  routeType: string = '';
  customerID: string = '';
  dateColumnLabel: string = 'Sent Date';
  showSpinner: boolean = false;
  isLoading: boolean = false;
  msg: string = '';
  code: number = 0;
  loadingStates = new Map<number, boolean>();
  batchWiseInventoryForm: FormGroup;
  listOfInventoryFiles: Inventory[] = [];
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;

  constructor(
    public dialogRef: MatDialogRef<InventoryBatchwiseComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private dialog: MatDialog,
    private api: InventoryService,
    private fb: FormBuilder,
    private toast: NgToastService,
    public languageService: LanguageService
  ) {
    this.batchID = data.batchID || (data.listofInventoryFiles?.[0]?.batchID ?? '');
    this.batchStatus = data.batchStatus || '';
    this.routeType = data.routeType || '';
    this.customerID = data.customerID || '';
    this.dateColumnLabel = this.isReceivedType() ? 'Received Date' : 'Sent Date';

    this.batchWiseInventoryForm = this.fb.group({
      itemID: fb.control('')
    });

    if (this.data.itemID) {
      this.batchWiseInventoryForm.get('itemID')?.setValue(this.data.itemID);
    }
  }

  ngOnInit(): void {
    this.loadBatchData();
  }

  isReceivedType(): boolean {
    const rt = this.routeType.toLowerCase();
    return rt.includes('full') || rt.includes('differential') || rt.includes('scsfullinventoryfeed') || rt.includes('scsdifferentialinventoryfeed');
  }

  onCancel() {
    this.dialogRef.close();
  }

  isLoadingFile(fileId: number): boolean {
    return this.loadingStates.get(fileId) || false;
  }

  onPageChange(event: PageEvent) {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadBatchData();
  }

  getBatchWiseItemID() {
    this.pageNumber = 1;
    this.loadBatchData();
  }

  loadBatchData() {
    this.isLoading = true;
    const itemID = this.batchWiseInventoryForm.get('itemID')?.value || '';
    this.api.getbatchWise(this.batchID, itemID, this.pageNumber, this.pageSize).subscribe({
      next: (res: any) => {
        this.dataSource = res.batchWiseInventory ?? [];
        this.totalCount = res.totalCount ?? 0;
        this.msg = res.message;
        this.code = res.code;

        if (this.dataSource.length === 0 && this.pageNumber === 1) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noInventoryDataMsg'), duration: 5000, position: 'topRight' });
        }

        this.isLoading = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.isLoading = false;
      },
    });
  }

  getInventoryFiles(element: any) {
    let customerId = element.customerID;
    let itemId = element.itemId;
    let batchId = element.batchID;

    this.showSpinner = false;

    this.api.getInventoryFiles(customerId, itemId, batchId).subscribe({
      next: (res: any) => {
        this.listOfInventoryFiles = res.files;
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfInventoryFiles.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noInventoryDataMsg'), duration: 5000, position: 'topRight' });
          this.showSpinner = false;
          return;
        }

        const dialogRef = this.dialog.open(InventorypopupComponent, {
          width: '85%',
          maxWidth: '1100px',
          disableClose: true,
          data: this.listOfInventoryFiles,
        });

        dialogRef.afterClosed().subscribe(result => {
          console.log('The dialog was closed');
        });

        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }
}
