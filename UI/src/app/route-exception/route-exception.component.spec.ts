import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RouteExceptionComponent } from './route-exception.component';

describe('RouteExceptionComponent', () => {
  let component: RouteExceptionComponent;
  let fixture: ComponentFixture<RouteExceptionComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [RouteExceptionComponent]
    });
    fixture = TestBed.createComponent(RouteExceptionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
