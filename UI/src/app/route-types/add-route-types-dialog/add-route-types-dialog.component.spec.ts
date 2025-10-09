import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddRouteTypesDialogComponent } from './add-route-types-dialog.component';

describe('AddRouteTypesDialogComponent', () => {
  let component: AddRouteTypesDialogComponent;
  let fixture: ComponentFixture<AddRouteTypesDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AddRouteTypesDialogComponent]
    });
    fixture = TestBed.createComponent(AddRouteTypesDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
