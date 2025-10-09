import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditRouteTypesDialogComponent } from './edit-route-types-dialog.component';

describe('EditRouteTypesDialogComponent', () => {
  let component: EditRouteTypesDialogComponent;
  let fixture: ComponentFixture<EditRouteTypesDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EditRouteTypesDialogComponent]
    });
    fixture = TestBed.createComponent(EditRouteTypesDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
