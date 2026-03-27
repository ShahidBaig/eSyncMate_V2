import { Component, Inject, OnInit, ViewChild, ElementRef } from '@angular/core';
import { DatePipe, NgIf, CommonModule } from '@angular/common';
import { MatTable, MatTableModule } from '@angular/material/table';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule } from '@ngx-translate/core';
import { RoutesService } from '../../services/routes.service';
import html2canvas from 'html2canvas';
import { jsPDF } from 'jspdf';

interface RouteInfo {
    id: number;
    name: string;
    sourceParty: string;
    destinationParty: string;
    sourceConnector: string;
    destinationConnector: string;
    routeType: string;
    customerName: string;
}

interface DiagramStep {
    stepNum: number;
    shortName: string;
    fullName: string;
    from: string;
    to: string;
    fromKey: string;
    toKey: string;
    fromIcon: string;
    toIcon: string;
    fromClass: string;
    toClass: string;
    description: string;
    frequency: string;
    status: string;
    nextRecurrence: string;
}

@Component({
    selector: 'view-flow-dialog',
    templateUrl: './view-flow-dialog.component.html',
    styleUrls: ['./view-flow-dialog.component.scss'],
    standalone: true,
    providers: [DatePipe],
    imports: [
        MatTableModule,
        DatePipe,
        ReactiveFormsModule,
        NgIf,
        MatButtonModule,
        MatIconModule,
        CommonModule,
        MatTabsModule,
        TranslateModule,
        MatDialogModule,
        MatTooltipModule
    ],
})
export class ViewFlowDialogComponent implements OnInit {
    viewFlowForm: FormGroup;
    daysOfWeek: string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    allRouteOptions: RouteInfo[] = [];
    diagramSteps: DiagramStep[] = [];
    showFullscreen = false;

    detailOnDayLists: { name: string }[][] = [];
    detailExecutionTimeLists: { name: string }[][] = [];

    @ViewChild('detailsTable') detailsTable!: MatTable<any>;
    @ViewChild('diagramContent') diagramContent!: ElementRef;
    @ViewChild('diagramTabContent') diagramTabContent!: ElementRef;

    constructor(
        public dialogRef: MatDialogRef<ViewFlowDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: any,
        private fb: FormBuilder,
        private datePipe: DatePipe,
        private routeApi: RoutesService,
    ) {
        this.viewFlowForm = this.fb.group({
            customerID: [{ value: data.customerID, disabled: true }],
            title: [{ value: data.title, disabled: true }],
            description: [{ value: data.description, disabled: true }],
            status: [{ value: data.status, disabled: true }],
            flowDetails: this.fb.array([])
        });
    }

    get flowDetails(): FormArray {
        return this.viewFlowForm.get('flowDetails') as FormArray;
    }

    ngOnInit() {
        this.initFlowDetails();
        this.getRoutes();
    }

    getRoutes() {
        this.routeApi.getRouteName().subscribe({
            next: (res: any) => {
                const raw: any[] = res.routes ?? res;
                this.allRouteOptions = raw.map((r: any) => ({
                    id: r.id,
                    name: r.name,
                    sourceParty: r.sourceParty || '',
                    destinationParty: r.destinationParty || '',
                    sourceConnector: r.sourceConnector || '',
                    destinationConnector: r.destinationConnector || '',
                    routeType: r.routeType || '',
                    customerName: r.customerName || '',
                }));
                // Update routeName in flow details
                this.flowDetails.controls.forEach((ctrl) => {
                    const group = ctrl as FormGroup;
                    const routeId = group.get('routeId')?.value;
                    if (routeId) {
                        const route = this.allRouteOptions.find(r => r.id === routeId);
                        if (route) {
                            group.get('routeName')?.setValue(route.name);
                        }
                    }
                });
                // Build diagram after routes loaded
                this.buildDiagramSteps();
            },
        });
    }

    initFlowDetails() {
        if (this.data.flowDetails && this.data.flowDetails.length > 0) {
            this.data.flowDetails.forEach((detail: any) => {
                const weekDaysArr = detail.weekDays ? detail.weekDays.split(',').map((s: string) => s.trim()) : [];
                const weekdayControls = this.daysOfWeek.map(day => this.fb.control({ value: weekDaysArr.includes(day), disabled: true }));

                this.detailOnDayLists.push(
                    detail.onDay && detail.onDay !== '' ? detail.onDay.split(',').map((n: string) => ({ name: n.trim() })) : []
                );
                this.detailExecutionTimeLists.push(
                    detail.executionTime && detail.executionTime !== '' ? detail.executionTime.split(',').map((n: string) => ({ name: n.trim() })) : []
                );

                const group = this.fb.group({
                    routeId: [{ value: detail.routeId, disabled: true }],
                    routeName: [{ value: detail.routeName || `Route #${detail.routeId}`, disabled: true }],
                    status: [{ value: detail.status, disabled: true }],
                    frequencyType: [{ value: detail.frequencyType, disabled: true }],
                    startDate: [{ value: detail.startDate ? new Date(detail.startDate) : '', disabled: true }],
                    endDate: [{ value: detail.endDate ? new Date(detail.endDate) : '', disabled: true }],
                    repeatCount: [{ value: detail.repeatCount || 0, disabled: true }],
                    selectedWeekday: this.fb.array(weekdayControls),
                });
                this.flowDetails.push(group);
            });
        }
    }

    buildDiagramSteps() {
        this.diagramSteps = [];
        const partnerName = this.getPartnerDisplayName();

        this.flowDetails.controls.forEach((ctrl, i) => {
            const group = ctrl as FormGroup;
            const routeId = group.get('routeId')?.value;
            const routeInfo = this.allRouteOptions.find(r => r.id === routeId);
            const routeName = group.get('routeName')?.value || '';
            const status = group.get('status')?.value || 'In-Active';
            const frequency = this.getFrequencyDisplay(i);
            const shortName = this.getShortRouteName(routeName);

            // Determine correct flow direction — use route name keywords first, fallback to API source/dest
            const direction = this.getFlowDirection(shortName, partnerName, routeInfo);

            const fromLabel = direction.from;
            const toLabel = direction.to;
            const fromKey = direction.fromKey;
            const toKey = direction.toKey;

            const description = this.buildStepDescription(shortName, fromLabel, toLabel, frequency);

            this.diagramSteps.push({
                stepNum: i + 1,
                shortName: shortName,
                fullName: routeName,
                from: fromLabel,
                to: toLabel,
                fromKey: fromKey,
                toKey: toKey,
                fromIcon: this.getPartyIcon(fromKey),
                toIcon: this.getPartyIcon(toKey),
                fromClass: this.getPartyClass(fromKey),
                toClass: this.getPartyClass(toKey),
                description: description,
                frequency: frequency,
                status: status,
                nextRecurrence: this.getNextRecurrence(i)
            });
        });
    }

    getFlowDirection(shortName: string, partnerName: string, routeInfo?: RouteInfo): { from: string; to: string; fromKey: string; toKey: string } {
        const name = shortName.toLowerCase();

        // --- Keyword-based matching for known route patterns ---

        // ERP (SPARS) → eSyncMate: Fetch/download data from ERP
        if (name.includes('fetch full inventory') || name.includes('fetch differential inventory') || name.includes('fetch diff inventory')) {
            return { from: 'ERP (SPARS)', to: 'eSyncMate', fromKey: 'spars', toKey: 'esyncmate' };
        }
        if (name.includes('fetch asn') || name.includes('fetch cancel')) {
            return { from: 'ERP (SPARS)', to: 'eSyncMate', fromKey: 'spars', toKey: 'esyncmate' };
        }

        // eSyncMate → Trading Partner: Upload/send data to partner
        if (name.includes('upload inventory') || name.includes('upload warehouse')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }
        if (name.includes('upload bulk item prices') || name.includes('upload prices') || name.includes('update item price')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }
        if (name.includes('send asn') || name.includes('shipment notification')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }
        if (name.includes('send cancel') || name.includes('cancellation to')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }
        if (name.includes('create product') || name.includes('delete product')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }
        if (name.includes('upload promo') || name.includes('revert promo')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }

        // Trading Partner → eSyncMate: Get orders from partner
        if (name.includes('get order')) {
            return { from: partnerName, to: 'eSyncMate', fromKey: 'partner', toKey: 'esyncmate' };
        }

        // eSyncMate → Trading Partner: Request/download product data from partner
        if (name.includes('request item type') || name.includes('download item type') || name.includes('download item')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }
        if (name.includes('get item type attribute')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }
        if (name.includes('download items data')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }

        // eSyncMate → ERP (SPARS): Place order in ERP
        if (name.includes('place order')) {
            return { from: 'eSyncMate', to: 'ERP (SPARS)', fromKey: 'esyncmate', toKey: 'spars' };
        }

        // Check status: eSyncMate → Trading Partner
        if (name.includes('check') && name.includes('status')) {
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }
        // --- Generic keyword fallback for new/unknown routes ---
        // Inbound keywords: data coming INTO eSyncMate
        if (name.includes('fetch') || name.includes('download') || name.includes('get') || name.includes('pull') || name.includes('receive') || name.includes('import')) {
            // Check if "from erp" in name
            if (name.includes('erp') || name.includes('spars')) {
                return { from: 'ERP (SPARS)', to: 'eSyncMate', fromKey: 'spars', toKey: 'esyncmate' };
            }
            return { from: partnerName, to: 'eSyncMate', fromKey: 'partner', toKey: 'esyncmate' };
        }
        // Outbound keywords: data going OUT of eSyncMate
        if (name.includes('upload') || name.includes('send') || name.includes('push') || name.includes('create') || name.includes('submit') || name.includes('export') || name.includes('notify')) {
            if (name.includes('erp') || name.includes('spars')) {
                return { from: 'eSyncMate', to: 'ERP (SPARS)', fromKey: 'esyncmate', toKey: 'spars' };
            }
            return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
        }
        // Place/sync to ERP
        if (name.includes('place') || name.includes('sync to erp')) {
            return { from: 'eSyncMate', to: 'ERP (SPARS)', fromKey: 'esyncmate', toKey: 'spars' };
        }

        // --- API fallback: use sourceParty/destinationParty from Routes table ---
        if (routeInfo && (routeInfo.sourceParty || routeInfo.destinationParty)) {
            const fromRaw = routeInfo.sourceParty || 'esyncmate';
            const toRaw = routeInfo.destinationParty || 'esyncmate';
            return {
                from: this.getPartyLabel(fromRaw),
                to: this.getPartyLabel(toRaw),
                fromKey: this.resolvePartyKey(fromRaw),
                toKey: this.resolvePartyKey(toRaw)
            };
        }

        // Ultimate default
        return { from: 'eSyncMate', to: partnerName, fromKey: 'esyncmate', toKey: 'partner' };
    }

    private resolvePartyKey(party: string): string {
        if (!party) return 'esyncmate';
        const p = party.toLowerCase();
        if (p === 'spars') return 'spars';
        if (p === 'esyncmate') return 'esyncmate';
        return 'partner';
    }

    getPartyLabel(party: string): string {
        if (!party) return 'Unknown';
        const p = party.toLowerCase();
        if (p === 'spars') return 'ERP (SPARS)';
        if (p === 'esyncmate') return 'eSyncMate';
        // Trading partners - use friendly name
        const partnerMap: { [key: string]: string } = {
            'ama1005': 'Amazon', 'kno8068': 'Knot', 'low2221mp': 'Lowes',
            'mac0149m': 'Macys', 'mic1300mp': 'Michaels',
            'tar6266p': 'Target', 'tar6266pah': 'Target SEI', 'wal4001mp': 'Walmart'
        };
        return partnerMap[p] || party;
    }

    getPartyIcon(party: string): string {
        if (!party) return 'help';
        const p = party.toLowerCase();
        if (p === 'spars') return 'dns';
        if (p === 'esyncmate') return 'hub';
        return 'store';
    }

    getPartyClass(party: string): string {
        if (!party) return 'party-unknown';
        const p = party.toLowerCase();
        if (p === 'spars') return 'party-erp';
        if (p === 'esyncmate') return 'party-esyncmate';
        return 'party-partner';
    }

    buildStepDescription(shortName: string, from: string, to: string, frequency: string): string {
        const name = shortName.toLowerCase();

        // Inventory
        if (name.includes('fetch full inventory'))
            return `Downloads complete inventory snapshot from ${from} and stores it in ${to} for processing`;
        if (name.includes('fetch differential inventory'))
            return `Downloads only changed/updated inventory items from ${from} since last sync`;
        if (name.includes('upload warehouse'))
            return `Pushes warehouse-wise stock levels from ${from} to ${to} marketplace`;
        if (name.includes('upload inventory'))
            return `Pushes current stock levels from ${from} to ${to} marketplace`;
        if (name.includes('check inventory feed status'))
            return `Checks if the uploaded inventory feed was processed successfully on ${to}`;

        // Pricing
        if (name.includes('upload bulk item prices'))
            return `Sends bulk product pricing updates from ${from} to ${to} marketplace`;
        if (name.includes('update item price'))
            return `Updates individual item prices on ${to}`;
        if (name.includes('upload promo'))
            return `Uploads promotional/sale prices to ${to}`;
        if (name.includes('revert promo'))
            return `Reverts promotional prices back to regular pricing on ${to}`;

        // Orders
        if (name.includes('get order'))
            return `Pulls new customer orders from ${from} marketplace into ${to}`;
        if (name.includes('place order'))
            return `Creates the received orders in ${to} ERP for warehouse fulfillment`;

        // ASN / Shipment
        if (name.includes('fetch asn'))
            return `Retrieves shipment/tracking data from ${from} ERP for shipped orders`;
        if (name.includes('send asn') || name.includes('shipment notification'))
            return `Sends shipping confirmation and tracking info to ${to} marketplace`;

        // Cancellations
        if (name.includes('fetch cancel'))
            return `Retrieves order cancellation data from ${from} ERP`;
        if (name.includes('send cancel') || name.includes('cancellation to'))
            return `Sends cancellation updates to ${to} marketplace`;

        // Product Catalog
        if (name.includes('request item type'))
            return `eSyncMate requests item type/category report from ${to}`;
        if (name.includes('download item type'))
            return `eSyncMate downloads item type definitions from ${to}`;
        if (name.includes('get item type attribute'))
            return `eSyncMate retrieves product attribute specifications from ${to}`;
        if (name.includes('download items data'))
            return `eSyncMate downloads approved product data from ${to}`;
        if (name.includes('create product'))
            return `eSyncMate lists new products on ${to} marketplace`;
        if (name.includes('check product approval') || name.includes('approval status'))
            return `eSyncMate checks if submitted products are approved on ${to}`;
        if (name.includes('delete product'))
            return `eSyncMate removes obsolete products from ${to} catalog`;

        return `Transfers data from ${from} to ${to}`;
    }

    getShortRouteName(routeName: string): string {
        if (!routeName) return '';
        const dashIndex = routeName.indexOf(' - ');
        return dashIndex > -1 ? routeName.substring(dashIndex + 3) : routeName;
    }

    getPartnerDisplayName(): string {
        const customerId = this.data.customerID || '';
        const partnerMap: { [key: string]: string } = {
            'AMA1005': 'Amazon', 'KNO8068': 'Knot', 'LOW2221MP': 'Lowes',
            'MAC0149M': 'Macys', 'MIC1300MP': 'Michaels',
            'TAR6266P': 'Target', 'TAR6266PAH': 'Target SEI', 'WAL4001MP': 'Walmart'
        };
        return partnerMap[customerId] || customerId;
    }

    getFrequencyDisplay(index: number): string {
        const detail = this.flowDetails.at(index) as FormGroup;
        const freqType = detail.get('frequencyType')?.value || '';
        const repeatCount = detail.get('repeatCount')?.value || 0;
        const weekDaysArr = detail.get('selectedWeekday') as FormArray;
        const onDayList = this.detailOnDayLists[index] || [];
        const execTimeList = this.detailExecutionTimeLists[index] || [];

        switch (freqType) {
            case 'Minutely': return `Every ${repeatCount} min${repeatCount > 1 ? 's' : ''}`;
            case 'Hourly': return `Every ${repeatCount} hr${repeatCount > 1 ? 's' : ''}`;
            case 'Daily': {
                if (execTimeList.length > 0) return `Daily at ${execTimeList.map(t => t.name).join(', ')}`;
                return 'Daily';
            }
            case 'Weekly': {
                const days = this.daysOfWeek.filter((_, i) => weekDaysArr?.at(i)?.value).map(d => d.substring(0, 3));
                let display = `Weekly on ${days.join(', ')}`;
                if (execTimeList.length > 0) display += ` at ${execTimeList.map(t => t.name).join(', ')}`;
                return display;
            }
            case 'Monthly': {
                let display = onDayList.length > 0 ? `Monthly on day${onDayList.length > 1 ? 's' : ''} ${onDayList.map(d => d.name).join(', ')}` : 'Monthly';
                if (execTimeList.length > 0) display += ` at ${execTimeList.map(t => t.name).join(', ')}`;
                return display;
            }
            default: return freqType || 'Not configured';
        }
    }

    getNextRecurrence(index: number): string {
        const detail = this.flowDetails.at(index) as FormGroup;
        if (detail.get('status')?.value !== 'Active') return 'Inactive';

        const freqType = detail.get('frequencyType')?.value || '';
        const repeatCount = detail.get('repeatCount')?.value || 0;
        const weekDaysArr = detail.get('selectedWeekday') as FormArray;
        const onDayList = this.detailOnDayLists[index] || [];
        const execTimeList = this.detailExecutionTimeLists[index] || [];
        const now = new Date();
        let nextDate: Date | null = null;

        try {
            switch (freqType) {
                case 'Minutely': {
                    if (repeatCount <= 0) break;
                    const next = new Date(now);
                    const nextMin = Math.ceil((next.getMinutes() + 1) / repeatCount) * repeatCount;
                    next.setMinutes(nextMin, 0, 0);
                    if (next <= now) next.setMinutes(next.getMinutes() + repeatCount);
                    nextDate = next;
                    break;
                }
                case 'Hourly': {
                    if (repeatCount <= 0) break;
                    const next = new Date(now);
                    const nextHr = Math.ceil((next.getHours() + 1) / repeatCount) * repeatCount;
                    next.setHours(nextHr, 0, 0, 0);
                    if (next <= now) next.setHours(next.getHours() + repeatCount);
                    nextDate = next;
                    break;
                }
                case 'Daily': {
                    if (execTimeList.length > 0) {
                        for (let d = 0; d <= 1; d++) {
                            const date = new Date(now.getFullYear(), now.getMonth(), now.getDate() + d);
                            const result = this.findNextExecTimeForDate(date, now, execTimeList);
                            if (result) { nextDate = result; break; }
                        }
                    } else {
                        const next = new Date(now);
                        next.setDate(next.getDate() + 1);
                        next.setHours(0, 0, 0, 0);
                        nextDate = next;
                    }
                    break;
                }
                case 'Weekly': {
                    const activeDays: number[] = [];
                    const dayMap = [1, 2, 3, 4, 5, 6, 0];
                    this.daysOfWeek.forEach((_, i) => { if (weekDaysArr?.at(i)?.value) activeDays.push(dayMap[i]); });
                    if (activeDays.length === 0) break;
                    for (let d = 0; d <= 7; d++) {
                        const candidate = new Date(now); candidate.setDate(candidate.getDate() + d);
                        if (activeDays.includes(candidate.getDay())) {
                            if (execTimeList.length > 0) {
                                const result = this.findNextExecTimeForDate(candidate, now, execTimeList);
                                if (result) { nextDate = result; break; }
                            } else if (d > 0) {
                                candidate.setHours(0, 0, 0, 0); nextDate = candidate; break;
                            }
                        }
                    }
                    break;
                }
                case 'Monthly': {
                    if (onDayList.length === 0) break;
                    const days = onDayList.map(d => parseInt(d.name, 10)).filter(d => !isNaN(d)).sort((a, b) => a - b);
                    for (let m = 0; m <= 1; m++) {
                        const monthDate = new Date(now.getFullYear(), now.getMonth() + m, 1);
                        for (const day of days) {
                            const candidate = new Date(monthDate.getFullYear(), monthDate.getMonth(), day);
                            if (candidate.getMonth() !== monthDate.getMonth()) continue;
                            if (execTimeList.length > 0) {
                                const result = this.findNextExecTimeForDate(candidate, now, execTimeList);
                                if (result) { nextDate = result; break; }
                            } else { candidate.setHours(0, 0, 0, 0); if (candidate > now) { nextDate = candidate; break; } }
                        }
                        if (nextDate) break;
                    }
                    break;
                }
            }
        } catch (e) { return 'N/A'; }

        if (!nextDate) return 'N/A';
        return this.datePipe.transform(nextDate, 'MMM dd, yyyy hh:mm a') || 'N/A';
    }

    private findNextExecTimeForDate(date: Date, now: Date, execTimeList: { name: string }[]): Date | null {
        const times = execTimeList
            .map(t => { const p = t.name.split(':'); return p.length === 2 ? { h: parseInt(p[0], 10), m: parseInt(p[1], 10) } : null; })
            .filter(t => t !== null)
            .sort((a, b) => a!.h * 60 + a!.m - (b!.h * 60 + b!.m));
        for (const time of times) {
            const candidate = new Date(date.getFullYear(), date.getMonth(), date.getDate(), time!.h, time!.m, 0, 0);
            if (candidate > now) return candidate;
        }
        return null;
    }

    isValidDate(value: any): boolean {
        if (!value) return false;
        const date = new Date(value);
        return !isNaN(date.getTime()) && date.getFullYear() > 1900;
    }

    downloadDiagramFromTab(): void {
        const element = this.diagramTabContent?.nativeElement as HTMLElement;
        if (!element) return;
        this.captureAndDownload(element);
    }

    downloadDiagram(): void {
        const element = this.diagramContent?.nativeElement as HTMLElement;
        if (!element) return;
        this.captureAndDownload(element);
    }

    private captureAndDownload(element: HTMLElement): void {

        const partnerName = this.getPartnerDisplayName();
        const fileName = `${partnerName}_${this.data.title}_Flow_Diagram`;

        html2canvas(element, {
            scale: 2,
            backgroundColor: '#f9fafb',
            useCORS: true,
            logging: false,
        }).then(canvas => {
            const imgWidth = canvas.width;
            const imgHeight = canvas.height;

            // Landscape if wider than tall, portrait otherwise
            const orientation = imgWidth > imgHeight ? 'landscape' : 'portrait';
            const pdf = new jsPDF(orientation as any, 'px', [imgWidth + 60, imgHeight + 100]);

            // Header
            pdf.setFillColor(63, 81, 181);
            pdf.rect(0, 0, imgWidth + 60, 50, 'F');
            pdf.setTextColor(255, 255, 255);
            pdf.setFontSize(18);
            pdf.setFont('helvetica', 'bold');
            pdf.text(`${partnerName} — ${this.data.title} — Flow Diagram`, 20, 32);

            // Diagram image
            const imgData = canvas.toDataURL('image/png');
            pdf.addImage(imgData, 'PNG', 30, 65, imgWidth, imgHeight);

            pdf.save(`${fileName}.pdf`);
        });
    }

    onClose(): void {
        this.dialogRef.close();
    }
}
