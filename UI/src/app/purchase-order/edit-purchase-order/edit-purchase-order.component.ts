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
  status: string;
  lineNo?: number;
  unitPrice: number;
  orderQty: number;
  isNew?: boolean;
  extendedPrice: number
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
  selector: 'edit-purchase-order',
  templateUrl: './edit-purchase-order.component.html',
  styleUrls: ['./edit-purchase-order.component.scss'],
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
export class EditPurchaseOrderComponent implements OnInit {
  wareHouseOptions: WareHouse[] | undefined;
  editPurchaseOrderForm: FormGroup;
  supplierDetailControl = new FormControl({ value: '', disabled: false });
  myControl: FormControl;
  filteredOptions: Observable<Supplier[]> | undefined;
  filteredSupplierOptions: Observable<Supplier[]> | undefined;
  supplierOptions: Supplier[] = [];
  displayedColumns: string[] = ['LineNo', 'ItemID', 'Description', 'OrderQty', 'UnitPrice','extendedPrice', 'UPC', 'Qty', 'PrimaryCategoryName', 'SecondaryCategoryName', 'ManufacturerName', 'UOM' ,'NDCItemID', 'ProductName', 'Status' ,'Actions'];
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
  status?: string;
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
  visibleData: DetailItem[] = []; // This will hold only non-deleted items for UI
    table: any;

  constructor(
    public dialogRef: MatDialogRef<EditPurchaseOrderComponent>,
    private formBuilder: FormBuilder,
    private WareHouseApi: WareHouseService,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private purchaseOrderApi: PurchaseOrderService,
    private toast: NgToastService,
    private datePipe: DatePipe,
    public languageService: LanguageService,
    private translate: TranslateService
  ) {

    this.status = this.data.status;

    this.editPurchaseOrderForm = this.formBuilder.group({
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
      ItemID: [''],
      UPC: [''],
      ManufacturerName: [''],
      UOM: [''],
      NDCItemID: [''],
      ProductName: [''],
      PrimaryCategoryName: [''],
      SecondaryCategoryName: [''],
      Status: [''],
      UnitPrice: [{ value: 0, disabled: true }],
      OrderQty: [{ value: '', disabled: true }],
      QTY: [0],
      TotalQty: [0],
      WarehouseID: ['', Validators.required],
      TotalExtendedPrice:[0],
      extendedPrice:[0]
    });

    this.myControl = this.editPurchaseOrderForm.get('supplierID') as FormControl;
  }

  ngOnInit() {
    this.initializeForm();
    this.getWarehouseData();

    this.editPurchaseOrderForm.get('WarehouseID')?.valueChanges.subscribe((selectedId) => {
      const selectedWarehouse = this.wareHouseOptions?.find((warehouse) => warehouse.id === selectedId);
      if (selectedWarehouse) {
        this.updateAddressFields(selectedWarehouse);
      } else {
        this.clearAddressFields();
      }
    });
    
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

  getWarehouseData() {
    this.WareHouseApi.getWareHouses().subscribe({
      next: (res: any) => {
        this.wareHouseOptions = res.warehouses;
      },
    });
  }

  updateAddressFields(warehouse: WareHouse) {
    this.editPurchaseOrderForm.patchValue({
      shipToAddress1: warehouse.address1 || '',
      shipToAddress2: warehouse.address2 || '',
      shipToCity: warehouse.city || '',
      shipToState: warehouse.state || '',
      shipToZip: warehouse.zip || '',
      shipToCountry: warehouse.country || ''
    });
  }

  clearAddressFields() {
    this.editPurchaseOrderForm.patchValue({
      shipToAddress1: '',
      shipToAddress2: '',
      shipToCity: '',
      shipToState: '',
      shipToZip: '',
      shipToCountry: ''
    });
  }


  initializeForm() {
    this.getSuppliers();
    this.getPurchaseOrderDetail();

    this.editPurchaseOrderForm = this.formBuilder.group({
      id: this.data.id,
      orderDate: this.data.orderDate,
      vExpectedDate: this.data.vExpectedDate,
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
      WarehouseID: this.data.warehouseID,
      TotalQty: this.data.totalQty,
      TotalExtendedPrice: this.data.totalExtendedPrice,
      ItemID: [''],
      Description: [''],
      ManufacturerName: [''],
      UOM: [''],
      NDCItemID: [''],
      ProductName: [''],
      PrimaryCategoryName: [''],
      SecondaryCategoryName: [''],
      Status: this.data.status,
      UnitPrice: [{ value: 0, disabled: true }],
      OrderQty: [{ value: '', disabled: true }],
      UPC: [''],
      QTY: [0],
      extendedPrice: [0],
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
    this.editPurchaseOrderForm.get('OrderQty')?.enable();
    this.editPurchaseOrderForm.get('UnitPrice')?.enable();
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

        const selectedSupplier = this.supplierOptions.find(
          supplier => supplier.supplierID === this.data.supplierID
        );

        if (selectedSupplier) {
          this.myControl.setValue(selectedSupplier);
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
          uom: item.uom,
          ndcItemID: item.ndcItemID,
          productName: item.productName,
          primaryCategoryName: item.primaryCategoryName,
          secondaryCategoryName: item.secondaryCategoryName,
          status: item.status,
          lineNo: item.lineNo,
          unitPrice: item.unitPrice,
          orderQty: item.orderQty,
          extendedPrice: item.extendedPrice,
          isNew: false,
        }));

        this.refreshUI();
      },
      error: (err: any) => {
        console.error('Error fetching purchase order details:', err);
        this.toast.error({
          detail: "ERROR",
          summary: "Failed to load purchase order details.",
          duration: 5000,
          position: 'topRight'
        });
      }
    });
  }


  onSave(): void {
    const shipDate = this.editPurchaseOrderForm.get('vExpectedDate')?.value;
    if (!shipDate) {
      this.toast.warning({
        detail: "WARNING",
        summary: "Please set Ship Date.",
        duration: 5000,
        position: 'topRight'
      });
      return;
    }

    const selectedWarehouse = this.editPurchaseOrderForm.get('WarehouseID')?.value;
    if (!selectedWarehouse) {
      this.toast.warning({ detail: "WARNING", summary: "Please select a Warehouse.", duration: 5000, position: 'topRight' });
      return;
    }

    const poNumber = this.editPurchaseOrderForm.get('poNumber')?.value;
    if (!poNumber) {
      this.toast.warning({
        detail: "WARNING",
        summary: "Please set Purchase Order Number.",
        duration: 5000,
        position: 'topRight'
      });
      return;
    }

    const supplier = this.editPurchaseOrderForm.get('supplierID')?.value;
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

    const formValue = this.editPurchaseOrderForm.value;
    const supplierID = this.supplierID || formValue.supplierID;
    const orderModel = {
      Id: this.data.id,
      orderDate: formValue.orderDate ? this.datePipe.transform(formValue.orderDate, 'yyyy-MM-ddTHH:mm:ss') : '1900-01-01',
      poNumber: formValue.poNumber,
      supplierID: supplierID,
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
    this.purchaseOrderApi.updatePurchaseOrder(orderModel).subscribe({
      next: (res: { code: number; description: any; message: any; }) => {
        if (res.code === 100) {
          this.toast.success({ detail: "SUCCESS", summary: res.description, duration: 5000, position: 'topRight' });
          this.dialogRef.close('updated');
          this.showSpinner = false;
        } else {
          this.toast.error({ detail: "ERROR", summary: res.message || 'An error occurred', duration: 5000, position: 'topRight' });
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
    const qty = this.editPurchaseOrderForm.get('QTY')?.value;
    const orderQty = this.editPurchaseOrderForm.get('OrderQty')?.value;

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

    const unitPrice = this.editPurchaseOrderForm.get('UnitPrice')?.value;
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
      upc: this.editPurchaseOrderForm.get('UPC')?.value,
      qty: this.editPurchaseOrderForm.get('QTY')?.value,
      description: this.itemDescription,
      manufacturerName: this.editPurchaseOrderForm.get('ManufacturerName')?.value || '-',
      uom: this.editPurchaseOrderForm.get('UOM')?.value || '-',
      ndcItemID: this.editPurchaseOrderForm.get('NDCItemID')?.value || '-',
      productName: this.editPurchaseOrderForm.get('ProductName')?.value || '-',
      primaryCategoryName: this.editPurchaseOrderForm.get('PrimaryCategoryName')?.value || '-',
      secondaryCategoryName: this.editPurchaseOrderForm.get('SecondaryCategoryName')?.value || '-',
      status: this.editPurchaseOrderForm.get('Status')?.value || 'NEW',
      unitPrice: this.editPurchaseOrderForm.get('UnitPrice')?.value,
      orderQty: this.editPurchaseOrderForm.get('OrderQty')?.value,
      lineNo: this.dataSource.data.length + 1,
      isNew: true,
      extendedPrice: this.ExtendedPrice,
    };

    const newData = [...this.dataSource.data, newDetail];
    this.dataSource.data = newData;
    this.refreshUI();
    this.updateTotalQty();
    this.updateQtyAPI(newDetail.itemID, newDetail.orderQty, 'ADD', []);
    this.clearDetails();
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

    const newItems = this.dataSource.data.filter(item => item.isNew).map(item => ({
      ItemID: item.itemID,
      OrderQty: item.orderQty,
    }));

    if (newItems.length === 0) {
      return;
    }

    this.updateQtyAPI("", 0, 'CANCEL', newItems);
  }

  clearDetails(): void {
    this.itemID = "";
    this.itemControl.reset();
    this.itemDescriptionControl.reset();
    this.editPurchaseOrderForm.patchValue({
      ItemID: '',
      UPC: '',
      QTY: [0],
      UnitPrice: 0,
      extendedPrice:0,
      OrderQty: 0,
      ManufacturerName: '',
      UOM: '',
      NDCItemID: '',
      ProductName: '',
      PrimaryCategoryName: '',
      SecondaryCategoryName: '',
      Status: '',
    });
  }

  removeDetail(lineNo: number) {
    const removedItem = this.dataSource.data.find(item => item.lineNo === lineNo);

    if (removedItem) {
      removedItem.status = "DELETE"; 
      this.refreshUI();
      this.updateTotalQty();
      this.updateQtyAPI(removedItem.itemID, removedItem.orderQty, 'DELETE', []);
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

  updateTotalQty() {
    const totalQty = this.dataSource.data
      .filter(item => item.status !== "DELETE") 
      .reduce((sum, item) => sum + (item.orderQty || 0), 0);
      
     const price = this.dataSource.data
      .filter(item => item.status !== "DELETE") 
      .reduce((sum, item) => sum + (item.extendedPrice || 0), 0);  

    this.editPurchaseOrderForm.get('TotalQty')?.setValue(totalQty);
    this.editPurchaseOrderForm.get('TotalExtendedPrice')?.setValue(price);
  }

  refreshUI() {
    this.visibleData = this.dataSource.data.filter(item => item.status !== "DELETE");
    this.dataSource.data = [...this.dataSource.data]; 
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

  isFieldInvalid(fieldName: string): boolean {
    const field = this.editPurchaseOrderForm.get(fieldName);
    return field ? field.invalid && (field.dirty || field.touched || this.editPurchaseOrderForm.touched) : false;
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

  onItemSelected(event: any) {
    if (event.isUserInput) {
      const selectedItemID = event.source.value;

      if (selectedItemID) {
        this.itemID = selectedItemID.itemID;
        const matchingItem = this.itemsOptions.find(item => item.itemID === this.itemID && item.description === selectedItemID.description);

        if (matchingItem) {
          this.itemDescription = matchingItem.description;
          this.itemDescriptionControl.setValue(matchingItem);
          this.ndcItemID = matchingItem.ndcItemID;

          this.purchaseOrderApi.getItemSelected(this.itemID, this.ndcItemID).subscribe({
            next: (res: any) => {
              this.selectedItems = res.itemsData[0];
              this.editPurchaseOrderForm.patchValue({
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
              this.editPurchaseOrderForm.patchValue({
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
    let price = this.editPurchaseOrderForm.get('UnitPrice')?.value;
    if (price){
      this.ExtendedPrice = value * price
    }
  }

  calculateExtendedPriceForUnitPrice(value: number){
    let orderQty = this.editPurchaseOrderForm.get('OrderQty')?.value;
    if (orderQty){
      this.ExtendedPrice = value * orderQty
    }
  }
}
