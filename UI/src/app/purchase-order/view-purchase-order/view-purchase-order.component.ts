import { Component, inject, Inject, OnInit } from '@angular/core';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule, FormsModule, FormArray } from '@angular/forms';
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
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipsModule } from '@angular/material/chips';
import { LanguageService } from '../../services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { InvFeedFromNDC, Supplier } from '../../models/models';
import { PurchaseOrderService } from '../../services/purchaseOrder.service';
import { Observable } from 'rxjs';
import { map, startWith } from 'rxjs/operators';
import { AsyncPipe } from '@angular/common';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatDividerModule } from '@angular/material/divider';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatCardModule } from '@angular/material/card';
import { MatTableDataSource } from '@angular/material/table';
export interface DetailItem {
  itemID: string;
  upc: string;
  description: string;
  manufacturerName: string;
  ndcItemID: string;
  productName: string;
  primaryCategoryName: string;
  secondaryCategoryName: string;
  lineNo?: number;
  unitPrice: number;
  orderQty: number;
  extendedPrice:number
}

@Component({
  selector: 'view-purchase-order',
  templateUrl: './view-purchase-order.component.html',
  styleUrls: ['./view-purchase-order.component.scss'],
  standalone: true,
  providers: [DatePipe],
  imports: [
    MatButtonToggleModule,
    MatTableModule,
    DatePipe,
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
    MatTabsModule,
    MatCheckboxModule,
    MatIconModule,
    MatChipsModule,
    TranslateModule,
    MatAutocompleteModule,
    AsyncPipe,
    MatDividerModule,
    MatGridListModule,
    MatCardModule,
  ],
})
export class ViewPurchaseOrderComponent implements OnInit {
  viewPurchaseOrderForm: FormGroup;
  myControl: FormControl;
  itemControl: FormControl;
  filteredOptions: Observable<Supplier[]> | undefined;
  supplierOptions: Supplier[] = [];
  displayedColumns: string[] = ['LineNo', 'ItemID', 'Description', 'OrderQty','extendedPrice', 'UnitPrice', 'UPC', 'PrimaryCategoryName', 'SecondaryCategoryName', 'ManufacturerName', 'NDCItemID', 'ProductName'];
  dataSource = new MatTableDataSource<DetailItem>();
  supplierID = '';
  itemID = '';
  status?: string;
  showDataColumn: boolean = true;

  filteredItemOptions: Observable<InvFeedFromNDC[]> | undefined;
  itemsOptions: InvFeedFromNDC[] = [];
  constructor(
    public dialogRef: MatDialogRef<ViewPurchaseOrderComponent>,
    private formBuilder: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private purchaseOrderApi: PurchaseOrderService,
    private toast: NgToastService,
    private datePipe: DatePipe,
    public languageService: LanguageService,
    private translate: TranslateService
  ) {

    this.status = this.data.status;

    this.viewPurchaseOrderForm = this.formBuilder.group({
      orderDate: [''],
      vExpectedDate: [''],
      poNumber: [''],
      supplierID: [''],
      shipToAddress1: [''],
      shipToAddress2: [''],
      shipToCity: [''],
      shipToState: [''],
      shipToZip: [''],
      shipToCountry: [''],
      shipToEmail: [''],
      shipToPhone: [''],
      billToAddress1: [''],
      billToAddress2: [''],
      billToCity: [''],
      billToState: [''],
      billToZip: [''],
      billToCountry: [''],
      billToEmail: [''],
      billToPhone: [''],
      referenceNo: [''],
      shipServiceCode: [''],
      warehouseName: [''],
      ItemID: [''],
      UPC: [''],
      Description: [''],
      ManufacturerName: [''],
      NDCItemID: [''],
      ProductName: [''],
      PrimaryCategoryName: [''],
      SecondaryCategoryName: [''],
      UnitPrice: [0],
      OrderQty: [0],
      TotalQty: [0],
      extendedPrice:[0],
      TotalExtendedPrice:[0]
    });

    this.myControl = this.viewPurchaseOrderForm.get('supplierID') as FormControl;
    this.itemControl = new FormControl('');
  }

  ngOnInit() {
    this.initializeForm();
    this.viewPurchaseOrderForm.controls['TotalExtendedPrice'].valueChanges.subscribe(value => {
      if (value !== null) {
        this.viewPurchaseOrderForm.controls['TotalExtendedPrice'].setValue(`$${value}`, { emitEvent: false });
      }
    });
  }

  initializeForm() {

    this.getSuppliers();
    this.getItemsData();
    this.getPurchaseOrderDetail();

    this.viewPurchaseOrderForm = this.formBuilder.group({
      id: this.data.id,
      orderDate: [
        this.data.orderDate
          ? this.datePipe.transform(this.data.orderDate, 'MM/dd/yyyy')
          : null
      ],
      vExpectedDate: [
        this.data.vExpectedDate
          ? this.datePipe.transform(this.data.vExpectedDate, 'MM/dd/yyyy')
          : null
      ],
      poNumber: this.data.poNumber,
      supplierID: this.data.supplierID,
      shipToAddress1: this.data.shipToAddress1,
      shipToAddress2: this.data.shipToAddress2,
      shipToCity: this.data.shipToCity,
      shipToState: this.data.shipToCountry,
      shipToZip: this.data.shipToZip,
      shipToCountry: this.data.shipToCountry,
      shipToEmail: this.data.shipToEmail,
      shipToPhone: this.data.shipToPhone,
      billToAddress1: this.data.billToAddress1,
      billToAddress2: this.data.billToAddress2,
      billToCity: this.data.billToCity,
      billToState: this.data.billToState,
      billToZip: this.data.billToZip,
      billToCountry: this.data.billToCountry,
      billToEmail: this.data.billToEmail,
      billToPhone: this.data.billToPhone,
      referenceNo: this.data.referenceNo,
      shipServiceCode: this.data.shipServiceCode,
      warehouseName: this.data.warehouseName,
      ItemID: [''],
      Description: [''],
      ManufacturerName: [''],
      NDCItemID: [''],
      ProductName: [''],
      PrimaryCategoryName: [''],
      SecondaryCategoryName: [''],
      UnitPrice: [0],
      OrderQty: [0],
      UPC: [''],
      TotalQty: this.data.totalQty,
      extendedPrice:[0],
      TotalExtendedPrice:this.data.totalExtendedPrice
    });
  }

  getItemsData() {
    this.purchaseOrderApi.getItemsData().subscribe({
      next: (res: any) => {
        this.itemsOptions = res.itemsData;
        this.filteredItemOptions = this.itemControl.valueChanges.pipe(
          startWith(''),
          map(value => this._Itemsfilter(value || ''))
        );
      },
    });
  }

  getSuppliers() {
    this.purchaseOrderApi.getSupplierData().subscribe({
      next: (res: any) => {
        this.supplierOptions = res.supplierData;

        // Set filtered options for autocomplete
        this.filteredOptions = this.myControl.valueChanges.pipe(
          startWith(''),
          map(value => this._Supplierfilter(value || ''))
        );

        // Find the selected supplier and set it to the form control
        const selectedSupplier = this.supplierOptions.find(
          supplier => supplier.supplierID === this.data.supplierID
        );

        // Set the supplier field in view mode with concatenated value
        if (selectedSupplier) {
          const supplierInfo = `${selectedSupplier.supplierID} - ${selectedSupplier.name}`;
          this.viewPurchaseOrderForm.get('supplierID')?.setValue(supplierInfo);
          this.myControl.setValue(selectedSupplier); // For autocomplete if needed
        }
      },
    });
  }

  getPurchaseOrderDetail() {
    this.purchaseOrderApi.getPurchaseOrderDetail(this.data.id).subscribe({
      next: (res: any) => {
        this.dataSource.data = res.detailData.map((item: any) => ({
          itemID: item.itemID,
          upc: item.upc,
          description: item.description,
          manufacturerName: item.manufacturerName,
          ndcItemID: item.ndcItemID,
          productName: item.productName,
          primaryCategoryName: item.primaryCategoryName,
          secondaryCategoryName: item.secondaryCategoryName,
          lineNo: item.lineNo,
          unitPrice: item.unitPrice,
          orderQty: item.orderQty,
          extendedPrice:item.extendedPrice,
          isNew: false, // Mark existing items as not new
        }));
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  displayFn(supplier: Supplier): string {
    return supplier && supplier.supplierID ? supplier.supplierID : '';
  }

  private _Supplierfilter(name: string): Supplier[] {
    const filterValue = (name && typeof name === 'string' ? name : '').toLowerCase();
    return this.supplierOptions.filter(option =>
      option.supplierID.toLowerCase().includes(filterValue)
    );
  }

  displayitemFn(item: InvFeedFromNDC): string {
    return item && item.itemID ? item.itemID : '';
  }

  private _Itemsfilter(name: string): InvFeedFromNDC[] {
    const filterValue = (name && typeof name === 'string' ? name : '').toLowerCase();
    return this.itemsOptions.filter(item =>
      item.itemID && typeof item.itemID === 'string' &&
      item.itemID.toLowerCase().includes(filterValue)
    );
  }

  getStatusTooltip(status: string, batchID: string): any {
    switch (status) {
      case 'NEW':
        return { key: 'NEW' };
      case 'INPROGRESS':
        return { key: 'INPROGRESS' };
      case 'SYNCED':
        return { key: 'SYNCED' };
      case 'ERROR':
        return { key: 'Batch Error', params: { batchID: batchID.toUpperCase() } };
      default:
        return { key: '' };
    }
  }

  getTooltipWithTranslation(element: any): string {
    const tooltipData = this.getStatusTooltip(element.status.toUpperCase(), element.batchID);
    return this.translate.instant(tooltipData.key, tooltipData.params);
  }

  getStatusClass(status: string): string {
    if (status.toLocaleUpperCase() === 'NEW') {
      return 'new-status';
    } else if (status.toLocaleUpperCase() === 'INPROGRESS') {
      return 'inprogress-status';
    } else if (status.toLocaleUpperCase() === 'ERROR') {
      return 'syncerror-status';
    } else if (status.toLocaleUpperCase() === 'SYNCED') {
      return 'sysced-status';
    } else {
      return '';
    }
  }
}
