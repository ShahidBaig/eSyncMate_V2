import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RouteDataDialogComponent } from './route-data-dialog.component';

describe('RouteDataDialogComponent', () => {
  let component: RouteDataDialogComponent;
  let fixture: ComponentFixture<RouteDataDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [RouteDataDialogComponent]
    });
    fixture = TestBed.createComponent(RouteDataDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
