import { Component, OnInit, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { ItemQueryType, GridViewModel, ExplorerService, FileService, PathService, IPathIdentifier, IRequestBatchData, IGridColumnSpecification } from '../../index';
import * as _ from 'lodash';
const { find } = _;

@Component({
    selector: 'details-view',
    templateUrl: './details-view.component.html',
    styleUrls: ['./details-view.component.scss']
})
export class DetailsViewComponent implements OnInit {
    @Input() gridView: GridViewModel;
    @Input() pathIdentifier: IPathIdentifier;
    @Input() pageFiltersParams: any;
    @Output() toggleAll = new EventEmitter();
    @Output() changeSelectRow = new EventEmitter();
    @Output() processBatchUiAction = new EventEmitter();
    public hasRowsAllowedOperations: boolean;
    public rows: ItemQueryType[];
    public gridColumns: IGridColumnSpecification[];
    public pathValidation: any = { 'pattern': '^[a-zA-Z0-9-_. ]+' };
    public fileValidation: any = { 'pattern': null };
    public sortField: string = 'name';
    public sortAscending: boolean = true;
    public baseUrl: string;
    constructor(
        public explorerService: ExplorerService,
        private pathService: PathService,
        private fileService: FileService) {
        
    }

    ngOnInit() {
        this.hasRowsAllowedOperations = this.gridView.hasRowsAllowedOperations;
        this.rows = this.gridView.rows;
        this.gridColumns = this.gridView.gridColumns;
    }

    ngOnChanges(simpleChanges: SimpleChanges) {
        if (this.pageFiltersParams.sortAscending && this.pageFiltersParams.sortField) {
            this.sortAscending = <boolean>(this.pageFiltersParams.sortAscending === 'true');
            this.sortField = this.pageFiltersParams.sortField;
        } else {
            this.sortField = 'name';
            this.sortAscending = true;
        }


        if (simpleChanges.pathIdentifier) {
            // this.folderKey = this.pathIdentifier.folderKey;
            // this.pathKey = this.pathIdentifier.pathKey;
            this.baseUrl = this.pathService.getBaseUrl(this.pathIdentifier);
        }
    }

    saveCallback(requestBatchData: IRequestBatchData, rows?: ItemQueryType[]) {
        (!!rows) && Object.assign(requestBatchData,{rows: rows});
        return this.processBatchUiAction.emit(requestBatchData);
     }
}
