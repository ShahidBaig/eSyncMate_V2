import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface RemapLine {
    lineNo: string;
    sellerSku: string;
    currentItemId: string;
    newItemId: string | null;
    result: string;
}

export interface RemapResult {
    orderNumber: string;
    updated: number;
    unchanged: number;
    notFound: number;
    ambiguous: number;
    lines: RemapLine[];
}

@Component({
    selector: 'remap-result-dialog',
    standalone: true,
    imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
    template: `
        <div class="remap-dialog">
            <!-- Header — same shape as the dashboard drilldown dialog -->
            <div class="remap-header">
                <div class="remap-header-left">
                    <div class="remap-icon">
                        <mat-icon>auto_fix_high</mat-icon>
                    </div>
                    <div class="remap-title-wrap">
                        <h2 class="remap-title">Re-Map Item IDs</h2>
                        <span class="remap-subtitle">
                            Order {{ data.orderNumber }}
                            <span class="remap-count">
                                <mat-icon>list_alt</mat-icon>{{ data.lines.length }} line(s)
                            </span>
                        </span>
                    </div>
                </div>
                <button mat-icon-button class="remap-close" (click)="dialogRef.close()">
                    <mat-icon>close</mat-icon>
                </button>
            </div>

            <div class="remap-body">
                <div class="summary">
                    <span class="chip chip-updated" *ngIf="data.updated > 0">{{ data.updated }} updated</span>
                    <span class="chip chip-same" *ngIf="data.unchanged > 0">{{ data.unchanged }} already correct</span>
                    <span class="chip chip-missing" *ngIf="data.notFound > 0">{{ data.notFound }} not found</span>
                    <span class="chip chip-ambiguous" *ngIf="data.ambiguous > 0">{{ data.ambiguous }} ambiguous</span>
                </div>

                <!-- The point users need to be sure of: an unmatched line was not blanked out. -->
                <p class="kept-note" *ngIf="data.notFound > 0 || data.ambiguous > 0">
                    <mat-icon>lock</mat-icon>
                    <span>
                        {{ data.notFound + data.ambiguous }} line(s) could not be matched and
                        <strong>kept their existing Item ID</strong>. Nothing was cleared.
                    </span>
                </p>

                <div class="table-scroll">
                    <table class="lines">
                        <thead>
                            <tr>
                                <th>Line</th>
                                <th>Seller SKU</th>
                                <th>Item ID</th>
                                <th>Result</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr *ngFor="let line of data.lines; trackBy: trackByLine">
                                <td>{{ line.lineNo }}</td>
                                <td class="sku">{{ line.sellerSku || '—' }}</td>
                                <td class="item-id">
                                    <ng-container *ngIf="isUpdated(line); else keptValue">
                                        <span class="old">{{ line.currentItemId || '—' }}</span>
                                        <mat-icon class="arrow">arrow_forward</mat-icon>
                                        <span class="new">{{ line.newItemId }}</span>
                                    </ng-container>
                                    <ng-template #keptValue>
                                        <span class="kept">{{ line.currentItemId || '—' }}</span>
                                        <span class="kept-tag" *ngIf="!isCorrect(line)">unchanged</span>
                                    </ng-template>
                                </td>
                                <td>
                                    <span class="badge" [ngClass]="badgeClass(line)">{{ friendly(line) }}</span>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <p class="next-step" *ngIf="data.updated > 0">
                    <mat-icon>info</mat-icon>
                    <span>Re-Process the order to send it to the ERP.</span>
                </p>
            </div>

            <div class="remap-actions">
                <button mat-raised-button class="close-btn" (click)="dialogRef.close()">Close</button>
            </div>
        </div>
    `,
    styles: [`
        .remap-dialog {
            font-family: "Poppins", "Roboto", sans-serif;
            max-width: 820px;
        }

        /* ---- Header: matches .drilldown-header in the dashboard dialog ---- */
        .remap-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 20px 24px 16px;
            background: linear-gradient(180deg, #ffffff 0%, #f1f5f9 100%);
            border-bottom: 1px solid #e2e8f0;
            box-shadow: 0 1px 0 rgba(255, 255, 255, 0.9) inset, 0 4px 12px -6px rgba(15, 23, 42, 0.18);
        }
        .remap-header-left { display: flex; align-items: center; gap: 14px; }
        .remap-icon {
            width: 46px; height: 46px; border-radius: 14px;
            display: flex; align-items: center; justify-content: center;
            position: relative;
            background: #E8834A;
            box-shadow: 0 6px 14px -4px rgba(15, 23, 42, 0.45),
                        0 2px 4px rgba(15, 23, 42, 0.2),
                        0 1px 0 rgba(255, 255, 255, 0.35) inset;

            &::after {
                content: '';
                position: absolute;
                inset: 0;
                border-radius: 14px;
                background: linear-gradient(160deg, rgba(255, 255, 255, 0.38) 0%, rgba(255, 255, 255, 0) 55%);
                pointer-events: none;
            }

            mat-icon { font-size: 22px; width: 22px; height: 22px; color: #fff; position: relative; z-index: 1; }
        }
        .remap-title-wrap { display: flex; flex-direction: column; }
        .remap-title {
            font-size: 18px; font-weight: 700; color: #1e293b; margin: 0;
            text-shadow: 0 1px 0 rgba(255, 255, 255, 0.8);
        }
        .remap-subtitle {
            font-size: 12px; color: #94a3b8; font-weight: 500;
            display: inline-flex; align-items: center; gap: 12px; flex-wrap: wrap;
        }
        .remap-count {
            display: inline-flex; align-items: center; gap: 4px;
            mat-icon { font-size: 14px; width: 14px; height: 14px; }
        }
        .remap-close {
            color: #94a3b8;
            transition: all 0.15s ease;
            &:hover { color: #475569; background: #ffffff; box-shadow: 0 2px 6px rgba(15, 23, 42, 0.15); }
        }

        .remap-body { padding: 16px 24px 0; }

        /* ---- Summary chips ---- */
        .summary { display: flex; flex-wrap: wrap; gap: 8px; }
        .chip {
            display: inline-block; padding: 4px 12px; border-radius: 12px;
            font-size: 12px; font-weight: 600; border: 1px solid;
        }
        .chip-updated   { background: #ecfdf5; color: #047857; border-color: #a7f3d0; }
        .chip-same      { background: #f1f5f9; color: #475569; border-color: #e2e8f0; }
        .chip-missing   { background: #fff7ed; color: #c2410c; border-color: #fed7aa; }
        .chip-ambiguous { background: #fefce8; color: #a16207; border-color: #fef08a; }

        .kept-note {
            display: flex; align-items: flex-start; gap: 8px;
            margin: 14px 0 0; padding: 10px 12px;
            background: #fff7ed; border: 1px solid #fed7aa; border-radius: 6px;
            color: #9a3412; font-size: 13px; line-height: 1.45;

            mat-icon { font-size: 18px; width: 18px; height: 18px; margin-top: 1px; }
        }

        /* ---- Lines table ---- */
        .table-scroll {
            margin-top: 16px; max-height: 340px; overflow: auto;
            border: 1px solid #e2e8f0; border-radius: 6px;
        }
        table.lines { width: 100%; border-collapse: collapse; font-size: 13px; }
        table.lines th {
            position: sticky; top: 0; z-index: 1;
            background: #f8fafc; text-align: left; padding: 10px 12px;
            font-weight: 600; color: #475569; border-bottom: 1px solid #e2e8f0; white-space: nowrap;
        }
        table.lines td { padding: 10px 12px; border-bottom: 1px solid #f1f5f9; vertical-align: middle; color: #334155; }
        table.lines tr:last-child td { border-bottom: none; }
        .sku { font-family: 'Courier New', monospace; word-break: break-all; }
        .item-id { white-space: nowrap; }
        .item-id .old { color: #94a3b8; text-decoration: line-through; }
        .item-id .new { color: #047857; font-weight: 600; }
        .item-id .kept { color: #334155; font-weight: 600; }
        .item-id .kept-tag {
            margin-left: 8px; font-size: 11px; color: #64748b;
            background: #f1f5f9; border-radius: 8px; padding: 2px 8px;
        }
        .arrow { font-size: 15px; width: 15px; height: 15px; vertical-align: middle; margin: 0 6px; color: #94a3b8; }

        .badge {
            display: inline-block; padding: 3px 10px; border-radius: 10px;
            font-size: 11px; font-weight: 600; border: 1px solid; white-space: nowrap;
        }
        .badge-updated   { background: #ecfdf5; color: #047857; border-color: #a7f3d0; }
        .badge-correct   { background: #f1f5f9; color: #475569; border-color: #e2e8f0; }
        .badge-missing   { background: #fff7ed; color: #c2410c; border-color: #fed7aa; }
        .badge-ambiguous { background: #fefce8; color: #a16207; border-color: #fef08a; }

        .next-step {
            display: flex; align-items: center; gap: 6px;
            margin: 14px 0 0; font-size: 13px; color: #64748b;

            mat-icon { font-size: 16px; width: 16px; height: 16px; color: #94a3b8; }
        }

        .remap-actions { display: flex; justify-content: flex-end; padding: 16px 24px 20px; }
        .close-btn {
            background: #E8834A !important; color: #fff !important; min-width: 100px;
            &:hover { background: #d2703a !important; }
        }
    `]
})
export class RemapResultDialogComponent {
    constructor(
        public dialogRef: MatDialogRef<RemapResultDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: RemapResult
    ) {}

    trackByLine = (_: number, line: RemapLine) => `${line.lineNo}|${line.sellerSku}`;

    isUpdated(line: RemapLine): boolean { return (line.result || '') === 'UPDATED'; }
    isCorrect(line: RemapLine): boolean { return (line.result || '') === 'ALREADY CORRECT'; }

    badgeClass(line: RemapLine): string {
        const result = line.result || '';
        if (result === 'UPDATED') return 'badge-updated';
        if (result === 'ALREADY CORRECT') return 'badge-correct';
        if (result.indexOf('AMBIGUOUS') === 0) return 'badge-ambiguous';
        return 'badge-missing';
    }

    // The endpoint's verdicts are terse by design; spell them out for the grid user.
    friendly(line: RemapLine): string {
        const result = line.result || '';
        if (result === 'UPDATED') return 'Updated';
        if (result === 'ALREADY CORRECT') return 'Already correct';
        if (result === 'NOT IN INVENTORY FEED') return 'SKU not in inventory feed';
        if (result === 'NO SELLER SKU ON LINE') return 'No Seller SKU on this line';
        if (result.indexOf('AMBIGUOUS') === 0) return 'Several Item IDs match this SKU';
        return result;
    }
}
