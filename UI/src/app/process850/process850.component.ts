import { Component } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { NgToastService } from 'ng-angular-popup';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, NgIf } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LanguageService } from '../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'process850',
  templateUrl: './process850.component.html',
  styleUrls: ['./process850.component.scss'],
  standalone: true,
  imports: [
    MatCardModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    DatePipe,
    NgIf,
    MatTooltipModule,
    TranslateModule
  ],
})
export class Process850Component {
  process850 = 'EDI Process 850';
  selectedFile: File | null = null;
  msg: string = '';
  code: number = 0;
  isButtonDisabled: boolean = false;
  showSpinner: boolean = false;

  constructor(private api: ApiService, private toast: NgToastService, public languageService: LanguageService) { }

  onFileSelected(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    if (inputElement.files) {
      this.selectedFile = inputElement.files[0];
    }
  }

  clearFile() {
    const input = document.querySelector('.file-input') as HTMLInputElement;
    if (input) {
      input.value = '';
      this.selectedFile = null;
    }
    this.showSpinner = false;
    this.isButtonDisabled = false;
  }

  uploadFile() {
    if (this.selectedFile == null) {
      this.toast.info({ detail: "INFO", summary: this.languageService.getTranslation('choosefileWarning'), duration: 5000, /*sticky: true,*/ position: 'topRight' });
      return;
    }

    if (this.selectedFile) {
      this.isButtonDisabled = true;
      this.showSpinner = true;

      this.api.uploadFile(this.selectedFile).subscribe(
        {
          next: (res: any) => {
            this.msg = res.message;
            this.code = res.code;
            if (this.code === 200) {
              this.toast.success({ detail: "SUCCESS", summary: this.languageService.getTranslation('file') + ': [ ' +  this.selectedFile?.name  + ' ] ' + this.languageService.getTranslation('successfully') , duration: 5000, sticky: true, position: 'topRight' });
            }
            else if (this.code === 400) {
              this.toast.error({ detail: "ERROR", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
            } else {
              this.toast.info({ detail: "INFO", summary: this.msg, duration: 5000, sticky: true, position: 'topRight' });
            }

            this.clearFile();
          },
          error: (err: any) => {
            this.toast.error({ detail: "ERROR", summary: err, duration: 5000, sticky: true, position: 'topRight' });
            this.isButtonDisabled = false;
            this.showSpinner = false;
          }
        });
    }
  }
}
