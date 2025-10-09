import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FileContentViewerDialogComponent } from './file-content-viewer-dialog.component';

describe('FileContentViewerDialogComponent', () => {
  let component: FileContentViewerDialogComponent;
  let fixture: ComponentFixture<FileContentViewerDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [FileContentViewerDialogComponent]
    });
    fixture = TestBed.createComponent(FileContentViewerDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
