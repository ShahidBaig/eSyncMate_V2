import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomerAlertsDialogComponent } from './customer-alerts-dialog.component';

describe('CustomerAlertsDialogComponent', () => {
  let component: CustomerAlertsDialogComponent;
  let fixture: ComponentFixture<CustomerAlertsDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CustomerAlertsDialogComponent]
    });
    fixture = TestBed.createComponent(CustomerAlertsDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
