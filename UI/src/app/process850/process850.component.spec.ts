import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Process850Component } from './process850.component';

describe('Process850Component', () => {
  let component: Process850Component;
  let fixture: ComponentFixture<Process850Component>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [Process850Component]
    });
    fixture = TestBed.createComponent(Process850Component);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
