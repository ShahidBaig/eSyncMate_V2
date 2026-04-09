import { Component, ElementRef, ViewChild } from '@angular/core';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'route-types-help-dialog',
  templateUrl: './route-types-help-dialog.component.html',
  styleUrls: ['./route-types-help-dialog.component.scss'],
  standalone: true,
  imports: [MatDialogModule, MatIconModule, MatButtonModule, MatDividerModule, CommonModule, MatTooltipModule],
})
export class RouteTypesHelpDialogComponent {
  @ViewChild('helpContent', { static: false }) helpContent!: ElementRef;
  constructor(public dialogRef: MatDialogRef<RouteTypesHelpDialogComponent>) {}
  close(): void { this.dialogRef.close(); }

  downloadHelp(): void {
    const el = this.helpContent?.nativeElement;
    if (!el) return;

    // Extract all CSS from document stylesheets
    let css = '';
    try {
      for (const sheet of Array.from(document.styleSheets)) {
        try {
          for (const rule of Array.from(sheet.cssRules)) {
            css += rule.cssText + '\n';
          }
        } catch (_) {}
      }
    } catch (_) {}

    const content = el.innerHTML;
    const html = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>Route Types — Help Guide</title>
  <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@400;500;600;700;800&display=swap" rel="stylesheet">
  <link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet">
  <style>
    ${css}
    body { margin: 0; background: #f8fafc; font-family: 'Poppins', sans-serif; }
    .help-content { max-height: none !important; overflow: visible !important; padding: 24px 28px !important; background: #f8fafc; }
    .header-actions, .download-btn, .close-btn { display: none !important; }
    mat-icon, .mat-icon { font-family: 'Material Icons' !important; font-size: 20px; font-style: normal; display: inline-block; line-height: 1; text-transform: none; letter-spacing: normal; word-wrap: normal; white-space: nowrap; direction: ltr; -webkit-font-smoothing: antialiased; vertical-align: middle; }
    mat-divider, .mat-divider { display: block; border-top: 1px solid #e2e8f0; margin: 0; }
    @media print { body { background: white; } .help-content { padding: 0 !important; } }
  </style>
</head>
<body>
  <div class="help-content">${content}</div>
</body>
</html>`;
    const blob = new Blob([html], { type: 'text/html' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = 'Route-Types-Help-Guide.html';
    link.click();
    URL.revokeObjectURL(link.href);
  }
}
