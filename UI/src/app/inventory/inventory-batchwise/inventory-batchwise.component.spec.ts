import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventoryBatchwiseComponent } from './inventory-batchwise.component';

describe('InventoryBatchwiseComponent', () => {
  let component: InventoryBatchwiseComponent;
  let fixture: ComponentFixture<InventoryBatchwiseComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [InventoryBatchwiseComponent]
    });
    fixture = TestBed.createComponent(InventoryBatchwiseComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
