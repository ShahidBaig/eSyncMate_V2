import { Component, Inject, OnInit, NgModule } from '@angular/core';
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
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { FileContentViewerDialogComponent } from '../../file-content-viewer-dialog/file-content-viewer-dialog.component';
import { RouteDataService } from '../../services/routedata.service';
import { BatchWiseInventory, Inventory, RouteData } from '../../models/models';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import * as moment from 'moment';
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
    CommonModule,
    MatSelectModule,
    MatPaginatorModule,
    TranslateModule
  ],
})
export class InventoryBatchwiseComponent {

  mydate = environment.date;
  displayedColumns: string[] = ['CustomerID', 'ItemID', 'Status', 'CustomerItemCode', 'ETADate', 'ETAQty', 'TotalATS', 'ATSL10', 'ATSL21', 'ATSL28', 'ATSL29', 'ATSL30', 'ATSL34', 'ATSL35', 'ATSL36', 'ATSL37', 'ATSL40', 'ATSL41', 'ATSL55', 'ATSL60', 'ATSL65', 'ATSL70', 'ATSL91','File',]; // Add the actual column names from your data
  //'ATSL10', 'ATSL21', 'ATSL28', 'ATSL30', 'ATSL34', 'ATSL35', 'ATSL36', 'ATSL37', 'ATSL40', 'ATSL41', 'ATSL55', 'ATSL60', 'ATSL70', 'ATSL91'
  dataSource = this.data.listofInventoryFiles || [];
  batchID?: string;
  showSpinner: boolean = false;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  loadingStates = new Map<number, boolean>();
  batchWiseInventoryForm: FormGroup;
  listBatchWiseInventory: BatchWiseInventory[] = [];
  listOfInventoryFiles: Inventory[] = []
  constructor(

    public dialogRef: MatDialogRef<InventoryBatchwiseComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any, private dialog: MatDialog, private api: InventoryService, private fb: FormBuilder, private toast: NgToastService, public languageService: LanguageService) {
    this.batchID = data.listofInventoryFiles[0].batchID;

    const threeDaysAgo = new Date();
    threeDaysAgo.setDate(threeDaysAgo.getDate() - 3);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.batchWiseInventoryForm = this.fb.group({
      itemID: fb.control('')
    });

    if (this.data.itemID) {
      this.batchWiseInventoryForm.get('itemID')?.setValue(this.data.itemID);
    }
  }
  ngOnInit(): void {
    //this.getSearchRouteData();
    if (this.data.itemID)
      this.listBatchWiseInventory = this.dataSource.filter((item: any) =>
        item.itemId.toString().includes(this.data.itemID.toString())
      );
    else
      this.listBatchWiseInventory = this.dataSource;

    this.dataSource = this.listBatchWiseInventory.slice(0, 10);

  }

  onCancel() {
    this.dialogRef.close();
  }

  isLoading(fileId: number): boolean {
    // Retrieve loading state for a specific fileId
    return this.loadingStates.get(fileId) || false;
  }

  getBatchWiseItemID() {

    let itemID = (this.batchWiseInventoryForm.get('itemID') as FormControl).value;

    this.showSpinnerforSearch = true;
    let batchID: any = this.batchID;

    // if (((itemID == '' ))) {
    //   this.toast.info({ detail: "orderId", summary: this.languageService.getTranslation('provideFieldMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
    //   this.showSpinnerforSearch = false;
    //   return;
    // }

    if (itemID)
      this.listBatchWiseInventory = this.dataSource.filter((item: any) =>
        item.itemId.toString().includes(itemID.toString())
      );
      else
      this.listBatchWiseInventory = this.data.listofInventoryFiles;

      this.dataSource = this.listBatchWiseInventory.slice(0, 10);

      this.showSpinnerforSearch = false;
  }

  getFormattedDate(date: any) {
    // let year = date.getFullYear();
    // let month = (1 + date.getMonth()).toString().padStart(2, '0');
    // let day = date.getDate().toString().padStart(2, '0');

    // return year + '-' + month + '-' + day;
    return moment(date).format('YYYY-MM-DD');
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.dataSource = this.listBatchWiseInventory.slice(startIndex, startIndex + event.pageSize);
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
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noInventoryDataMsg'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinner = false;

          return;
        }

        const dialogRef = this.dialog.open(InventorypopupComponent, {
          width: '70%',
          disableClose: true,
          data: this.listOfInventoryFiles,
        });

        dialogRef.afterClosed().subscribe(result => {
          console.log('The dialog was closed');
        });

        this.showSpinner = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinner = false;
      },
    });
  }
}
