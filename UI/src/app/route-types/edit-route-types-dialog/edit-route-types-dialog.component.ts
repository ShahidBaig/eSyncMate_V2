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
import { RouteTypesService } from '../../services/route-types.service';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';


interface RouteTypes {
  id: number;
  name: string;
}

@Component({
  selector: 'edit-route-types-dialog',
  templateUrl: './edit-route-types-dialog.component.html',
  styleUrls: ['./edit-route-types-dialog.component.scss'],
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
export class EditRouteTypesDialogComponent implements OnInit {
  updateRouteTypesForm: FormGroup | any;
  routeTypesOptions: RouteTypes[] | undefined;
  constructor(
    public dialogRef: MatDialogRef<EditRouteTypesDialogComponent>,
    private formBuilder: FormBuilder,
    private RouteTypesApi: RouteTypesService,
    private toast: NgToastService,
    public languageService: LanguageService,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) { }

  ngOnInit() {
    this.initializeForm();
  }

  initializeForm() {
    this.updateRouteTypesForm = this.formBuilder.group({
      id: [this.data.id, Validators.required],
      name: [this.data.name, Validators.required],
      description: [this.data.description, Validators.required], // Add this line
    });

    this.loadRouteTypes();
  }

  loadRouteTypes() {
    this.RouteTypesApi.getRouteTypesData().subscribe({
      next: (res: any) => {
        this.routeTypesOptions = res.routeTypes;
      },
    });
  }

  onCancel() {
    this.dialogRef.close();
  }

  updateRouteTypes(): void {
    const routetypeModel = {
      id: this.updateRouteTypesForm.get('id')?.value,
      name: this.updateRouteTypesForm.get('name')?.value,
      description: this.updateRouteTypesForm.get('description')?.value, // Add this line
    };

    if (this.updateRouteTypesForm.valid) {
      this.RouteTypesApi.updateRouteTypes(routetypeModel).subscribe({
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

          this.dialogRef.close('updated');
        },
        error: (err) => {
          this.toast.error({ detail: "ERROR", summary: err.message, duration: 5000, /*sticky: true,*/ position: 'topRight' });
        }
      });
    }
  }
}
