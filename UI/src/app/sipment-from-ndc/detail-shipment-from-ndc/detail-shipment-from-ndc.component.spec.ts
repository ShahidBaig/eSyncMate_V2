import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DetailShipmentFromNdcComponent } from './detail-shipment-from-ndc.component';

describe('DetailShipmentFromNdcComponent', () => {
  let component: DetailShipmentFromNdcComponent;
  let fixture: ComponentFixture<DetailShipmentFromNdcComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [DetailShipmentFromNdcComponent]
    });
    fixture = TestBed.createComponent(DetailShipmentFromNdcComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
