import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomerProductCatalogComponent } from './customer-product-catalog.component';

describe('CustomerProductCatalogComponent', () => {
  let component: CustomerProductCatalogComponent;
  let fixture: ComponentFixture<CustomerProductCatalogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [CustomerProductCatalogComponent]
    });
    fixture = TestBed.createComponent(CustomerProductCatalogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
