import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditCustomerPopupComponent } from './edit-customer-popup.component';

describe('EditCustomerPopupComponent', () => {
  let component: EditCustomerPopupComponent;
  let fixture: ComponentFixture<EditCustomerPopupComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EditCustomerPopupComponent]
    });
    fixture = TestBed.createComponent(EditCustomerPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
