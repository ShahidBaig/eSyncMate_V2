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
import { TranslateModule } from '@ngx-translate/core';
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
import { WareHouseService } from 'src/app/services/warehouse.service';
import { ScrollingModule } from '@angular/cdk/scrolling';

export interface DetailItem {
  itemID: string;
  upc: string;
  qty: number;
  description: string;
  manufacturerName: string;
  uom: string;
  ndcItemID: string;
  productName: string;
  primaryCategoryName: string;
  secondaryCategoryName: string;
  lineNo?: number;
  unitPrice: number;
  orderQty: number;
  extendedPrice:number
}

export interface WareHouse {
  id: number;
  name: string;
  address1: string;
  address2?: string;
  city: string;
  state: string;
  zip: string;
  country: string;
}

@Component({
  selector: 'add-purchase-order',
  templateUrl: './add-purchase-order.component.html',
  styleUrls: ['./add-purchase-order.component.scss'],
  standalone: true,
  providers: [DatePipe],
  imports: [
    ScrollingModule,
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
export class AddPurchaseOrderComponent implements OnInit {
  wareHouseOptions: WareHouse[] | undefined;
  newPurchaseOrderForm: FormGroup;
  supplierDetailControl = new FormControl({ value: '', disabled: false });
  myControl: FormControl;
  filteredOptions: Observable<Supplier[]> | undefined;
  filteredSupplierOptions: Observable<Supplier[]> | undefined;
  supplierOptions: Supplier[] = [];
  displayedColumns: string[] = ['LineNo', 'ItemID', 'Description', 'OrderQty', 'UnitPrice', 'extendedPrice', 'UPC', 'Qty', 'PrimaryCategoryName', 'SecondaryCategoryName', 'ManufacturerName', 'UOM', 'NDCItemID', 'ProductName', 'Actions'];
  dataSource = new MatTableDataSource<DetailItem>();
  supplierID = '';
  itemID = '';
  itemDescription = '';
  manufacturerName = '';
  uom = '';
  ExtendedPrice:number = 0
  ndcItemID = '';
  productName = '';
  primaryCategoryName = '';
  secondaryCategoryName = '';
  supplierName = 'NDC MedPlus';
  public address1: string = '';
  public address2: string = '';
  public city: string = '';
  public state: string = '';
  public zip: string = '';
  public country: string = '';
  minOrderDate: Date | undefined;
  filteredItemOptions: Observable<InvFeedFromNDC[]> | undefined;
  filteredItemDescriptionOptions: Observable<InvFeedFromNDC[]> | undefined;
  itemsOptions: InvFeedFromNDC[] = [];
  itemControl = new FormControl<InvFeedFromNDC | null>(null);
  itemDescriptionControl = new FormControl<InvFeedFromNDC | null>(null);
  selectedItems: InvFeedFromNDC | null = null;
  filteredItemsDataOptions: Observable<InvFeedFromNDC[]> | undefined;
  showDataColumn: boolean = true;
  showSpinner: boolean = false;
  showSpinnerDetails: boolean = false;
  constructor(
    public dialogRef: MatDialogRef<AddPurchaseOrderComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private WareHouseApi: WareHouseService,
    private purchaseOrderApi: PurchaseOrderService,
    private toast: NgToastService,
    private datePipe: DatePipe,
    public languageService: LanguageService
  ) {
    this.minOrderDate = new Date();
    this.newPurchaseOrderForm = this.fb.group({
      typeId: [null, Validators.required],
      orderDate: [new Date(), Validators.required],
      vExpectedDate: ['', Validators.required],
      poNumber: ['', Validators.required],
      supplierID: ['', Validators.required],
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
      ItemID: [''],
      UPC: [''],
      ManufacturerName: [''],
      UOM: [''],
      NDCItemID: [''],
      ProductName: [''],
      PrimaryCategoryName: [''],
      SecondaryCategoryName: [''],
      WarehouseID: ['', Validators.required],
      QTY: [0],
      UnitPrice: [{ value: 0, disabled: true }],
      OrderQty: [{ value: '', disabled: true }],
      TotalQty: [0],
      TotalExtendedPrice:[0],
      extendedPrice:[0]
    });

    this.myControl = this.newPurchaseOrderForm.get('supplierID') as FormControl;
  }

  ngOnInit() {
    this.newPurchaseOrderForm.markAllAsTouched();

    this.getSuppliers();
    this.getWarehouseData();

    this.newPurchaseOrderForm.get('WarehouseID')?.valueChanges.subscribe((selectedId) => {
      const selectedWarehouse = this.wareHouseOptions?.find((warehouse) => warehouse.id === selectedId);
      if (selectedWarehouse) {
        this.updateAddressFields(selectedWarehouse);
      } else {
        this.clearAddressFields();
      }
    });

    const autoGeneratedPONumber = this.generatePONumber();

    this.newPurchaseOrderForm.patchValue({ poNumber: autoGeneratedPONumber });

    this.setDefaultAddressValues();
    this.getSuppliersItemsData();
  }

  displayFnSupplier(supplier: Supplier): string {
    return supplier ? supplier.name : '';
  }

  private _filterSuppliers(value: string): Supplier[] {
    const filterValue =
      (value && typeof value === 'string' ? value : '').toLowerCase();
    return this.supplierOptions.filter(supplier =>
      supplier.name.toLowerCase().includes(filterValue)
    );
  }

  onSupplierSelectedForDetail(event: any): void {
    if (event.isUserInput) {
      this.supplierName = event.source.value.name;
      this.getSuppliersItemsData();
    }
  }

  private setDefaultAddressValues(): void {
    this.newPurchaseOrderForm.patchValue({
      billToAddress1: '10 Kees Pl',
      billToAddress2: '',
      billToCity: 'Merrick',
      billToState: 'NY',
      billToZip: '11566-3658',
      billToCountry: 'US',
    });
  }

  private generatePONumber(): string {
    const timestamp = new Date().getTime();
    return `${timestamp}`;
  }

  getWarehouseData() {
    this.WareHouseApi.getWareHouses().subscribe({
      next: (res: any) => {
        this.wareHouseOptions = res.warehouses;
      },
    });
  }

  updateAddressFields(warehouse: WareHouse) {
    this.newPurchaseOrderForm.patchValue({
      shipToAddress1: warehouse.address1 || '',
      shipToAddress2: warehouse.address2 || '',
      shipToCity: warehouse.city || '',
      shipToState: warehouse.state || '',
      shipToZip: warehouse.zip || '',
      shipToCountry: warehouse.country || ''
    });
  }

  clearAddressFields() {
    this.newPurchaseOrderForm.patchValue({
      shipToAddress1: '',
      shipToAddress2: '',
      shipToCity: '',
      shipToState: '',
      shipToZip: '',
      shipToCountry: ''
    });
  }

  getSuppliersItemsData() {
    this.showSpinnerDetails = true;
    this.purchaseOrderApi.getSuppliersItemsData(this.supplierName).subscribe({
      next: (res: any) => {
        this.itemsOptions = res.suppliersItemsData;
        this.enablDetailsControl();

        this.filteredItemOptions = this.itemControl.valueChanges.pipe(
          startWith(''),
          map((value: string | InvFeedFromNDC | null) => {
            const filterValue =
              typeof value === 'string' ? value.toLowerCase() : value?.itemID?.toLowerCase() || '';
            return this._Itemsfilter(filterValue);
          })
        );

        this.filteredItemDescriptionOptions = this.itemDescriptionControl.valueChanges.pipe(
          startWith(''),
          map((value: string | InvFeedFromNDC | null) => {
            const filterValue =
              typeof value === 'string' ? value.toLowerCase() : value?.itemID?.toLowerCase() || '';
            return this._ItemsDescriptionfilter(filterValue);
          })
        );

        this.showSpinnerDetails = false;
      },
      error: (err: any) => {
        console.error('Error fetching supplier items:', err);
        this.toast.error({
          detail: 'ERROR',
          summary: 'Failed to fetch supplier items.',
          duration: 5000,
          position: 'topRight',
        });
        this.showSpinnerDetails = false;
      },
    });
  }

  enablDetailsControl() {
    this.supplierDetailControl.enable();
    this.newPurchaseOrderForm.get('OrderQty')?.enable();
    this.newPurchaseOrderForm.get('UnitPrice')?.enable();
  }

  getSuppliers() {
    this.purchaseOrderApi.getSupplierData().subscribe({
      next: (res: any) => {
        this.supplierOptions = res.supplierData;
        this.filteredOptions = this.myControl.valueChanges.pipe(
          startWith(''),
          map(value => this._Supplierfilter(value || ''))
        );

        this.filteredSupplierOptions = this.supplierDetailControl.valueChanges.pipe(
          startWith(''),
          map(value => this._filterSuppliers(value || ''))
        );
      },
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.newPurchaseOrderForm.get(fieldName);
    return field ? field.invalid && (field.dirty || field.touched || this.newPurchaseOrderForm.touched) : false;
  }

  onSave(): void {
    const shipDate = this.newPurchaseOrderForm.get('vExpectedDate')?.value;
    if (!shipDate) {
      this.toast.warning({
        detail: "WARNING",
        summary: "Please set Ship Date.",
        duration: 5000,
        position: 'topRight'
      });
      return;
    }

    const selectedWarehouse = this.newPurchaseOrderForm.get('WarehouseID')?.value;
    if (!selectedWarehouse) {
      this.toast.warning({ detail: "WARNING", summary: "Please select a Warehouse.", duration: 5000, position: 'topRight' });
      return;
    }

    const poNumber = this.newPurchaseOrderForm.get('poNumber')?.value;
    if (!poNumber) {
      this.toast.warning({
        detail: "WARNING",
        summary: "Please set Purchase Order Number.",
        duration: 5000,
        position: 'topRight'
      });
      return;
    }

    const supplier = this.newPurchaseOrderForm.get('supplierID')?.value;
    if (!supplier) {
      this.toast.warning({
        detail: "WARNING",
        summary: "Please select Supplier.",
        duration: 5000,
        position: 'topRight'
      });
      return;
    }

    if (this.dataSource.data.length === 0) {
      this.toast.warning({ detail: "WARNING", summary: "At least one product should be selected.", duration: 5000, position: 'topRight' });
      return;
    }

    const formValue = this.newPurchaseOrderForm.value;
    const orderModel = {
      orderDate: formValue.orderDate ? this.datePipe.transform(formValue.orderDate, 'yyyy-MM-ddTHH:mm:ss') : '1900-01-01',
      poNumber: formValue.poNumber,
      supplierID: formValue.supplierID.supplierID,
      vExpectedDate: formValue.vExpectedDate ? this.datePipe.transform(formValue.vExpectedDate, 'yyyy-MM-ddTHH:mm:ss') : '1900-01-01',
      shipToAddress1: formValue.shipToAddress1,
      shipToAddress2: formValue.shipToAddress2,
      shipToCity: formValue.shipToCity,
      shipToState: formValue.shipToState,
      shipToZip: formValue.shipToZip,
      shipToCountry: formValue.shipToCountry,
      billToAddress1: formValue.billToAddress1,
      billToAddress2: formValue.billToAddress2,
      billToCity: formValue.billToCity,
      billToState: formValue.billToState,
      billToZip: formValue.billToZip,
      billToCountry: formValue.billToCountry,
      referenceNo: formValue.referenceNo,
      shipServiceCode: formValue.shipServiceCode,
      warehouseID: selectedWarehouse,
      totalQty: formValue.TotalQty,
      totalExtendedPrice:formValue.TotalExtendedPrice,
      details: this.dataSource.data,
    };

    this.showSpinner = true;
    this.purchaseOrderApi.savePurchaseOrder(orderModel).subscribe({
      next: (res: { code: number; description: any; message: any; }) => {
        if (res.code === 100) {
          this.toast.success({ detail: "SUCCESS", summary: res.description, duration: 5000, position: 'topRight' });
          this.dialogRef.close('saved');
          this.showSpinner = false;
        } else {
          this.toast.error({ detail: "ERROR", summary: res.description || 'An error occurred', duration: 5000, position: 'topRight' });
          this.showSpinner = false;
        }

        this.showSpinner = false;
      },
      error: (err: { message: any; }) => {
        this.toast.error({ detail: "ERROR", summary: err.message || 'An error occurred', duration: 5000, position: 'topRight' });
        this.showSpinner = false;
      }
    });
  }

  addDetail() {
    const qty = this.newPurchaseOrderForm.get('QTY')?.value;
    const orderQty = this.newPurchaseOrderForm.get('OrderQty')?.value;

    if (!orderQty || orderQty <= 0) {
      this.toast.warning({
        detail: "WARNING",
        summary: "Order qty should be greater than 0.",
        duration: 5000,
        position: 'topRight'
      });
      return;
    }

    //if (orderQty > qty || qty < 0) {
    //  this.toast.warning({
    //    detail: "WARNING", summary: qty.qty < 0 ? "ATS Qty cannot be negative" : "Order Qty must be less than ATS Qty", duration: 5000, position: 'topRight'
    //  });
    //  return;
    //}

    const unitPrice = this.newPurchaseOrderForm.get('UnitPrice')?.value;
    if (!unitPrice || unitPrice <= 0) {
      this.toast.warning({
        detail: "WARNING",
        summary: "Unit Price should be greater than 0.",
        duration: 5000,
        position: 'topRight'
      });
      return;
    }

    if (!this.itemID || !unitPrice || !orderQty) {
      this.toast.error({
        detail: "ERROR",
        summary: "Please provide all item details",
        duration: 5000,
        position: 'topRight'
      });
      return;
    }

    const newDetail: DetailItem = {
      itemID: this.itemID,
      upc: this.newPurchaseOrderForm.get('UPC')?.value,
      qty: this.newPurchaseOrderForm.get('QTY')?.value,
      description: this.itemDescription,
      manufacturerName: this.newPurchaseOrderForm.get('ManufacturerName')?.value || '-',
      uom: this.newPurchaseOrderForm.get('UOM')?.value || '-',
      ndcItemID: this.newPurchaseOrderForm.get('NDCItemID')?.value || '-',
      productName: this.newPurchaseOrderForm.get('ProductName')?.value || '-',
      primaryCategoryName: this.newPurchaseOrderForm.get('PrimaryCategoryName')?.value || '-',
      secondaryCategoryName: this.newPurchaseOrderForm.get('SecondaryCategoryName')?.value || '-',
      unitPrice: this.newPurchaseOrderForm.get('UnitPrice')?.value,
      orderQty: this.newPurchaseOrderForm.get('OrderQty')?.value,
      lineNo: this.dataSource.data.length + 1,
      extendedPrice:this.ExtendedPrice
    };

    const newData = [...this.dataSource.data, newDetail];
    this.dataSource.data = newData;
    this.updateTotalQty();
    this.updateQtyAPI(newDetail.itemID, newDetail.orderQty, 'ADD', []);
    this.clearDetails();
  }

  updateTotalQty() {
    const totalQty = this.dataSource.data.reduce((sum, item) => sum + (item.orderQty || 0), 0);
    const price = this.dataSource.data.reduce((sum, item) => sum + (item.extendedPrice || 0), 0);

    this.newPurchaseOrderForm.get('TotalQty')?.setValue(totalQty);
    this.newPurchaseOrderForm.get('TotalExtendedPrice')?.setValue(price);

    
  }

  updateQtyAPI(itemID: string, orderQty: number, action: string, details: any[] = []): void {
    const payload = {
      ItemID: itemID || 'EMPTY',
      OrderQty: orderQty,
      Action: action,
      Details: details,
    };

    this.purchaseOrderApi.updateQty(payload).subscribe({
      next: (res: any) => {
        let actionMessage = '';
        switch (action.toUpperCase()) {
          case 'ADD':
            actionMessage = `added to`;
            break;
          case 'DELETE':
            actionMessage = `removed from`;
            break;
          case 'CANCEL':
            actionMessage = `cancelled for`;
            break;
          default:
            actionMessage = `updated for`;
            break;
        }

        this.toast.success({
          detail: "SUCCESS",
          summary: `Quantity for Item ID '${itemID || "multiple items"}' has been ${actionMessage} the inventory successfully.`,
          duration: 5000,
          position: 'topRight'
        });
      },
      error: (err: any) => {
        this.toast.error({
          detail: "ERROR",
          summary: `Failed to process action '${action}' for Item ID '${itemID || "multiple items"}'.`,
          duration: 5000,
          position: 'topRight'
        });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();

    const details = this.dataSource.data.map(item => ({
      ItemID: item.itemID,
      OrderQty: item.orderQty,
    }));

    if (details.length === 0) {
      console.log('No items to process. API call skipped.');
      return;
    }

    this.updateQtyAPI("", 0, 'CANCEL', details);
  }

  clearDetails(): void {
    this.itemID = "";
    this.itemControl.reset();
    this.itemDescriptionControl.reset();
    this.newPurchaseOrderForm.patchValue({
      ItemID: '',
      UPC: '',
      ManufacturerName: '',
      UOM: '',
      NDCItemID: '',
      ProductName: '',
      PrimaryCategoryName: '',
      SecondaryCategoryName: '',
      QTY: [0],
      UnitPrice: 0,
      OrderQty: 0,
    });
  }

  removeDetail(lineNo: number) {
    const removedItem = this.dataSource.data.find(item => item.lineNo === lineNo);

    if (removedItem) {
      const itemID = removedItem.itemID;
      const orderQty = removedItem.orderQty;
      const newData = this.dataSource.data.filter(item => item.lineNo !== lineNo);

      newData.forEach((item, index) => item.lineNo = index + 1);
      this.dataSource.data = newData;

      this.updateQtyAPI(itemID, orderQty, 'DELETE', []);
      this.updateTotalQty();

    } else {
      this.toast.warning({
        detail: "WARNING",
        summary: "Item not found for removal.",
        duration: 5000,
        position: 'topRight'
      });
    }

    this.clearDetails();
  }

  displayFn(supplier: Supplier): string {
    return supplier && supplier.supplierID
      ? `${supplier.supplierID} - ${supplier.name}`
      : '';
  }

  private _Supplierfilter(value: string): Supplier[] {
    const filterValue =
      (value && typeof value === 'string' ? value : '').toLowerCase();
    return this.supplierOptions.filter(option =>
      `${option.supplierID} - ${option.name}`.toLowerCase().includes(filterValue)
    );
  }

  onSupplierSelected(event: any): void {
    if (event.isUserInput) {
      this.supplierID = event.source.value.supplierID;
    }
  }

  displayitemDescriptionFn(item: InvFeedFromNDC): string {
    return item && item.description ? item.description : '';
  }

  private _ItemsDescriptionfilter(name: string): InvFeedFromNDC[] {
    const filterValue = (name && typeof name === 'string' ? name : '').toLowerCase();
    return this.itemsOptions.filter(item =>
      item.description && typeof item.description === 'string' &&
      item.description.toLowerCase().includes(filterValue)
    );
  }

  displayitemFn(item: InvFeedFromNDC): string {
    return item && item.itemID ? item.itemID : '';
  }

  private _Itemsfilter(name: string): InvFeedFromNDC[] {
    const filterValue = (name && typeof name === 'string' ? name : '').toLowerCase();    
    return this.itemsOptions
      .filter(item =>
        item.itemID && typeof item.itemID === 'string' &&
        item.itemID.toLowerCase().includes(filterValue) 
      )
      .sort((a, b) => {
        const aStarts = a.itemID.toLowerCase().startsWith(filterValue);
        const bStarts = b.itemID.toLowerCase().startsWith(filterValue);
        
        if (aStarts && !bStarts) return -1; 
        if (!aStarts && bStarts) return 1;  
        return 0; 
      });
  }

  onItemSelected(event: any) {
    if (event.isUserInput) {
      const selectedItemID = event.source.value;

      if (selectedItemID) {
        this.itemID = selectedItemID.itemID;
        const matchingItem = this.itemsOptions.find(
          item => item.itemID === this.itemID && item.description === selectedItemID.description);

        if (matchingItem) {
          this.itemDescription = matchingItem.description;
          this.itemDescriptionControl.setValue(matchingItem);
          this.ndcItemID = matchingItem.ndcItemID;

          this.purchaseOrderApi.getItemSelected(this.itemID, this.ndcItemID).subscribe({
            next: (res: any) => {
              this.selectedItems = res.itemsData[0];
              this.newPurchaseOrderForm.patchValue({
                UPC: this.selectedItems?.sku,
                QTY: this.selectedItems?.qty,
                UnitPrice: this.selectedItems?.unitPrice,
                ManufacturerName: this.selectedItems?.manufacturerName,
                UOM: this.selectedItems?.uom,
                NDCItemID: this.selectedItems?.ndcItemID,
                ProductName: this.selectedItems?.productName,
                PrimaryCategoryName: this.selectedItems?.primaryCategoryName,
                SecondaryCategoryName: this.selectedItems?.secondaryCategoryName,
              });
            },
            error: (err: any) => {
              this.toast.error({
                detail: "ERROR",
                summary: `Failed to fetch details for Item ID '${this.itemID}'.`,
                duration: 5000,
                position: 'topRight',
              });
            },
          });
        } else {
          this.toast.error({
            detail: "ERROR",
            summary: `No item found with description '${this.itemDescription}'.`,
            duration: 5000,
            position: 'topRight',
          });
        }
      }
    }
  }

  onItemDescriptionSelected(event: any) {
    if (event.isUserInput) {
      const selectedDescription = event.source.value;

      if (selectedDescription) {
        this.itemDescription = selectedDescription.description;

        const matchingItem = this.itemsOptions.find(item => item.description === this.itemDescription);

        if (matchingItem) {
          this.itemID = matchingItem.itemID;
          this.itemControl.setValue(matchingItem);
          this.ndcItemID = matchingItem.ndcItemID;

          this.purchaseOrderApi.getItemSelected(this.itemID, this.ndcItemID).subscribe({
            next: (res: any) => {
              this.selectedItems = res.itemsData[0];
              this.newPurchaseOrderForm.patchValue({
                UPC: this.selectedItems?.sku,
                QTY: this.selectedItems?.qty,
                UnitPrice: this.selectedItems?.unitPrice,
                ManufacturerName: this.selectedItems?.manufacturerName,
                UOM: this.selectedItems?.uom,
                NDCItemID: this.selectedItems?.ndcItemID,
                ProductName: this.selectedItems?.productName,
                PrimaryCategoryName: this.selectedItems?.primaryCategoryName,
                SecondaryCategoryName: this.selectedItems?.secondaryCategoryName,
              });
            },
            error: (err: any) => {
              this.toast.error({
                detail: "ERROR",
                summary: `Failed to fetch details for Item ID '${this.itemID}'.`,
                duration: 5000,
                position: 'topRight',
              });
            },
          });
        } else {
          this.toast.error({
            detail: "ERROR",
            summary: `No item found with description '${this.itemDescription}'.`,
            duration: 5000,
            position: 'topRight',
          });
        }
      }
    }
  }

  calculateExtendedPricForQty(value: number){
    let price = this.newPurchaseOrderForm.get('UnitPrice')?.value;
    if (price){
      this.ExtendedPrice = value * price
    }
  }

  calculateExtendedPriceForUnitPrice(value: number){
    let orderQty = this.newPurchaseOrderForm.get('OrderQty')?.value;
    if (orderQty){
      this.ExtendedPrice = value * orderQty
    }
  }
}
