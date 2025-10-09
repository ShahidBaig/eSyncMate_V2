import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DetailSalesInvoiceNdcComponent } from './detail-sales-invoice-ndc.component';

describe('DetailShipmentFromNdcComponent', () => {
  let component: DetailSalesInvoiceNdcComponent;
  let fixture: ComponentFixture<DetailSalesInvoiceNdcComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [DetailSalesInvoiceNdcComponent]
    });
    fixture = TestBed.createComponent(DetailSalesInvoiceNdcComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
