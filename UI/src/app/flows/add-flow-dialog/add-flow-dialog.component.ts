import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { DatePipe, NgIf, CommonModule } from '@angular/common';
import { MatTable, MatTableModule } from '@angular/material/table';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { FormGroup, FormControl, FormBuilder, ReactiveFormsModule, FormsModule, FormArray } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { NgToastService } from 'ng-angular-popup';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogRef } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatChipInputEvent, MatChipsModule } from '@angular/material/chips';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { LanguageService } from '../../services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { FlowsService } from '../../services/flows.service';
import { RoutesService } from '../../services/routes.service';
import { CustomerProductCatalogService } from '../../services/customerProductCatalogDialog.service';

interface Customers {
    erpCustomerID: string;
    name: string;
    id: any;
}

interface RouteOption {
    id: number;
    name: string;
    customerName?: string;
}

@Component({
    selector: 'add-flow-dialog',
    templateUrl: './add-flow-dialog.component.html',
    styleUrls: ['./add-flow-dialog.component.scss'],
    standalone: true,
    providers: [DatePipe],
    imports: [
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
        MatChipsModule,
        TranslateModule
    ],
})
export class AddFlowDialogComponent implements OnInit {
    newFlowForm: FormGroup;
    customerOptions: Customers[] | undefined;
    allRouteOptions: RouteOption[] = [];
    filteredRouteOptions: RouteOption[] = [];
    frequencyTypeOptions = ['Minutely', 'Hourly', 'Daily', 'Weekly', 'Monthly'];
    daysOfWeek: string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    addOnBlur = true;
    readonly separatorKeysCodes = [ENTER, COMMA] as const;
    announcer = inject(LiveAnnouncer);

    detailOnDayLists: { name: string }[][] = [];
    detailExecutionTimeLists: { name: string }[][] = [];

    // Input form for adding new details
    detailInputForm: FormGroup;

    // Table columns for the details table
    detailTableColumns: string[] = ['index', 'routeName', 'status', 'inOut', 'startDate', 'endDate', 'actions'];

    @ViewChild('detailsTable') detailsTable!: MatTable<any>;

    constructor(
        public dialogRef: MatDialogRef<AddFlowDialogComponent>,
        private fb: FormBuilder,
        private flowsApi: FlowsService,
        private routeApi: RoutesService,
        private ERPApi: CustomerProductCatalogService,
        private toast: NgToastService,
        private datePipe: DatePipe,
        public languageService: LanguageService
    ) {
        this.newFlowForm = this.fb.group({
            customerID: [''],
            title: [''],
            description: [''],
            status: ['Active'],
            flowDetails: this.fb.array([])
        });

        // Initialize the standalone input form for adding new details
        this.detailInputForm = this.fb.group({
            routeId: [null],
            status: ['Active'],
            in_Out: [''],
            frequencyType: [''],
            startDate: [''],
            endDate: [''],
            repeatCount: [0],
            selectedWeekday: this.fb.array(this.daysOfWeek.map(() => this.fb.control(false))),
            onDay: [''],
            executionTime: [''],
        });
    }

    get flowDetails(): FormArray {
        return this.newFlowForm.get('flowDetails') as FormArray;
    }

    ngOnInit() {
        this.getCustomersData();
        this.getRoutes();

        // Listen for customer changes to re-filter routes
        this.newFlowForm.get('customerID')?.valueChanges.subscribe(() => {
            this.filterRoutesByCustomer();
        });
    }

    getCustomersData() {
        this.ERPApi.getERPCustomers().subscribe({
            next: (res: any) => {
                this.customerOptions = res.customers;
                this.filterRoutesByCustomer();
            },
        });
    }

    getRoutes() {
        this.routeApi.getRouteName().subscribe({
            next: (res: any) => {
                this.allRouteOptions = res.routes ?? res;
                this.filterRoutesByCustomer();
            },
        });
    }

    filterRoutesByCustomer() {
        const selectedCustomerID = this.newFlowForm.get('customerID')?.value;
        if (!selectedCustomerID || !this.customerOptions || !this.allRouteOptions) {
            this.filteredRouteOptions = this.allRouteOptions || [];
            return;
        }

        const selectedCustomer = this.customerOptions.find(c => c.erpCustomerID === selectedCustomerID);
        if (!selectedCustomer) {
            this.filteredRouteOptions = this.allRouteOptions;
            return;
        }

        this.filteredRouteOptions = this.allRouteOptions.filter(
            route => route.customerName && route.customerName.toLowerCase() === selectedCustomer.name.toLowerCase()
        );
    }

    addDetailFromForm() {
        const formVal = this.detailInputForm.value;

        if (!formVal.routeId) {
            this.toast.warning({ detail: "WARNING", summary: "Please select a Route Name", duration: 3000, position: 'topRight' });
            return;
        }

        // Check for duplicate route
        const isDuplicate = this.flowDetails.controls.some(
            ctrl => (ctrl as FormGroup).get('routeId')?.value === formVal.routeId
        );
        if (isDuplicate) {
            this.toast.warning({ detail: "WARNING", summary: "This route is already added!", duration: 3000, position: 'topRight' });
            return;
        }

        const weekDaysArr = formVal.selectedWeekday || [];
        const weekdayControls = this.daysOfWeek.map((_: string, i: number) => this.fb.control(weekDaysArr[i] || false));

        const group = this.fb.group({
            routeId: [formVal.routeId],
            status: [formVal.status || 'Active'],
            in_Out: [formVal.in_Out || ''],
            frequencyType: [formVal.frequencyType || ''],
            startDate: [formVal.startDate || ''],
            endDate: [formVal.endDate || ''],
            repeatCount: [formVal.repeatCount || 0],
            selectedWeekday: this.fb.array(weekdayControls),
            onDay: [''],
            executionTime: [''],
        });
        this.flowDetails.push(group);
        this.detailOnDayLists.push([]);
        this.detailExecutionTimeLists.push([]);

        // Reset the input form
        this.detailInputForm.reset({
            routeId: null,
            status: 'Active',
            in_Out: '',
            frequencyType: '',
            startDate: '',
            endDate: '',
            repeatCount: 0,
        });
        const weekdayArray = this.detailInputForm.get('selectedWeekday') as FormArray;
        weekdayArray.controls.forEach(ctrl => ctrl.setValue(false));

        // Re-render the table
        if (this.detailsTable) {
            this.detailsTable.renderRows();
        }
    }

    removeFlowDetail(index: number) {
        this.flowDetails.removeAt(index);
        this.detailOnDayLists.splice(index, 1);
        this.detailExecutionTimeLists.splice(index, 1);

        if (this.detailsTable) {
            this.detailsTable.renderRows();
        }
    }

    getWeekdayCtrl(di: number, wi: number): FormControl | null {
        const arr = (this.flowDetails.at(di) as FormGroup).get('selectedWeekday') as FormArray;
        return arr ? arr.at(wi) as FormControl : null;
    }

    getInputWeekdayCtrl(wi: number): FormControl | null {
        const arr = this.detailInputForm.get('selectedWeekday') as FormArray;
        return arr ? arr.at(wi) as FormControl : null;
    }

    showDaysOfWeek(i: number): boolean {
        return (this.flowDetails.at(i) as FormGroup).get('frequencyType')?.value === 'Weekly';
    }

    showExecutionTime(i: number): boolean {
        const v = (this.flowDetails.at(i) as FormGroup).get('frequencyType')?.value;
        return v === 'Daily' || v === 'Weekly';
    }

    showRepeatCount(i: number): boolean {
        const v = (this.flowDetails.at(i) as FormGroup).get('frequencyType')?.value;
        return v === 'Minutely' || v === 'Hourly';
    }

    showOnDayInput(i: number): boolean {
        return (this.flowDetails.at(i) as FormGroup).get('frequencyType')?.value === 'Monthly';
    }

    showInputDaysOfWeek(): boolean {
        return this.detailInputForm.get('frequencyType')?.value === 'Weekly';
    }

    showInputExecutionTime(): boolean {
        const v = this.detailInputForm.get('frequencyType')?.value;
        return v === 'Daily' || v === 'Weekly';
    }

    showInputRepeatCount(): boolean {
        const v = this.detailInputForm.get('frequencyType')?.value;
        return v === 'Minutely' || v === 'Hourly';
    }

    showInputOnDayInput(): boolean {
        return this.detailInputForm.get('frequencyType')?.value === 'Monthly';
    }

    getRouteNameById(routeId: number): string {
        if (!this.allRouteOptions) return routeId?.toString() || '';
        const route = this.allRouteOptions.find(r => r.id === routeId);
        return route ? route.name : (routeId?.toString() || '');
    }

    addOnDay(event: MatChipInputEvent, i: number): void {
        const value = (event.value || '').trim();
        if (value) { this.detailOnDayLists[i].push({ name: value }); }
        event.chipInput!.clear();
    }

    removeOnDay(item: { name: string }, i: number): void {
        const idx = this.detailOnDayLists[i].indexOf(item);
        if (idx >= 0) { this.detailOnDayLists[i].splice(idx, 1); }
    }

    addExecutionTime(event: MatChipInputEvent, i: number): void {
        const value = (event.value || '').trim();
        if (value) { this.detailExecutionTimeLists[i].push({ name: value }); }
        event.chipInput!.clear();
    }

    removeExecutionTime(item: { name: string }, i: number): void {
        const idx = this.detailExecutionTimeLists[i].indexOf(item);
        if (idx >= 0) { this.detailExecutionTimeLists[i].splice(idx, 1); }
    }

    onCancel(): void {
        this.dialogRef.close();
    }

    onSave(): void {
        const details = this.flowDetails.controls.map((ctrl, i) => {
            const d = ctrl as FormGroup;
            const weekDays = this.daysOfWeek.filter((_, wi) => this.getWeekdayCtrl(i, wi)?.value).join(',');
            const onDay = this.detailOnDayLists[i].map(x => x.name).join(',');
            const execTime = this.detailExecutionTimeLists[i].map(x => x.name).join(',');
            return {
                routeId: d.get('routeId')?.value,
                status: d.get('status')?.value,
                in_Out: d.get('in_Out')?.value,
                frequencyType: d.get('frequencyType')?.value,
                startDate: d.get('startDate')?.value ? new Date(d.get('startDate')?.value).toISOString() : '1900-01-01T00:00:00',
                endDate: d.get('endDate')?.value ? new Date(d.get('endDate')?.value).toISOString() : '1900-01-01T00:00:00',
                repeatCount: d.get('repeatCount')?.value || 0,
                weekDays: weekDays,
                onDay: onDay,
                executionTime: execTime,
            };
        });

        const flowModel = {
            customerID: this.newFlowForm.get('customerID')?.value,
            title: this.newFlowForm.get('title')?.value,
            description: this.newFlowForm.get('description')?.value,
            status: this.newFlowForm.get('status')?.value,
            flowDetails: details
        };

        this.flowsApi.createFlow(flowModel).subscribe({
            next: (res) => {
                if (res.code === 100) {
                    this.toast.success({ detail: "SUCCESS", summary: res.description || res.message || 'Flow created!', duration: 5000, position: 'topRight' });
                    this.dialogRef.close('saved');
                } else {
                    this.toast.error({ detail: "ERROR", summary: res.description || res.message || 'Failed to create flow', duration: 5000, position: 'topRight' });
                }
            },
            error: (err) => {
                this.toast.error({ detail: "ERROR", summary: err.error?.message || err.message || 'Server error', duration: 5000, position: 'topRight' });
            }
        });
    }
}
