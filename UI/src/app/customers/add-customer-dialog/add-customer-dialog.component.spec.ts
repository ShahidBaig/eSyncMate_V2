import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddCustomerDialogComponent } from './add-customer-dialog.component';

describe('AddCustomerDialogComponent', () => {
  let component: AddCustomerDialogComponent;
  let fixture: ComponentFixture<AddCustomerDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AddCustomerDialogComponent]
    });
    fixture = TestBed.createComponent(AddCustomerDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
