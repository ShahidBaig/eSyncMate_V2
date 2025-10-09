import { Component, inject, Inject, OnInit } from '@angular/core';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
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
import { RoutesService } from '../../services/routes.service';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipEditedEvent, MatChipInputEvent } from '@angular/material/chips';
import { MatChipsModule } from '@angular/material/chips';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { CustomerProductCatalogService } from '../../services/customerProductCatalogDialog.service';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'edit-customer-product-catalog-dialog',
  templateUrl: './edit-customer-product-catalog-dialog.component.html',
  styleUrls: ['./edit-customer-product-catalog-dialog.component.scss'],
  standalone: true,
  providers: [DatePipe],
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
    MatTabsModule,
    MatCheckboxModule,
    MatChipsModule,
    TranslateModule
  ],
})
export class EditCustomerProductCatalogDialogComponent implements OnInit {
  updateCustomerProductCatalogForm: FormGroup | any;
  
  constructor(
    public dialogRef: MatDialogRef<EditCustomerProductCatalogDialogComponent>,
    private formBuilder: FormBuilder,
    private customerProductCatalogApi: CustomerProductCatalogService,
    private toast: NgToastService,
    private datePipe: DatePipe,
    public languageService: LanguageService,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) { }

  ngOnInit() {
    this.initializeForm();
  }


  initializeForm() {
    this.updateCustomerProductCatalogForm = this.formBuilder.group({
      productId: [this.data.productId, Validators.required],
      brand: [this.data.brand],
      itemID: [this.data.itemID],
      upc: [this.data.upc],
      itemTypeName: [this.data.itemTypeName], 
      productRelation: [this.data.productRelation], 
      parentID: [this.data.parentID], 
      listPrice: [this.data.listPrice], 
      mapPrice: [this.data.mapPrice], 
      offPrice: [this.data.offPrice], 
    });
  }

  onCancel() {
    this.dialogRef.close();
  }

  updateCustomerProductCatalog(): void {
    const mapModel = {
      productId: this.updateCustomerProductCatalogForm.get('productId')?.value,
      customerID: this.data.customerID,
      brand: this.updateCustomerProductCatalogForm.get('brand')?.value,
      itemID: this.updateCustomerProductCatalogForm.get('itemID')?.value,
      upc: this.updateCustomerProductCatalogForm.get('upc')?.value,
      itemTypeName: this.updateCustomerProductCatalogForm.get('itemTypeName')?.value, 
      productRelation: this.updateCustomerProductCatalogForm.get('productRelation')?.value, 
      parentID: this.updateCustomerProductCatalogForm.get('parentID')?.value, 
      listPrice: this.updateCustomerProductCatalogForm.get('listPrice')?.value, 
      mapPrice: this.updateCustomerProductCatalogForm.get('mapPrice')?.value, 
      offPrice: this.updateCustomerProductCatalogForm.get('offPrice')?.value, 
    };

    if (this.updateCustomerProductCatalogForm.valid) {
      this.customerProductCatalogApi.updateCustomerProductCatalog(mapModel).subscribe({
        next: (res) => {
          if (res.code === 100)
          {
            this.toast.success({ detail: "SUCCESS", summary: res.description, duration: 5000, position: 'topRight' });

          } else if (res.code === 400) {
            this.toast.error({ detail: "ERROR", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          } else if (res.code === 401) {
            this.toast.warning({ detail: "WARNING", summary: res.description, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          } else {
            this.toast.info({ detail: "INFO", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          }

          this.dialogRef.close('updated');
        },
        error: (err) => {
          this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        }
      });
    }
  }


}
