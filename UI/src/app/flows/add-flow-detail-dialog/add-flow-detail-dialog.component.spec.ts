import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddFlowDetailDialogComponent } from './add-flow-detail-dialog.component';

describe('AddFlowDetailDialogComponent', () => {
  let component: AddFlowDetailDialogComponent;
  let fixture: ComponentFixture<AddFlowDetailDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AddFlowDetailDialogComponent]
    });
    fixture = TestBed.createComponent(AddFlowDetailDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
