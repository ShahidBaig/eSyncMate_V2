import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductUploadPricesComponent } from './product-upload-prices.component';

describe('ProductUploadPricesComponent', () => {
  let component: ProductUploadPricesComponent;
  let fixture: ComponentFixture<ProductUploadPricesComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ProductUploadPricesComponent]
    });
    fixture = TestBed.createComponent(ProductUploadPricesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
