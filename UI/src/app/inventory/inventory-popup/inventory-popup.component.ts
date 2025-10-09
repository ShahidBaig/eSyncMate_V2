import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Component, OnInit, Inject } from '@angular/core';
import { DatePipe, NgIf, NgFor} from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { ReactiveFormsModule, FormsModule  } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { MatGridListModule } from '@angular/material/grid-list';
import { NgToastService } from 'ng-angular-popup';
import { TranslateModule } from '@ngx-translate/core';
import { FileContentViewerDialogComponent } from '../../file-content-viewer-dialog/file-content-viewer-dialog.component';
import { LanguageService } from '../../services/language.service';

@Component({
  selector: 'inventory-popup',
  templateUrl: './inventory-popup.component.html',
  standalone: true,
  styleUrls: ['./inventory-popup.component.scss'],
  imports: [
    MatButtonToggleModule,
    MatTableModule,
    DatePipe,
    MatCardModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    NgIf,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTooltipModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    NgFor,
    MatGridListModule,
    FormsModule,
    TranslateModule
  ],

})
export class InventorypopupComponent {
  showSpinner: boolean = false;
  showDataColumn: boolean = true;

  columns: string[] = [
    'id',
    'CustomerId',
    'ItemId',
    'type',
    'createdDate',
    'data',
    'fileName',
    'DownloadFile',
  ];

  constructor(@Inject(MAT_DIALOG_DATA) public data: any, public dialogRef: MatDialogRef<InventorypopupComponent>,
    private dialog: MatDialog, private toast: NgToastService, public languageService: LanguageService) {
    }

  downloadFile(data: string, filename: string) {
    this.showSpinner = true;

    const blob = new Blob([data], { type: 'text/plain' });
    const link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = filename;
    link.click();

    this.showSpinner = false;
  }

  closeDialog(): void {
    this.dialogRef.close();
  }

  viewFile(data: string, filename: any) {
    const fileExtension = filename.split('.').pop().toLowerCase();
    let parsedData;

    if (fileExtension === 'json') {
      try {
        parsedData = JSON.parse(data);
      } catch (e) {
        console.error('Error parsing JSON', e);
      }
    } else if (fileExtension === 'edi') {
      data = data.replace(/~/g, '\n');
      parsedData = data;
    } else {
      parsedData = data;
    }

    this.dialog.open(FileContentViewerDialogComponent, {
      data: { content: parsedData, type: fileExtension },
      width: '800px',
    });
  }
}
