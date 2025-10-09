import { Component, inject, Inject, OnInit } from '@angular/core';
import { DatePipe, NgIf, formatDate } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
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
import { MatCheckboxModule } from '@angular/material/checkbox';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
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

@Component({
  selector: 'edit-routes-dialog',
  templateUrl: './edit-routes-dialog.component.html',
  styleUrls: ['./edit-routes-dialog.component.scss'],
  standalone: true,
  providers: [DatePipe],
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
    MatTabsModule,
    MatCheckboxModule,
    MatChipsModule,
    TranslateModule
  ],
})
export class EditRoutesDialogComponent {

  updateRouteForm: FormGroup;
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
    public dialogRef: MatDialogRef<EditRoutesDialogComponent>,
    private formBuilder: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private routeApi: RoutesService,
    private toast: NgToastService,
    private datePipe: DatePipe,
    public languageService: LanguageService
  ) {
    const formControls = this.daysOfWeek.map(day => this.fb.control(false));

    this.updateRouteForm = this.fb.group({
      sourceParty: [''],
      destinationParty: [null],
      sourceConnector: [null],
      destinationConnector: [null],
      map: [null],
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

  ngOnInit() {
    this.initializeForm();
  }

  initializeForm() {
    const weekDaysArray = this.data.weekDays.split(',');
    const formControls = this.daysOfWeek.map(day => this.fb.control(weekDaysArray.includes(day)));
    this.getCustomersData();
    this.getConnectors();
    this.getMaps();
    this.getPartnerGroup();
    this.getRouteTypes();
    if (this.data.onDay != "") {
      this.onDayList = this.data.onDay.split(',').map((name: any) => ({ name }));
    }

    if (this.data.executionTime != "") {
      this.executionTimelist = this.data.executionTime.split(',').map((name: any) => ({ name }));
    }

    this.updateRouteForm = this.formBuilder.group({
      id: this.data.id,
      sourceParty: this.data.sourcePartyId,
      destinationParty: this.data.destinationPartyId,
      sourceConnector: this.data.sourceConnectorId,
      destinationConnector: this.data.destinationConnectorId,
      map: this.data.mapId,
      partnerGroup: this.data.partnerGroupId,
      routeType: this.data.typeId,
      route: this.data.typeId,
      status: this.data.status,
      frequencyType: this.data.frequencyType,
      startDate: this.data.startDate,
      endDate: this.data.endDate,
      repeatCount: this.data.repeatCount,
      selectedWeekday: this.fb.array(formControls),
      executionTimelist: this.executionTimelist,
      onDayList: this.onDayList,
      onDay: [''],
      executionTime: [''],
      name: this.data.name,
      routeGroup: this.data.routeGroup,
    });
  }

  executionTimeAdd(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();

    // Add our fruit
    if (value) {
      this.executionTimelist.push({ name: value });
    }

    // Clear the input value
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

    // Remove fruit if it no longer has a name
    if (!value) {
      this.remove(fruit);
      return;
    }

    // Edit existing fruit
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

  getSelectedWeekdayControl(index: number): FormControl | null {
    const selectedWeekdaysControl = this.updateRouteForm.get('selectedWeekday') as FormArray;
    return selectedWeekdaysControl ? selectedWeekdaysControl.at(index) as FormControl : null;
  }

  get showdaysOfWeek(): boolean {
    return this.updateRouteForm.get('frequencyType')?.value === 'Weekly';
  }

  get showOnDayInput(): boolean {
    return this.updateRouteForm.get('frequencyType')?.value === 'Monthly';
  }

  updateOnDay(value: string[]): void {
    this.updateRouteForm.get('onDay')?.setValue(value);
  }

  get showExecutionTime(): boolean {
    const frequencyTypeValue = this.updateRouteForm.get('frequencyType')?.value;

    return frequencyTypeValue === 'Daily' || frequencyTypeValue === 'Weekly';
  }

  get showRepeatCount(): boolean {
    const frequencyTypeValue = this.updateRouteForm.get('frequencyType')?.value;

    return frequencyTypeValue === 'Minutely' || frequencyTypeValue === 'Hourly';
  }

  updateRoute(): void {

    const OnDay: string = this.onDayList.map(item => item.name).join(',');
    const executionTime: string = this.executionTimelist.map(item => item.name).join(',');
    const selecteddaysOfWeek = this.daysOfWeek
      .filter((day, index) => this.getSelectedWeekdayControl(index)?.value)
      .join(',');

    const routeModel = {
      id: this.updateRouteForm.get('id')?.value,
      sourcePartyId: this.updateRouteForm.get('sourceParty')?.value,
      destinationPartyId: this.updateRouteForm.get('destinationParty')?.value,
      typeId: this.updateRouteForm.get('routeType')?.value,
      sourceConnectorId: this.updateRouteForm.get('sourceConnector')?.value,
      destinationConnectorId: this.updateRouteForm.get('destinationConnector')?.value,
      mapId: this.updateRouteForm.get('map')?.value,
      partyGroupId: this.updateRouteForm.get('partnerGroup')?.value,
      status: this.updateRouteForm.get('status')?.value,
      frequencyType: this.updateRouteForm.get('frequencyType')?.value,
      startDate: this.updateRouteForm.get('startDate')?.value ? this.datePipe.transform(this.updateRouteForm.get('startDate')?.value, 'yyyy-MM-ddTHH:mm:ss') : '1900-01-01',
      endDate: this.updateRouteForm.get('endDate')?.value ? this.datePipe.transform(this.updateRouteForm.get('endDate')?.value, 'yyyy-MM-ddTHH:mm:ss') : '1900-01-01',
      repeatCount: this.updateRouteForm.get('repeatCount')?.value,
      weekDays: selecteddaysOfWeek,
      onDay: OnDay,
      executionTime: executionTime,
      name: this.updateRouteForm.get('name')?.value,
      routeGroup: this.updateRouteForm.get('routeGroup')?.value,
    };

    if (this.updateRouteForm.valid) {
      this.routeApi.updateRoute(routeModel).subscribe({
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
