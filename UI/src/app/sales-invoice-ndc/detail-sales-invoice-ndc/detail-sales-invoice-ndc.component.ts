import { Component, Inject, } from '@angular/core';
import { DatePipe, NgIf } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
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
import { SalesInvoiceFromNDC } from '../../models/models';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import * as moment from 'moment';
import { environment } from 'src/environments/environment';
import { ShipmentFromNDCService } from '../../services/shipmentFromNDC.service';
import * as html2pdf from 'html2pdf.js';

interface InvoiceData {
  senderQualifierID: string;
  recipientQualifierID: string;
  createdBy: number;
  createdDate: string;
  frieght: number;
  handlingAmount: number;
  id: number;
  invoiceAmount: number;
  invoiceDate: string;
  invoiceNo: number;
  invoiceTerms: string;
  modifiedBy: number;
  modifiedDate: string;
  poNumber: string;
  routing: string;
  salesTax: number;
  scacCode: string;
  sellerID: string;
  shippingAddress1: string;
  shippingAddress2: string;
  shippingCity: string;
  shippingCountry: string;
  shippingDate: string;
  shippingName: string;
  shippingState: string;
  shippingToNo: string;
  shippingZip: string;
  status: string;
  trackingNo: string;
}

@Component({
  selector: 'detail-sales-invoice-ndc',
  templateUrl: './detail-sales-invoice-ndc.component.html',
  styleUrls: ['./detail-sales-invoice-ndc.component.scss'],
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
export class DetailSalesInvoiceNdcComponent {
  mydate = environment.date;
  dataSource = this.data.invoiceData || [];
  invoiceData: InvoiceData;
  showSpinner: boolean = false;
  msg: string = '';
  code: number = 0;
  showSpinnerforSearch: boolean = false;
  loadingStates = new Map<number, boolean>();
  listShipmentDetailFromNDC: SalesInvoiceFromNDC[] = [];
  constructor(
    public dialogRef: MatDialogRef<DetailSalesInvoiceNdcComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any, private dialog: MatDialog, private api: ShipmentFromNDCService, private fb: FormBuilder, private toast: NgToastService, public languageService: LanguageService) {
    this.invoiceData = data.listofDetail;

    const threeDaysAgo = new Date();
    threeDaysAgo.setDate(threeDaysAgo.getDate() - 3);

    const today = new Date();
    today.setDate(today.getDate() + this.mydate);
  }

  ngOnInit(): void {
    this.listShipmentDetailFromNDC = this.dataSource;
    this.dataSource = this.listShipmentDetailFromNDC.slice(0, 10);
  }

  onCancel() {
    this.dialogRef.close();
  }

  isLoading(fileId: number): boolean {
    return this.loadingStates.get(fileId) || false;
  }

  getFormattedDate(date: any) {
    return moment(date).format('YYYY-MM-DD');
  }

  onPageChange(event: PageEvent) {
    const startIndex = event.pageIndex * event.pageSize;
    this.dataSource = this.listShipmentDetailFromNDC.slice(startIndex, startIndex + event.pageSize);
  }

  downloadPDF(): void {
    const reportContent = document.querySelector('.scrollable-content');
    if (!reportContent) {
      console.error('Report content not found!');
      return;
    }
    setTimeout(() => {
      const options = {
        filename: 'report.pdf',
        image: { type: 'jpeg', quality: 0.98 },
        html2canvas: { scale: 6, logging: true, useCORS: true },
        jsPDF: {
          unit: 'in', 
          format: 'a4', 
          orientation: 'portrait', 
          // autoSize: true, 
          putOnlyUsedFonts: true,
          compress: true
        }
      };
      html2pdf().from(reportContent).set(options).save();
    }, 500);
  }
}
