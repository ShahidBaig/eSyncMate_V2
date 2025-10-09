import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EdiFileCounterComponent } from './edi-file-counter.component';

describe('EdiFileCounterComponent', () => {
  let component: EdiFileCounterComponent;
  let fixture: ComponentFixture<EdiFileCounterComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EdiFileCounterComponent]
    });
    fixture = TestBed.createComponent(EdiFileCounterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
