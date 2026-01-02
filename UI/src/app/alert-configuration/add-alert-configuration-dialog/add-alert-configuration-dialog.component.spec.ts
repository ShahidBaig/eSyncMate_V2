import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddAlertConfigurationDialogComponent } from './add-alert-configuration-dialog.component';

describe('AddAlertConfigurationDialogComponent', () => {
  let component: AddAlertConfigurationDialogComponent;
  let fixture: ComponentFixture<AddAlertConfigurationDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AddAlertConfigurationDialogComponent]
    });
    fixture = TestBed.createComponent(AddAlertConfigurationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
