import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EditPartnerGroupDialogComponent } from './edit-partnergroup-dialog.component';



describe('EditPartnerGroupDialogComponent', () => {
  let component: EditPartnerGroupDialogComponent;
  let fixture: ComponentFixture<EditPartnerGroupDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EditPartnerGroupDialogComponent]
    });
    fixture = TestBed.createComponent(EditPartnerGroupDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
