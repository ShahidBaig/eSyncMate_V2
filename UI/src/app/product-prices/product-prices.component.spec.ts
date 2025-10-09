import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductPricesComponent } from './product-prices.component';

describe('ProductPricesComponent', () => {
  let component: ProductPricesComponent;
  let fixture: ComponentFixture<ProductPricesComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ProductPricesComponent]
    });
    fixture = TestBed.createComponent(ProductPricesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
