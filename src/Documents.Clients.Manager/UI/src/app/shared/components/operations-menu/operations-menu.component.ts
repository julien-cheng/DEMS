import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { IAllowedOperation, IBatchOperation, batchOperationsDefaults, IRequestBatchData, ItemQueryType, EventType } from '../../index';
import { BatchOperationService } from '../../services/batch-operation.service';

@Component({
  selector: '[app-operations-menu]',
  template: `
    <ng-container *ngFor="let operation of allowedOperations; let i = index">
      <button
        *ngIf="isButton && batchOperationService.displayButtonByOperation(operation)"
        [class]="itemClass"
        [title]="operation.displayName"
        (click)="saveCallback(operation.batchOperation, 'initialize')"
      >
        <i class="icon fa {{ operation.icons | iconClass: this.iconDefaults[operation.batchOperation.type] }}"></i>
        {{ showText ? (batchOperationService.modalLoading ? 'Loading...' : operation.displayName) : '' }}
      </button>
      <a
        *ngIf="!isButton && batchOperationService.displayButtonByOperation(operation)"
        [class]="itemClass"
        [ngClass]="{ 'd-none': operation?.isDisabled }"
        (click)="saveCallback(operation.batchOperation, 'initialize')"
      >
        <i class="icon fa {{ operation.icons | iconClass: this.iconDefaults[operation.batchOperation.type] }}"></i>
        {{ showText ? (batchOperationService.modalLoading ? 'Loading...' : operation.displayName) : '' }}
      </a>
    </ng-container>
  `,
  styleUrls: ['./operations-menu.component.scss']
})
export class OperationsMenuComponent implements OnInit {
  @Input() allowedOperations: IAllowedOperation[];
  @Input() isButton = true; // Default to buttons
  @Input() itemClass: string;
  @Input() showText = true;
  @Output() processBatchUiAction = new EventEmitter();
  public iconDefaults: any = batchOperationsDefaults.icons;

  constructor(public batchOperationService: BatchOperationService) {}

  ngOnInit() {
    // console.log(this.allowedOperations);
  }

  // saveCallback(batchOperation: IBatchOperation, eventType: EventType) { BatchOperation
  saveCallback(batchOperation: IBatchOperation, eventType: EventType) {
    const request: IRequestBatchData = {
      batchOperations: [batchOperation],
      requestType: batchOperation.type,
      eventType
    };
    return this.processBatchUiAction.emit(request);
  }

  // Description: returns true or false for operation types that should or should not have buttons
  // showOperationButton(operation: IAllowedOperation): boolean {
  //   return operation.batchOperation.type !== 'UploadRequest';
  // }
}
