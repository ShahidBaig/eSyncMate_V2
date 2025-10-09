import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditConnectorDialogComponent } from './edit-connector-dialog.component';

describe('EditConnectorDialogComponent', () => {
  let component: EditConnectorDialogComponent;
  let fixture: ComponentFixture<EditConnectorDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EditConnectorDialogComponent]
    });
    fixture = TestBed.createComponent(EditConnectorDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
