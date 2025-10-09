import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CarrierLoadTenderComponent } from './carrier-load-tender.component';

describe('CarrierLoadTenderComponent', () => {
  let component: CarrierLoadTenderComponent;
  let fixture: ComponentFixture<CarrierLoadTenderComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [CarrierLoadTenderComponent]
    });
    fixture = TestBed.createComponent(CarrierLoadTenderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
