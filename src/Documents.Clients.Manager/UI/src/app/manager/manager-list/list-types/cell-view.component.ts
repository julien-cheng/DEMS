import { Component, Input } from '@angular/core';
import { GridViewModel, IGridColumnSpecification, ItemQueryType, DateService } from '../../index';
import * as moment from 'moment';

@Component({
  selector: '[app-cell-view]',
  template: `
    <ng-container [ngSwitch]="col.keyName">
      <span [class]="col.keyName" *ngSwitchDefault>
        {{ dateService.isDate(row[col.keyName]) ? (row[col.keyName] | date: 'short') : row[col.keyName] }}
      </span>
      <a *ngSwitchCase="'email'" href="mailto:{{row.email}}"> {{ row.email }} </a>
    </ng-container>
  `,
  styleUrls: ['./details-view.component.scss']
})
export class CellViewComponent {
  @Input() row: ItemQueryType;
  @Input() col: IGridColumnSpecification;
  constructor(public dateService: DateService) {}
}
