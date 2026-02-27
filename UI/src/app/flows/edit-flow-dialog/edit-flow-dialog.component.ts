import { Component, inject, Inject, OnInit, ViewChild } from '@angular/core';
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
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
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
    status?: string;
    frequencyType?: string;
    startDate?: string;
    endDate?: string;
    repeatCount?: number;
    weekDays?: string;
    onDay?: string;
    executionTime?: string;
}

@Component({
    selector: 'edit-flow-dialog',
    templateUrl: './edit-flow-dialog.component.html',
    styleUrls: ['./edit-flow-dialog.component.scss'],
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
export class EditFlowDialogComponent implements OnInit {
    editFlowForm: FormGroup;
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

    // Chip lists for the input form (autofilled from API)
    inputOnDayList: { name: string }[] = [];
    inputExecutionTimeList: { name: string }[] = [];

    // Track whether we are editing an existing detail row
    editingDetailIndex: number | null = null;

    // Input form for adding new details
    detailInputForm: FormGroup;

    // Table columns for the details table
    detailTableColumns: string[] = ['index', 'routeName', 'status', 'inOut', 'startDate', 'endDate', 'actions'];

    @ViewChild('detailsTable') detailsTable!: MatTable<any>;

    constructor(
        public dialogRef: MatDialogRef<EditFlowDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: any,
        private fb: FormBuilder,
        private flowsApi: FlowsService,
        private routeApi: RoutesService,
        private ERPApi: CustomerProductCatalogService,
        private toast: NgToastService,
        private datePipe: DatePipe,
        public languageService: LanguageService
    ) {
        this.editFlowForm = this.fb.group({
            id: [data.id],
            customerID: [data.customerID],
            title: [data.title],
            description: [data.description],
            status: [data.status],
            flowDetails: this.fb.array([])
        });

        // Initialize the standalone input form for adding new details
        this.detailInputForm = this.fb.group({
            routeId: [null],
            status: ['Active'],
            in_Out: [{ value: '', disabled: true }],
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
        return this.editFlowForm.get('flowDetails') as FormArray;
    }

    ngOnInit() {
        this.getCustomersData();
        this.getRoutes();
        this.initFlowDetails();

        // Listen for customer changes to re-filter routes and reset route selection
        this.editFlowForm.get('customerID')?.valueChanges.subscribe(() => {
            this.filterRoutesByCustomer();
            // Reset the route input form since the old route may not belong to the new customer
            this.detailInputForm.patchValue({ routeId: null });
            this.inputOnDayList = [];
            this.inputExecutionTimeList = [];
        });

        // Listen for route selection to autofill default data
        this.detailInputForm.get('routeId')?.valueChanges.subscribe((routeId) => {
            if (routeId) {
                // Check if this route already exists in the details table
                const existingIndex = this.flowDetails.controls.findIndex(
                    ctrl => (ctrl as FormGroup).get('routeId')?.value === routeId
                );
                if (existingIndex >= 0) {
                    // Load existing detail into the form for editing
                    this.loadDetailIntoForm(existingIndex);
                } else {
                    // New route — autofill from route data / API
                    this.editingDetailIndex = null;
                    this.onRouteSelected(routeId);
                }
            }
        });
    }

    getCustomersData() {
        this.ERPApi.getERPCustomers().subscribe({
            next: (res: any) => {
                this.customerOptions = res.customers;
                // Filter routes once customers are loaded
                this.filterRoutesByCustomer();
            },
        });
    }

    getRoutes() {
        this.routeApi.getRouteName().subscribe({
            next: (res: any) => {
                const raw: any[] = res.routes ?? res;
                this.allRouteOptions = raw.map((r: any) => ({
                    id: r.id,
                    name: r.name,
                    customerName: r.customerName,
                    status: r.status,
                    frequencyType: r.frequencyType,
                    startDate: r.startDate,
                    endDate: r.endDate,
                    repeatCount: r.repeatCount,
                    weekDays: r.weekDays,
                    onDay: r.onDay,
                    executionTime: r.executionTime,
                }));
                // Filter routes once routes are loaded
                this.filterRoutesByCustomer();
            },
        });
    }

    filterRoutesByCustomer() {
        const selectedCustomerID = this.editFlowForm.get('customerID')?.value;
        if (!selectedCustomerID || !this.customerOptions || !this.allRouteOptions) {
            this.filteredRouteOptions = [];
            return;
        }

        // Find the customer name for the selected customerID
        const selectedCustomer = this.customerOptions.find(c => c.erpCustomerID === selectedCustomerID);
        if (!selectedCustomer) {
            this.filteredRouteOptions = [];
            return;
        }

        // Filter routes by matching customerName
        this.filteredRouteOptions = this.allRouteOptions.filter(
            route => route.customerName && route.customerName.toLowerCase() === selectedCustomer.name.toLowerCase()
        );
    }

    onRouteSelected(routeId: number) {
        // Step 1: Instantly fill from cached route data (real-time, no extra API call needed)
        const route = this.allRouteOptions.find(r => r.id === routeId);
        if (route) {
            this.detailInputForm.patchValue({
                frequencyType: route.frequencyType || '',
                startDate: route.startDate ? new Date(route.startDate) : '',
                endDate: route.endDate ? new Date(route.endDate) : '',
                repeatCount: route.repeatCount ?? 0,
            });

            // Set weekday checkboxes from route
            const weekDaysArr = route.weekDays ? route.weekDays.split(',').map((s: string) => s.trim()) : [];
            const weekdayFormArray = this.detailInputForm.get('selectedWeekday') as FormArray;
            this.daysOfWeek.forEach((day, i) => {
                weekdayFormArray.at(i).setValue(weekDaysArr.includes(day));
            });

            // Set OnDay chip list from route
            this.inputOnDayList = route.onDay && route.onDay !== ''
                ? route.onDay.split(',').map((n: string) => ({ name: n.trim() }))
                : [];

            // Set ExecutionTime chip list from route
            this.inputExecutionTimeList = route.executionTime && route.executionTime !== ''
                ? route.executionTime.split(',').map((n: string) => ({ name: n.trim() }))
                : [];
        }

        // Step 2: Also call API for existing-flow overrides (non-blocking)
        const customerId = this.editFlowForm.get('customerID')?.value || '';
        if (customerId) {
            this.flowsApi.getAutofillByRouteId(customerId, routeId).subscribe({
                next: (res: any) => {
                    if (res.data) {
                        const d = res.data;
                        // Only override fields where the API returns actual non-empty values
                        const patch: any = {};
                        if (d.frequencyType) patch.frequencyType = d.frequencyType;
                        if (d.startDate) patch.startDate = new Date(d.startDate);
                        if (d.endDate) patch.endDate = new Date(d.endDate);
                        if (d.repeatCount) patch.repeatCount = parseInt(d.repeatCount, 10);
                        if (Object.keys(patch).length > 0) {
                            this.detailInputForm.patchValue(patch);
                        }

                        // Override weekday checkboxes if API provides them
                        if (d.weekDays) {
                            const weekDaysArr = d.weekDays.split(',').map((s: string) => s.trim());
                            const weekdayFormArray = this.detailInputForm.get('selectedWeekday') as FormArray;
                            this.daysOfWeek.forEach((day, i) => {
                                weekdayFormArray.at(i).setValue(weekDaysArr.includes(day));
                            });
                        }

                        // Override OnDay chip list if API provides it
                        if (d.onDay && d.onDay !== '') {
                            this.inputOnDayList = d.onDay.split(',').map((n: string) => ({ name: n.trim() }));
                        }

                        // Override ExecTime chip list if API provides it
                        if (d.executionTime && d.executionTime !== '') {
                            this.inputExecutionTimeList = d.executionTime.split(',').map((n: string) => ({ name: n.trim() }));
                        }
                    }
                }
            });
        }
    }

    initFlowDetails() {
        if (this.data.flowDetails && this.data.flowDetails.length > 0) {
            this.data.flowDetails.forEach((detail: any) => {
                const weekDaysArr = detail.weekDays ? detail.weekDays.split(',') : [];
                const weekdayControls = this.daysOfWeek.map(day => this.fb.control(weekDaysArr.includes(day)));

                this.detailOnDayLists.push(
                    detail.onDay && detail.onDay !== '' ? detail.onDay.split(',').map((n: string) => ({ name: n.trim() })) : []
                );
                this.detailExecutionTimeLists.push(
                    detail.executionTime && detail.executionTime !== '' ? detail.executionTime.split(',').map((n: string) => ({ name: n.trim() })) : []
                );

                const group = this.fb.group({
                    routeId: [detail.routeId],
                    status: [detail.status],
                    in_Out: [detail.in_Out],
                    frequencyType: [detail.frequencyType],
                    startDate: [detail.startDate ? new Date(detail.startDate) : ''],
                    endDate: [detail.endDate ? new Date(detail.endDate) : ''],
                    repeatCount: [detail.repeatCount || 0],
                    selectedWeekday: this.fb.array(weekdayControls),
                    onDay: [''],
                    executionTime: [''],
                });
                this.flowDetails.push(group);
            });
        }
    }

    addDetailFromForm() {
        const formVal = this.detailInputForm.value;

        // Basic validation - at least route should be selected
        if (!formVal.routeId) {
            this.toast.warning({ detail: "WARNING", summary: "Please select a Route Name", duration: 3000, position: 'topRight' });
            return;
        }

        // UPDATE MODE: If editing an existing detail, update it in-place
        if (this.editingDetailIndex !== null) {
            const existingGroup = this.flowDetails.at(this.editingDetailIndex) as FormGroup;
            existingGroup.patchValue({
                status: formVal.status || 'Active',
                in_Out: formVal.in_Out || '',
                frequencyType: formVal.frequencyType || '',
                startDate: formVal.startDate || '',
                endDate: formVal.endDate || '',
                repeatCount: formVal.repeatCount || 0,
            });

            // Update weekday checkboxes
            const weekDaysArr = formVal.selectedWeekday || [];
            const existingWeekdayArray = existingGroup.get('selectedWeekday') as FormArray;
            this.daysOfWeek.forEach((_: string, i: number) => {
                existingWeekdayArray.at(i).setValue(weekDaysArr[i] || false);
            });

            // Update chip lists for the detail row
            this.detailOnDayLists[this.editingDetailIndex] = [...this.inputOnDayList];
            this.detailExecutionTimeLists[this.editingDetailIndex] = [...this.inputExecutionTimeList];

            this.toast.success({ detail: "SUCCESS", summary: "Detail updated!", duration: 3000, position: 'topRight' });
            this.resetInputForm();
            return;
        }

        // ADD MODE: Check for duplicate route
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
        // Transfer autofilled chip lists to the detail row
        this.detailOnDayLists.push([...this.inputOnDayList]);
        this.detailExecutionTimeLists.push([...this.inputExecutionTimeList]);

        this.resetInputForm();
    }

    loadDetailIntoForm(index: number) {
        this.editingDetailIndex = index;
        const detail = this.flowDetails.at(index) as FormGroup;

        this.detailInputForm.patchValue({
            routeId: detail.get('routeId')?.value,
            status: detail.get('status')?.value,
            in_Out: detail.get('in_Out')?.value,
            frequencyType: detail.get('frequencyType')?.value,
            startDate: detail.get('startDate')?.value,
            endDate: detail.get('endDate')?.value,
            repeatCount: detail.get('repeatCount')?.value,
        }, { emitEvent: false });

        // Load weekday checkboxes
        const detailWeekdays = detail.get('selectedWeekday') as FormArray;
        const inputWeekdays = this.detailInputForm.get('selectedWeekday') as FormArray;
        this.daysOfWeek.forEach((_, i) => {
            inputWeekdays.at(i).setValue(detailWeekdays.at(i).value);
        });

        // Load chip lists
        this.inputOnDayList = [...this.detailOnDayLists[index]];
        this.inputExecutionTimeList = [...this.detailExecutionTimeLists[index]];
    }

    editDetail(index: number) {
        this.loadDetailIntoForm(index);
    }

    cancelEdit() {
        this.resetInputForm();
    }

    private resetInputForm() {
        this.editingDetailIndex = null;
        this.inputOnDayList = [];
        this.inputExecutionTimeList = [];
        this.detailInputForm.reset({
            routeId: null,
            status: 'Active',
            in_Out: '',
            frequencyType: '',
            startDate: '',
            endDate: '',
            repeatCount: 0,
        });
        // Reset weekday checkboxes
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

        // Re-render the table
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

    // Helper to resolve a route ID to its name for display in the table
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
            id: this.editFlowForm.get('id')?.value,
            customerID: this.editFlowForm.get('customerID')?.value,
            title: this.editFlowForm.get('title')?.value,
            description: this.editFlowForm.get('description')?.value,
            status: this.editFlowForm.get('status')?.value,
            flowDetails: details
        };

        this.flowsApi.updateFlow(flowModel).subscribe({
            next: (res) => {
                if (res.code === 100) {
                    this.toast.success({ detail: "SUCCESS", summary: res.description || res.message || 'Flow updated!', duration: 5000, position: 'topRight' });
                    this.dialogRef.close('updated');
                } else {
                    this.toast.error({ detail: "ERROR", summary: res.description || res.message || 'Failed to update flow', duration: 5000, position: 'topRight' });
                }
            },
            error: (err) => {
                this.toast.error({ detail: "ERROR", summary: err.error?.message || err.message || 'Server error', duration: 5000, position: 'topRight' });
            }
        });
    }
}
