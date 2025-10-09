import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditMapDialogComponent } from './edit-map-dialog.component';

describe('EditMapDialogComponent', () => {
  let component: EditMapDialogComponent;
  let fixture: ComponentFixture<EditMapDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EditMapDialogComponent]
    });
    fixture = TestBed.createComponent(EditMapDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
