import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PartnerGroupsComponent } from './partnergroups.component';

describe('PartnerGroupsComponent', () => {
  let component: PartnerGroupsComponent;
  let fixture: ComponentFixture<PartnerGroupsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [PartnerGroupsComponent]
    });
    fixture = TestBed.createComponent(PartnerGroupsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
