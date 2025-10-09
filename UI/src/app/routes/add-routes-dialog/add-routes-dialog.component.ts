import { Component, inject, Inject, OnInit } from '@angular/core';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormGroup, FormControl, FormBuilder, Validators, ReactiveFormsModule, FormsModule, FormArray } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { RoutesService } from '../../services/routes.service';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipEditedEvent, MatChipInputEvent } from '@angular/material/chips';
import { MatChipsModule } from '@angular/material/chips';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';

interface Customers {
  id: number;
  name: string;
}

interface Connectors {
  id: number;
  name: string;
}

interface Maps {
  id: number;
  name: string;
}

interface RouteTypes {
  id: number;
  name: string;
}

interface PartnerGroup {
  id: number;
  description: string;
}

export interface OnDaylist {
  name: string;
}

export interface ExecutionTimelist {
  name: string;
}
interface Route {
  name: string;
}

@Component({
  selector: 'add-routes-dialog',
  templateUrl: './add-routes-dialog.component.html',
  styleUrls: ['./add-routes-dialog.component.scss'],
  standalone: true,
  providers: [DatePipe],
  imports: [
    MatButtonToggleModule,
    MatTableModule,
    DatePipe,
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
    MatTabsModule,
    MatCheckboxModule,
    MatIconModule,
    MatChipsModule,
    TranslateModule
  ],
})
export class AddRoutesDialogComponent {
  newRouteForm: FormGroup;
  customerOptions: Customers[] | undefined;
  connectorsOptions: Connectors[] | undefined;
  mapsOptions: Maps[] | undefined;
  routeTypesOptions: RouteTypes[] | undefined;
  partnerGroupOptions: PartnerGroup[] | undefined;
  frequencyTypeOptions = ['Minutely', 'Hourly', 'Daily', 'Weekly', 'Monthly'];
  selectedFrequencyType: string = '';
  daysOfWeek: string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  selectedWeekdays: string[] = this.daysOfWeek.filter(day => day !== 'Saturday' && day !== 'Sunday');
  selectedWeekdaysControl: string[] = [];
  selectedDays: any;
  addOnBlur = true;
  readonly separatorKeysCodes = [ENTER, COMMA] as const;
  onDayList: OnDaylist[] = [];
  executionTimelist: ExecutionTimelist[] = [];
  announcer = inject(LiveAnnouncer);
  constructor(
    public dialogRef: MatDialogRef<AddRoutesDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private routeApi: RoutesService,
    private toast: NgToastService,
    private datePipe: DatePipe,
    public languageService: LanguageService
  ) {
    const formControls = this.daysOfWeek.map(day => this.fb.control(false));

    this.newRouteForm = this.fb.group({
      sourceParty: [''],
      destinationParty: [null],
      sourceConnector: [null],
      destinationConnector: [null],
      map: [0],
      partnerGroup: [null],
      routeType: [null],
      status: [null],
      startDate: [''],
      endDate: [''],
      frequencyType: [''],
      selectedWeekday: this.fb.array(formControls),
      onDay: [''],
      onDaylist: [['']],
      executionTime: [''],
      executionTimelist: [[]],
      repeatCount: [0],
      name: [null],
      routeGroup:[''],
    });
  }

  getSelectedWeekdayControl(index: number): FormControl | null {
    const selectedWeekdaysControl = this.newRouteForm.get('selectedWeekday') as FormArray;
    return selectedWeekdaysControl ? selectedWeekdaysControl.at(index) as FormControl : null;
  }

  get showdaysOfWeek(): boolean {
    return this.newRouteForm.get('frequencyType')?.value === 'Weekly';
  }

  get showExecutionTime(): boolean {
    const frequencyTypeValue = this.newRouteForm.get('frequencyType')?.value;

    return frequencyTypeValue === 'Daily' || frequencyTypeValue === 'Weekly';
  }

  get showRepeatCount(): boolean {
    const frequencyTypeValue = this.newRouteForm.get('frequencyType')?.value;

    return frequencyTypeValue === 'Minutely' || frequencyTypeValue === 'Hourly';
  }


  ngOnInit() {
    this.newRouteForm.get('frequencyType')?.valueChanges.subscribe((frequencyType: string) => {
      if (frequencyType === 'Weekly') {
        this.newRouteForm.get('selectedWeekday')?.patchValue(this.daysOfWeek.filter(day => day !== 'Saturday' && day !== 'Sunday'));
      } else {
        this.newRouteForm.get('selectedWeekday')?.patchValue([]);
      }
    });

    this.getCustomersData();
    this.getConnectors();
    this.getMaps();
    this.getPartnerGroup();
    this.getRouteTypes();
  }

  get showOnDayInput(): boolean {
    return this.newRouteForm.get('frequencyType')?.value === 'Monthly';
  }

  updateOnDay(value: string[]): void {
    this.newRouteForm.get('onDay')?.setValue(value);
  }

  getCustomersData() {
    this.routeApi.getCustomersData().subscribe({
      next: (res: any) => {
        this.customerOptions = res.customers;
      },
    });
  }

  getConnectors() {
    this.routeApi.getConnectors().subscribe({
      next: (res: any) => {
        this.connectorsOptions = res.connectors;
      },
    });
  }

  getMaps() {
    this.routeApi.getMaps().subscribe({
      next: (res: any) => {
        this.mapsOptions = res.maps;
      },
    });
  }

  getPartnerGroup() {
    this.routeApi.getPartnerGroup().subscribe({
      next: (res: any) => {
        this.partnerGroupOptions = res.partnerGroup;
      },
    });
  }

  getRouteTypes() {
    this.routeApi.getRouteTypes().subscribe({
      next: (res: any) => {
        this.routeTypesOptions = res.routeType;
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    const OnDay: string = this.onDayList.map(item => item.name).join(',');
    const executionTime: string = this.executionTimelist.map(item => item.name).join(',');
    const selectedWeekdayss = this.daysOfWeek
      .filter((day, index) => this.getSelectedWeekdayControl(index)?.value)
      .join(',');

    const routeModel = {
      sourcePartyId: this.newRouteForm.get('sourceParty')?.value,
      destinationPartyId: this.newRouteForm.get('destinationParty')?.value,
      typeId: this.newRouteForm.get('routeType')?.value,
      sourceConnectorId: this.newRouteForm.get('sourceConnector')?.value,
      destinationConnectorId: this.newRouteForm.get('destinationConnector')?.value,
      mapId: this.newRouteForm.get('map')?.value,
      partyGroupId: this.newRouteForm.get('partnerGroup')?.value,
      status: this.newRouteForm.get('status')?.value,
      frequencyType: this.newRouteForm.get('frequencyType')?.value,
      startDate: this.newRouteForm.get('startDate')?.value ? this.datePipe.transform(this.newRouteForm.get('startDate')?.value, 'yyyy-MM-ddTHH:mm:ss') : '1900-01-01',
      endDate: this.newRouteForm.get('endDate')?.value ? this.datePipe.transform(this.newRouteForm.get('endDate')?.value, 'yyyy-MM-ddTHH:mm:ss') : '1900-01-01',
      repeatCount: this.newRouteForm.get('repeatCount')?.value,
      weekDays: selectedWeekdayss,
      onDay: OnDay,
      executionTime: executionTime,
      name: this.newRouteForm.get('name')?.value,
      routeGroup: this.newRouteForm.get('routeGroup')?.value,
    };

    /* if (this.newRouteForm.valid) {*/

    this.routeApi.saveRoute(routeModel).subscribe({
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
    //}
  }

  executionTimeAdd(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();

    if (value) {
      this.executionTimelist.push({ name: value });
    }
    event.chipInput!.clear();
  }

  executionTimeRemove(fruit: ExecutionTimelist): void {
    const index = this.executionTimelist.indexOf(fruit);

    if (index >= 0) {
      this.executionTimelist.splice(index, 1);

      this.announcer.announce(`Removed ${fruit}`);
    }
  }

  executionTimeEdit(fruit: ExecutionTimelist, event: MatChipEditedEvent) {
    const value = event.value.trim();

    if (!value) {
      this.remove(fruit);
      return;
    }

    const index = this.executionTimelist.indexOf(fruit);
    if (index >= 0) {
      this.executionTimelist[index].name = value;
    }
  }


  add(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();

    if (value) {
      this.onDayList.push({ name: value });
    }

    event.chipInput!.clear();
  }

  remove(fruit: OnDaylist): void {
    const index = this.onDayList.indexOf(fruit);

    if (index >= 0) {
      this.onDayList.splice(index, 1);

      this.announcer.announce(`Removed ${fruit}`);
    }
  }

  edit(fruit: OnDaylist, event: MatChipEditedEvent) {
    const value = event.value.trim();

    if (!value) {
      this.remove(fruit);
      return;
    }

    const index = this.onDayList.indexOf(fruit);
    if (index >= 0) {
      this.onDayList[index].name = value;
    }
  }
}
