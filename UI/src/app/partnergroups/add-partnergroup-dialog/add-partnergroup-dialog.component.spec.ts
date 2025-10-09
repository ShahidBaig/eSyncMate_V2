import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddPartnerGroupDialogComponent } from './add-partnergroup-dialog.component';

describe('AddPartnerGroupDialogComponent', () => {
  let component: AddPartnerGroupDialogComponent;
  let fixture: ComponentFixture<AddPartnerGroupDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AddPartnerGroupDialogComponent]
    });
    fixture = TestBed.createComponent(AddPartnerGroupDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
