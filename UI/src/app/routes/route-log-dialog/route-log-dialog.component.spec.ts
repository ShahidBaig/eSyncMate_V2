import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RouteLogDialogComponent } from './route-log-dialog.component';

describe('RouteLogDialogComponent', () => {
  let component: RouteLogDialogComponent;
  let fixture: ComponentFixture<RouteLogDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [RouteLogDialogComponent]
    });
    fixture = TestBed.createComponent(RouteLogDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
