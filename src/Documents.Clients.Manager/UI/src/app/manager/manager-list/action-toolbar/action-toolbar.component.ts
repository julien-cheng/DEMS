import { Component, SimpleChanges, Input, Output, EventEmitter, Inject, ViewChild, ElementRef } from '@angular/core';
import { FormGroup, FormControl, Validators, ValidatorFn } from '@angular/forms';
import { JQ_TOKEN, ListService, AuthService, ExplorerService, PathService, IManager, ViewMode } from '../../index';
import { IPathIdentifier, IAllowedOperation, IPath, IBatchResponse, IGridView, batchOperationsDefaults, IRequestBatchData } from '../../index';


@Component({
  selector: 'app-action-toolbar',
  templateUrl: './action-toolbar.component.html',
  styleUrls: ['./action-toolbar.component.scss']
})
export class ActionToolbarComponent {
  @Input() pathIdentifier: IPathIdentifier;
  @Input() manager: IManager;
  @Output() updateView = new EventEmitter();
  @Output() toggleAll = new EventEmitter();
  @Output() processBatchUiAction = new EventEmitter();
  public pathAllowedOperations: IAllowedOperation[];
  public icons: any = batchOperationsDefaults.icons;
  public eViewMode = ViewMode; // Description: Double-bind and Emit the change of viewmode
  public isSearchAOEnabled: boolean = false;
  @Output() viewModePropChange = new EventEmitter();
  viewMode: ViewMode;
  @Input()
  get viewModeProp() {
    return this.viewMode;
  }
  set viewModeProp(value) {
    this.viewMode = value;
    this.explorerService.fileExplorer.listviewMode = this.viewMode;
    this.viewModePropChange.emit(this.viewMode);
  }

  constructor(
    public auth: AuthService,
    public listService: ListService,
    private pathService: PathService,
    private explorerService: ExplorerService,
    @Inject(JQ_TOKEN) private $: any) {
  }

  ngOnChanges(simpleChanges: SimpleChanges) {
    if (simpleChanges.manager) {
      this.pathAllowedOperations = !!this.manager.allowedOperations ? this.manager.allowedOperations.filter((ao) => {
        (ao.batchOperation.type === 'SearchRequest') && ( this.isSearchAOEnabled = true);
        return ao.batchOperation.type !== 'MoveIntoRequest';
      }) : [];
    }
  }
  
  // Description: Trigger update of the list view
  refreshView() {
    this.updateView.emit();
  }

  // Description: Trigger Multiple action request from listservice selected rows (path items)
  // Path specific allowedOperations
  // --------------------------------------------------------------------------------------------
  processMultipleBatchRequest(requestBatchData: IRequestBatchData, isGlobal: boolean = false) {
    // console.log(requestBatchData, isGlobal);
    let rows = this.listService.selectedRowItems;

    // This might need to be readjusted
    if (isGlobal) {
      rows = [{
        pathIdentifier: this.pathIdentifier,
        type: 'ManagerPathModel',
        name: this.manager.pathName,
        allowedOperations: this.manager.allowedOperations
      }];
    }

    return this.processBatchUiAction.emit(Object.assign(requestBatchData, { rows: rows }));
  }

}
