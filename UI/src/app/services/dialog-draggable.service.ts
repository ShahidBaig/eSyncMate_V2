import { Injectable } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';

/**
 * Makes EVERY Material dialog in the app draggable, without editing each dialog.
 * Subscribes to MatDialog.afterOpened and wires up drag-by-header on the overlay pane.
 * Dragging starts from the dialog's header/title; clicks on buttons/inputs are ignored.
 */
@Injectable({ providedIn: 'root' })
export class DialogDraggableService {
  // Selectors used to find a "header" to drag from (first match wins). Fallback: whole dialog.
  private readonly handleSelectors =
    '[mat-dialog-title], [matDialogTitle], .mat-mdc-dialog-title, .dialog-header, .drilldown-header, .pfl-header, .dialog-container .dialog-header';

  // Don't start a drag when the user is interacting with these.
  private readonly interactiveSelectors =
    'button, a, input, textarea, select, mat-select, .mat-mdc-select, .mat-mdc-form-field, [mat-icon-button], [mat-button], .mat-mdc-button, .mat-mdc-icon-button';

  constructor(private dialog: MatDialog) {}

  init(): void {
    this.dialog.afterOpened.subscribe((ref: MatDialogRef<any>) => {
      // Defer so the dialog DOM is rendered
      setTimeout(() => this.makeDraggable(ref), 0);
    });
  }

  private makeDraggable(ref: MatDialogRef<any>): void {
    const container = document.getElementById(ref.id);
    if (!container) return;

    const pane = container.closest('.cdk-overlay-pane') as HTMLElement | null;
    if (!pane) return;

    const handle = (container.querySelector(this.handleSelectors) as HTMLElement) || container;
    handle.style.cursor = 'move';
    handle.style.userSelect = 'none';

    let dragging = false;
    let startX = 0, startY = 0;
    let offsetX = 0, offsetY = 0;       // current translate of the pane
    let baseX = 0, baseY = 0;

    const onMouseDown = (e: MouseEvent) => {
      // Ignore drags that begin on interactive controls
      if ((e.target as HTMLElement)?.closest(this.interactiveSelectors)) return;
      dragging = true;
      startX = e.clientX;
      startY = e.clientY;
      baseX = offsetX;
      baseY = offsetY;
      e.preventDefault();
      document.addEventListener('mousemove', onMouseMove);
      document.addEventListener('mouseup', onMouseUp);
    };

    const onMouseMove = (e: MouseEvent) => {
      if (!dragging) return;
      offsetX = baseX + (e.clientX - startX);
      offsetY = baseY + (e.clientY - startY);
      pane.style.transform = `translate(${offsetX}px, ${offsetY}px)`;
    };

    const onMouseUp = () => {
      dragging = false;
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
    };

    handle.addEventListener('mousedown', onMouseDown);

    // Cleanup when the dialog closes
    ref.afterClosed().subscribe(() => {
      handle.removeEventListener('mousedown', onMouseDown);
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
    });
  }
}
