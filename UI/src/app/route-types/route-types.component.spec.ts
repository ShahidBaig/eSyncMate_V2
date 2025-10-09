import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RouteTypesComponent } from './route-types.component';

describe('RouteTypesComponent', () => {
  let component: RouteTypesComponent;
  let fixture: ComponentFixture<RouteTypesComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [RouteTypesComponent]
    });
    fixture = TestBed.createComponent(RouteTypesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
