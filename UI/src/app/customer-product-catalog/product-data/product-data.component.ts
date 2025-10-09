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
import { FileContentViewerDialogComponent } from '../../file-content-viewer-dialog/file-content-viewer-dialog.component';
import { RouteDataService } from '../../services/routedata.service';
import { RouteData } from '../../models/models';
import { CustomerProductCatalogService } from '../../services/customerProductCatalogDialog.service';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'product-data',
  templateUrl: './product-data.component.html',
  styleUrls: ['./product-data.component.scss'],
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
    TranslateModule
  ],
})
export class ProductDataComponent implements OnInit {
  displayedColumns: string[] = ['CreatedDate', 'Type', 'fileName', 'DownloadFile']; // Add the actual column names from your data
  dataSource = this.data.historyData;
  currentProductId?: string;
  currentItemID?: string;
  showSpinner: boolean = false;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  loadingStates = new Map<number, boolean>();
  listOfProductsData: any[] = [];
  constructor(

    public dialogRef: MatDialogRef<ProductDataComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any, private dialog: MatDialog, private api: CustomerProductCatalogService, private fb: FormBuilder, private toast: NgToastService,
    public languageService: LanguageService) {
    this.currentProductId = data.historyData[0].productId;
    this.currentItemID = data.historyData[0].itemID;
  }
  ngOnInit(): void {
  }

  onCancel() {
    this.dialogRef.close();
  }

  downloadFile(id: number, fileName: string, type: any) {
    this.showSpinner = true;
    let l_data: string = "";

    this.api.getProductsData(id).subscribe({
      next: (res: any) => {
        this.listOfProductsData = res.customerProductCatalog.filter((product: { type: any; }) => product.type === type);
        this.msg = res.message;
        this.code = res.code;

        if (this.listOfProductsData == null || this.listOfProductsData.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;

          return;
        }

        if (this.code === 200) {
          //this.toast.success({ detail: "SUCCESS", summary: this.msg, duration: 5000, position: 'topRight' });
          this.showSpinnerforSearch = false;
        }
        else if (this.code === 400) {
          this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
        } else {
          this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.showSpinnerforSearch = false;
        }

        l_data = this.listOfProductsData[0].data;

        const blob = new Blob([l_data], { type: 'text/plain' });
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = fileName;
        link.click();

        this.showSpinner = false;

        this.showSpinnerforSearch = false;
      },
      error: (err: any) => {
        this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        this.showSpinnerforSearch = false;
      },
    });
  }

  isLoading(fileId: number): boolean {
    // Retrieve loading state for a specific fileId
    return this.loadingStates.get(fileId) || false;
  }

  viewFile(id: number, fileName: any, type: any, ID: any) {
    const fileExtension = fileName.split('.').pop().toLowerCase();
    let parsedData: any;
    let l_data: string = "";
    this.loadingStates.set(id, true);

    this.api.getProductsData(id).subscribe({
      next: (res: any) => {
        //this.listOfProductsData = res.customerProductCatalog.filter((product: { type: any; }) => product.type === type && product.id === ID);
        this.listOfProductsData = res.customerProductCatalog.filter((product: { type: any; id: any; }) => product.type === type && product.id === ID);

        this.msg = res.message;
        this.code = res.code;

        this.showSpinnerforSearch = false;
        this.loadingStates.set(id, false);
        if (this.listOfProductsData == null || this.listOfProductsData.length === 0) {
          this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('noFilterDataMessage'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
          this.loadingStates.set(id, false);

          return;
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

        l_data = this.listOfProductsData[0].data;

        if (fileExtension === 'json') {
          try {
            parsedData = JSON.parse(l_data);
          } catch (e) {
            console.error('Error parsing JSON', e);
          }
        } else if (fileExtension === 'edi') {
          l_data = l_data.replace(/~/g, '\n');
          parsedData = l_data;
        } else {
          parsedData = l_data;
        }

        this.dialog.open(FileContentViewerDialogComponent, {
          data: { content: parsedData, type: fileExtension },
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
}

