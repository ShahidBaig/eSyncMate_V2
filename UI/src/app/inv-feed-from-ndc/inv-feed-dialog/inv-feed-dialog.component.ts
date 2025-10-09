import { Component, Inject, OnInit } from '@angular/core';
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
import { InvFeedFromNDCService } from 'src/app/services/invFeedFromNDC.service';
import { RouteLog } from '../../models/models';
import { FileContentViewerDialogComponent } from '../../file-content-viewer-dialog/file-content-viewer-dialog.component';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import { environment } from 'src/environments/environment';
import { WareHouseService } from 'src/app/services/warehouse.service';


interface WareHouse {
  id: number;
  name: string;
}


@Component({
  selector: 'inv-feed-dialog',
  templateUrl: './inv-feed-dialog.component.html',
  styleUrls: ['./inv-feed-dialog.component.scss'],
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
    FormsModule,
    TranslateModule,
    MatPaginatorModule
  ],
})
export class InvFeedDialogComponent implements OnInit {
  mydate = environment.date;
  displayedColumns: string[] = ['id', 'orderId', 'createdAt', 'warehouseName', 'skuCode', 'title', 'quantity'];
  wareHouseOptions: WareHouse[] | undefined;

  // Add the actual column names from your data
  dataSource = this.data.historyData;
  currentRouteId?: string;
  currentName?: string;
  showSpinnerforSearch: boolean = false;
  msg: string = '';
  code: number = 0;
  RouteLogForm: FormGroup;
  listOfRouteLog: RouteLog[] = [];
  pageSize: number = 10; 

  loadingStates = new Map<number, boolean>();
  constructor(
    private WareHouseApi: WareHouseService,
    public dialogRef: MatDialogRef<InvFeedDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any, private fb: FormBuilder, private toast: NgToastService, private api: InvFeedFromNDCService, private dialog: MatDialog, public languageService: LanguageService) {
    this.currentRouteId = data.id;
    this.currentName = data.name;
    const threeDaysAgo = new Date();
    threeDaysAgo.setDate(threeDaysAgo.getDate() -3);
    const today = new Date();
    today.setDate(today.getDate() + this.mydate);

    this.RouteLogForm = this.fb.group({
      orderId: fb.control(''),
      fromDate: new FormControl(formatDate(threeDaysAgo, "yyyy-MM-dd", "en")),
      toDate: new FormControl(formatDate(today, "yyyy-MM-dd", "en")),
      message: fb.control(''),
      types: fb.control('Info')
    });
  }

  ngOnInit(): void {
   /* this.getSearchRoutelog();*/
   this.getWarehouseData();
   this.viewFile(this.currentRouteId);
  }

  onCancel() {
    this.dialogRef.close();
  }

  getWarehouseData() {
    this.WareHouseApi.getWareHouses().subscribe({
      next: (res: any) => {
        this.wareHouseOptions = res.warehouses;
        console.log("ware houses", this.wareHouseOptions);
      },
    });
  }

  getSearchRoutelog() {
    
  }

  getFormattedDate(date: any) {
    let year = date.getFullYear();
    let month = (1 + date.getMonth()).toString().padStart(2, '0');
    let day = date.getDate().toString().padStart(2, '0');

    return year + '-' + month + '-' + day;
  }

  isLoading(fileId: number): boolean {
    // Retrieve loading state for a specific fileId
    return this.loadingStates.get(fileId) || false;
  }

  viewFile(id: any) {
    let parsedData: any;
    let l_data: string = "";
    this.loadingStates.set(id, true);

    this.api.getInvFeed(id).subscribe({
      next: (res: any) => {
        this.listOfRouteLog = res.veeqoSaleOrdersDetailData;
        this.msg = res.message;
        this.code = res.code;

        this.showSpinnerforSearch = false;
        this.loadingStates.set(id, false);
        if (this.listOfRouteLog == null || this.listOfRouteLog.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates.set(id, false);
          return;
        }else {
          this.onPageChange();
        }

        if (this.code === 200) {
          //this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.loadingStates.set(id, false);
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates.set(id, false);
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates.set(id, false);
        }

        l_data = res.routeLog[0].details;

        parsedData = l_data;

        this.dialog.open(FileContentViewerDialogComponent, {
          data: { content: parsedData, type: "txt", routeName: res.routeLog[0].name },
          width: '800px',
        });

        this.loadingStates.set(id, false);
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.loadingStates.set(id, false);
      },
    });
  }

  onPageChange(event?: PageEvent) {
  // If no event is provided, you can define a default behavior if needed
  if (event) {
    const startIndex = event.pageIndex * event.pageSize;
    this.dataSource = this.listOfRouteLog.slice(startIndex, startIndex + event.pageSize);
  } else {
    // Handle the case where no event is passed (optional behavior)
    // For example, reset to the first page or set a default size
    this.dataSource = this.listOfRouteLog.slice(0, this.pageSize); // assuming pageSize is defined
  }

  console.log(this.dataSource); // Log the current data source for debugging
}

}
