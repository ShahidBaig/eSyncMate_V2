import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { NgxJsonViewerModule } from 'ngx-json-viewer';
import { MatDialogModule } from '@angular/material/dialog';
import { DatePipe, NgIf } from '@angular/common';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'file-content-viewer-dialog',
  templateUrl: './file-content-viewer-dialog.component.html',
  styleUrls: ['./file-content-viewer-dialog.component.scss'],
  standalone: true,
  imports: [
    NgxJsonViewerModule,
    MatDialogModule,
    DatePipe,
    NgIf,
    TranslateModule
  ],
})
export class FileContentViewerDialogComponent {
  constructor(public languageService: LanguageService, @Inject(MAT_DIALOG_DATA) public data: { content: any, type: string,routeName:string }) { }
}
