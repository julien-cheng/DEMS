import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { ItemQueryType, GridViewModel, ExplorerService, IRequestBatchData } from '../../index';

@Component({
    selector: 'list-view',
    template: `
        <div class="list-view">
            <ul class="list-unstyled">
               <li *ngFor="let row of rows; let i=index" 
                    class="{{row.uiClass}}" [ngClass]="{'selected': row.selected, 'editMode': row.editMode}" 
                    [appFileSortable]="row" [dropTarget]="row.dropTarget"  [dropSource]="row.dropSource"  [gridView]="gridView"
                    (saveMove)="saveCallback({requestType: 'MoveIntoRequest', eventType: 'send'}, $event)" >
               <div class="pretty" *ngIf="hasRowsAllowedOperations"><input type="checkbox"
                        [checked]="row.selected" (change)="changeSelectRow.emit({selected: $event.target.checked, row: row,index: i})"  >
                        <label><i class="fa fa-check"></i></label></div>
                <ng-container *ngIf="!row.editMode">
                        <app-link [row]="row" (triggerCallback)="saveCallback($event)"></app-link>
                </ng-container>
                <app-inline-editor [row]="row" [(ngModel)]="newName" label="{{row.name}}" [editing]="row.editMode" 
                [inlineValidators]="row.rowInlineValidators" [required]="true" 
                (saveChange)="saveCallback({requestType: 'RenameRequest', eventType: 'send'}, [row])" type="text" ngDefaultControl></app-inline-editor>
                <div class="dropdown" *ngIf="!!row.allowedOperationsButtonDriven && row.allowedOperationsButtonDriven.length >0">
                        <button class="btn btn-link btn-sm" type="button"
                            data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"><i class="fa fa-ellipsis-h"></i></button>
                        <div app-operations-menu class="dropdown-menu" aria-labelledby="Allowed Operations"  [allowedOperations]="row.allowedOperationsButtonDriven"
                            [isButton]="false" [itemClass]="'dropdown-item'" (processBatchUiAction)="saveCallback($event,  [row])"></div>
                </div>
               </li>
            </ul>
        </div>
    `,
    styleUrls: ['./details-view.component.scss']
})

export class ListViewComponent implements OnInit {
    @Input() gridView: GridViewModel;
    @Output() processBatchUiAction = new EventEmitter();
    @Output() changeSelectRow = new EventEmitter();
    public hasRowsAllowedOperations: boolean;
    public rows: ItemQueryType[];
    public pathValidation: any = { 'pattern': '^[a-zA-Z0-9-_. ]+' };
    public fileValidation: any = { 'pattern': null };

    constructor(public explorerService: ExplorerService) {
    }

    ngOnInit() {
        this.rows = this.gridView.rows;
        this.hasRowsAllowedOperations = this.gridView.hasRowsAllowedOperations;
    }

    saveCallback(requestBatchData: IRequestBatchData, rows?: ItemQueryType[]) {
        (!!rows) && Object.assign(requestBatchData, { rows: rows });
        return this.processBatchUiAction.emit(requestBatchData);
    }
}
