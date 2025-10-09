import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StoresOrderComponent } from './stores-order.component';

describe('StoresOrderComponent', () => {
  let component: StoresOrderComponent;
  let fixture: ComponentFixture<StoresOrderComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [StoresOrderComponent]
    });
    fixture = TestBed.createComponent(StoresOrderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
