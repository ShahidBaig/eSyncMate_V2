import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddRoutesDialogComponent } from './add-routes-dialog.component';

describe('AddRoutesDialogComponent', () => {
  let component: AddRoutesDialogComponent;
  let fixture: ComponentFixture<AddRoutesDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AddRoutesDialogComponent]
    });
    fixture = TestBed.createComponent(AddRoutesDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
