import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditAlertConfigurationComponent } from './edit-alert-configuration.component';

describe('EditAlertConfigurationComponent', () => {
  let component: EditAlertConfigurationComponent;
  let fixture: ComponentFixture<EditAlertConfigurationComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EditAlertConfigurationComponent]
    });
    fixture = TestBed.createComponent(EditAlertConfigurationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
