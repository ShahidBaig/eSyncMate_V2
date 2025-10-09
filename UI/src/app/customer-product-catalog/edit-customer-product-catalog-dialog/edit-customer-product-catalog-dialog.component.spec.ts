import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditCustomerProductCatalogDialogComponent } from './edit-customer-product-catalog-dialog.component';

describe('EditCustomerProductCatalogDialogComponent', () => {
  let component: EditCustomerProductCatalogDialogComponent;
  let fixture: ComponentFixture<EditCustomerProductCatalogDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EditCustomerProductCatalogDialogComponent]
    });
    fixture = TestBed.createComponent(EditCustomerProductCatalogDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
