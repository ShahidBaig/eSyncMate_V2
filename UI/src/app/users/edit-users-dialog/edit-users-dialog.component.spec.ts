import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditUsersDialogComponent } from './edit-users-dialog.component';

describe('EditUsersDialogComponent', () => {
  let component: EditUsersDialogComponent;
  let fixture: ComponentFixture<EditUsersDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EditUsersDialogComponent]
    });
    fixture = TestBed.createComponent(EditUsersDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
