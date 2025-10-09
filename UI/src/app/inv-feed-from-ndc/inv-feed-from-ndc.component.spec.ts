import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvFeedFromNDCComponent } from './inv-feed-from-ndc.component';

describe('InvFeedFromNDCComponent', () => {
  let component: InvFeedFromNDCComponent;
  let fixture: ComponentFixture<InvFeedFromNDCComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [InvFeedFromNDCComponent]
    });
    fixture = TestBed.createComponent(InvFeedFromNDCComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
