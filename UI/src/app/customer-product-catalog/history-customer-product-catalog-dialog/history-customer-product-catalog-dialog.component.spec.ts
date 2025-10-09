import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HistoryCustomerProductCatalogDialogComponent } from './history-customer-product-catalog-dialog.component';

describe('HistoryCustomerProductCatalogDialogComponent', () => {
  let component: HistoryCustomerProductCatalogDialogComponent;
  let fixture: ComponentFixture<HistoryCustomerProductCatalogDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [HistoryCustomerProductCatalogDialogComponent]
    });
    fixture = TestBed.createComponent(HistoryCustomerProductCatalogDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
