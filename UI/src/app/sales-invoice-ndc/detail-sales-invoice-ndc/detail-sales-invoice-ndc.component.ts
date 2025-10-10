import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule, DatePipe, NgIf, AsyncPipe } from '@angular/common';
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
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';

import * as moment from 'moment';
import * as html2pdf from 'html2pdf.js';
import { environment } from 'src/environments/environment';
import { ShipmentFromNDCService } from '../../services/shipmentFromNDC.service';
import { interval, map, startWith } from 'rxjs';
import { LanguageService } from '../../services/language.service';

interface InvoiceData {
  // Header-ish
  id: number;
  invoiceDate: string;
  poNumber: string;
  status: string;
  scacCode: string;
  routing: string;
  shippingDate: string;
  shippingName: string;
  shippingToNo: string;
  shippingAddress1: string;
  shippingAddress2: string;
  shippingCity: string;
  shippingState: string;
  shippingZip: string;
  shippingCountry: string;
  sellerID: string;
  invoiceTerms: string;
  freight?: number;     // preferred
  frieght?: number;     // fallback typo
  handlingAmount: number;
  salesTax: number;
  invoiceAmount: number;
  trackingNo: string;
  invoiceNo: number;

  // Detail-ish (reduced set)
  salesInvoice_ID?: number;
  detailPoNumber?: string;
  poNumberDetail?: string;
  sdPoNumber?: string;
  detailTrackingNo?: string;
  sdTrackingNo?: string;
  ediLineID?: string | number;
  sku?: string;
  uom?: string;
  unitPrice?: number | string | null;
  qty?: number | string | null;
}

@Component({
  selector: 'detail-sales-invoice-ndc',
  templateUrl: './detail-sales-invoice-ndc.component.html',
  styleUrls: ['./detail-sales-invoice-ndc.component.scss'],
  standalone: true,
  imports: [
    CommonModule, DatePipe, AsyncPipe, NgIf, ReactiveFormsModule,
    MatButtonToggleModule, MatTableModule, MatFormFieldModule, MatInputModule,
    MatCardModule, MatButtonModule, MatDatepickerModule, MatNativeDateModule,
    MatTooltipModule, MatIconModule, MatProgressSpinnerModule, MatSelectModule,
    TranslateModule
  ],
})
export class DetailSalesInvoiceNdcComponent implements OnInit {
  mydate = environment.date;

  /** Full list of lines */
  allRows: InvoiceData[] = [];

  /** Lines rendered in table (show all) */
  rows: InvoiceData[] = [];

  /** Header (first row) */
  header: Partial<InvoiceData> = {};

  /** Totals */
  totalQty = 0;
  totalAmount = 0;

  showSpinner = false;
  showSpinnerforSearch = false;
  msg = '';
  code = 0;

  now$ = interval(1000).pipe(startWith(0), map(() => new Date()));

  constructor(
    public dialogRef: MatDialogRef<DetailSalesInvoiceNdcComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private dialog: MatDialog,
    private api: ShipmentFromNDCService,
    private fb: FormBuilder,
    private toast: NgToastService,
    public languageService: LanguageService
  ) { }

  ngOnInit(): void {
    // Normalize incoming data to array
    const incoming = this.toArray<InvoiceData>(
      this.data?.invoiceData ?? this.data?.listofDetail
    );

    console.log('Invoice rows received:', incoming.length, incoming);

    this.allRows = incoming;
    this.rows = [...this.allRows]; // show all lines

    // Header = first row
    this.header = { ...(this.allRows[0] ?? {}) };

    // Totals (null/NaN-safe)
    this.totalQty = this.sumBy(this.allRows, r => this.toNum(r?.qty));
    this.totalAmount = this.sumBy(this.allRows, r => this.toNum(r?.qty) * this.toNum(r?.unitPrice));
  }

  /** Normalize various input shapes into an array */
  private toArray<T>(x: any): T[] {
    if (!x) return [];
    if (Array.isArray(x)) return x as T[];
    if (Array.isArray(x?.data)) return x.data as T[];
    if (Array.isArray(x?.invoiceData)) return x.invoiceData as T[];
    return [x as T];
  }

  /** Coerce value to number (handles "1,234.56" and null/undefined) */
  toNum(val: unknown): number {
    if (val == null) return 0;
    if (typeof val === 'number') return Number.isFinite(val) ? val : 0;
    if (typeof val === 'string') {
      const n = Number(val.replace(/,/g, '').trim());
      return Number.isFinite(n) ? n : 0;
    }
    return 0;
  }

  /** Sum helper */
  sumBy<T>(arr: T[], pick: (x: T) => number): number {
    return (arr ?? []).reduce((acc, item) => acc + pick(item), 0);
  }

  /** Line total (strict-template friendly) */
  safeLineTotal(line: InvoiceData | null | undefined): number {
    return this.toNum(line?.qty) * this.toNum(line?.unitPrice);
  }

  /** Prefer correct 'freight', fallback to 'frieght' */
  get freight(): number {
    return this.toNum(this.header?.freight ?? this.header?.frieght);
  }

  /** Subtotal for summary strip (no arrows in template) */
  get subtotal(): number {
    return (this.rows ?? []).reduce(
      (a, r) => a + this.toNum(r?.qty) * this.toNum(r?.unitPrice),
      0
    );
  }

  onCancel() {
    this.dialogRef.close();
  }

  /** Client asked for dd-MM-YYYY */
  getFormattedDate(date: any) {
    if (!date) return '';
    return moment(date).format('MM-DD-YYYY');
  }

  downloadPDF(): void {
    const reportContent = document.querySelector('#invoice-report') as HTMLElement | null;
    if (!reportContent) {
      console.error('Report content not found!');
      return;
    }
    setTimeout(() => {
      const options = {
        filename: 'invoice.pdf',
        image: { type: 'jpeg', quality: 0.98 },
        html2canvas: { scale: 4, logging: false, useCORS: true },
        jsPDF: { unit: 'pt', format: 'a4', orientation: 'portrait', putOnlyUsedFonts: true, compress: true }
      };
      // @ts-ignore
      (html2pdf as any)().from(reportContent).set(options).save();
    }, 200);
  }
}
