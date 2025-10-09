import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SipmentFromNdcComponent } from './sipment-from-ndc.component';

describe('SipmentFromNdcComponent', () => {
  let component: SipmentFromNdcComponent;
  let fixture: ComponentFixture<SipmentFromNdcComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [SipmentFromNdcComponent]
    });
    fixture = TestBed.createComponent(SipmentFromNdcComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
