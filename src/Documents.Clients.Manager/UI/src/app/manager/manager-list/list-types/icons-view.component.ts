import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { ItemQueryType, ExplorerService, IRequestBatchData, GridViewModel } from '../../index';

@Component({
  selector: 'icons-view',
  template: `
    <div class="list-view row">
      <div class="col-12 col-sm-6 col-md-3 mb-1" *ngFor="let row of rows; let i = index">
        <div
          class="icon-box {{row.uiClass}}"
          [ngClass]="{ selected: row.selected, editMode: row.editMode }"
          [appFileSortable]="row"
          [dropTarget]="row.dropTarget"
          [dropSource]="row.dropSource"
          [gridView]="gridView"
          (saveMove)="saveCallback({ requestType: 'MoveIntoRequest', eventType: 'send' }, $event)"
        >
          <div class="hover-toolbar">
            <div class="pretty float-left" *ngIf="hasRowsAllowedOperations">
              <input
                type="checkbox"
                (change)="
                  changeSelectRow.emit({
                    selected: $event.target.checked,
                    row: row,
                    index: i
                  })
                "
                [checked]="row.selected"
              />
              <label><i class="fa fa-check"></i></label>
            </div>
            <div class="dropdown float-right" *ngIf="!!row.allowedOperationsButtonDriven && row.allowedOperationsButtonDriven.length > 0">
              <button class="btn btn-link btn-sm" type="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                <i class="fa fa-ellipsis-h"></i>
              </button>
              <div
                class="dropdown-menu dropdown-menu-right"
                aria-labelledby="Allowed Operations"
                app-operations-menu
                [allowedOperations]="row.allowedOperationsButtonDriven"
                [isButton]="false"
                [itemClass]="'dropdown-item'"
                (processBatchUiAction)="saveCallback($event, [row])"
              ></div>
            </div>
          </div>
          <div class="icon-box-content">
            <p><app-link [row]="row" [imageLink]="true" (triggerCallback)="saveCallback($event)"></app-link></p>
            <ng-container *ngIf="!row.editMode">
              <app-link [row]="row" (triggerCallback)="saveCallback($event)" [hideIcon]="true"></app-link>
            </ng-container>
            <app-inline-editor
              [row]="row"
              [(ngModel)]="newName"
              label="{{row.name}}"
              [editing]="row.editMode"
              [required]="true"
              [inlineValidators]="row.rowInlineValidators"
              (saveChange)="saveCallback({ requestType: 'RenameRequest', eventType: 'send' }, [row])"
              type="text"
              ngDefaultControl
            ></app-inline-editor>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./details-view.component.scss']
})
export class IconsViewComponent implements OnInit {
  @Input() gridView: GridViewModel;
  rows: ItemQueryType[];
  @Output() processBatchUiAction = new EventEmitter();
  @Output() changeSelectRow = new EventEmitter();
  public hasRowsAllowedOperations: boolean;
  public pathValidation: any = { pattern: '^[a-zA-Z0-9-_. ]+' };
  public fileValidation: any = { pattern: null };
  constructor(public explorerService: ExplorerService) {}
  ngOnInit() {
    this.rows = this.gridView.rows;
    this.hasRowsAllowedOperations = this.gridView.hasRowsAllowedOperations;
  }

  saveCallback(requestBatchData: IRequestBatchData, rows?: ItemQueryType[]) {
    if (!!rows) {
      Object.assign(requestBatchData, { rows });
    }
    return this.processBatchUiAction.emit(requestBatchData);
  }
}
