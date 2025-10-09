import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { MapsService } from '../../services/maps.service';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

interface MapType {
  id: number;
  name: string;
}

@Component({
  selector: 'add-map-dialog',
  templateUrl: './add-map-dialog.component.html',
  styleUrls: ['./add-map-dialog.component.scss'],
  standalone: true,
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
    CommonModule,
    MatSelectModule,
    FormsModule,
    TranslateModule
  ],
})
export class AddMapDialogComponent implements OnInit {
  newMapForm: FormGroup;
  mapTypesOptions: MapType[] | undefined;

  constructor(
    public dialogRef: MatDialogRef<AddMapDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private MapsApi: MapsService,
    private toast: NgToastService,
    public languageService: LanguageService
  ) {
    this.newMapForm = this.fb.group({
      name: ['', Validators.required],
      typeId: [null, Validators.required], // Form control for map type ID
      map: ['', Validators.required]
    });
  }

  ngOnInit() {
    this.getMapTypesData();
  }

  getMapTypesData() {
    this.MapsApi.getMapTypesData().subscribe({
      next: (res: any) => {
        this.mapTypesOptions = res.mapTypes;
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    const mapModel = {
      name: this.newMapForm.get('name')?.value,
      typeId: this.newMapForm.get('typeId')?.value,
      map: this.newMapForm.get('map')?.value
    };

    if (this.newMapForm.valid) {
      this.MapsApi.saveMap(mapModel).subscribe({
        next: (res) => {
          if (res.code === 100) {
            this.toast.success({ detail: "SUCCESS", summary: res.description, duration: 5000, position: 'topRight' });
          } else if (res.code === 400) {
            this.toast.error({ detail: "ERROR", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          } else if (res.code === 401) {
            this.toast.warning({ detail: "WARNING", summary: res.description, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          } else {
            this.toast.info({ detail: "INFO", summary: res.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
          }

          this.dialogRef.close('saved');
        },
        error: (err) => {
          this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });

        }
      });
    }
  }
}

