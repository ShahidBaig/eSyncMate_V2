import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InvFeedDialogComponent } from './inv-feed-dialog.component';

describe('RouteLogDialogComponent', () => {
  let component: InvFeedDialogComponent;
  let fixture: ComponentFixture<InvFeedDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [InvFeedDialogComponent]
    });
    fixture = TestBed.createComponent(InvFeedDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
