import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddConnectorDialogComponent } from './add-connector-dialog.component';

describe('AddConnectorDialogComponent', () => {
  let component: AddConnectorDialogComponent;
  let fixture: ComponentFixture<AddConnectorDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AddConnectorDialogComponent]
    });
    fixture = TestBed.createComponent(AddConnectorDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
