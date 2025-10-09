import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConnectorsComponent } from './connectors.component';

describe('ConnectorsComponent', () => {
  let component: ConnectorsComponent;
  let fixture: ComponentFixture<ConnectorsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ConnectorsComponent]
    });
    fixture = TestBed.createComponent(ConnectorsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
