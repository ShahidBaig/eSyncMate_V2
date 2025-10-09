import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SalesInvoiceNdcComponent } from './sales-invoice-ndc.component';

describe('SipmentFromNdcComponent', () => {
  let component: SalesInvoiceNdcComponent;
  let fixture: ComponentFixture<SalesInvoiceNdcComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [SalesInvoiceNdcComponent]
    });
    fixture = TestBed.createComponent(SalesInvoiceNdcComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
