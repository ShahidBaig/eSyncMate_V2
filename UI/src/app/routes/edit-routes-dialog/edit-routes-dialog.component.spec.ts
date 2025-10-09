import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditRoutesDialogComponent } from './edit-routes-dialog.component';

describe('EditRoutesDialogComponent', () => {
  let component: EditRoutesDialogComponent;
  let fixture: ComponentFixture<EditRoutesDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EditRoutesDialogComponent]
    });
    fixture = TestBed.createComponent(EditRoutesDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
