import { Component, OnInit, SimpleChanges, Input, Output, EventEmitter, Inject, ViewChild } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { IGridView, ItemQueryType, IPagination, IPathIdentifier, IAllowedOperation, IRequestBatchData, GridViewModel, ViewMode, IFile, JQ_TOKEN } from '../../index';
import { ExplorerService, ListService, BatchOperationService, ValidationPatterns } from '../../index';
import { ImageGalleryComponent } from '../image-gallery/image-gallery.component';
@Component({
  selector: 'grid-view',
  templateUrl: './grid-view.component.html',
  styleUrls: ['./view-types.component.scss']
})
export class GridViewComponent implements OnInit {
  @ViewChild(ImageGalleryComponent) imageGalleryComponent: ImageGalleryComponent;
  @Input() viewMode: ViewMode;
  @Input() pathIdentifier: IPathIdentifier;
  @Input() viewItem: IGridView;
  @Input() pageFiltersParams: any;
  @Input() viewItemId: string = 'viewItemId';
  @Input() gridviewValidators: { [key: string]: ValidationPatterns[] };;
  @Output() processBatchUiAction = new EventEmitter();

  public isGalleryShown: boolean = false;
  public gridView: GridViewModel;
  public galleryItems: ItemQueryType[]; //Holds items that are either video or images
  public pagination: IPagination;
  //Gridview Service:
  public collapsed?: boolean = false;
  public gridTitle: string = '';
  public rows: ItemQueryType[] = [];
  public allowedOperations: IAllowedOperation[];
  public allowedOperationsIntersected: IAllowedOperation[];
  public hasRowsAllowedOperations: boolean;
  constructor(
    private sanitizer: DomSanitizer,
    public listService: ListService,
    private batchOperationService: BatchOperationService,
    private explorerService: ExplorerService,
    @Inject(JQ_TOKEN) private $: any
  ) {
  }

  ngOnInit() {
    this.gridView = new GridViewModel(this.viewItem, this.viewItemId);
    this.hasRowsAllowedOperations = this.gridView.hasRowsAllowedOperations;
    this.gridTitle = this.gridView.title;
    this.collapsed = this.gridView.collapsed;
    this._setRowUIProperties(this.gridView.rows);
    this.allowedOperations = this.gridView.allowedOperations;
    this.allowedOperationsIntersected = this.gridView.allowedOperationsIntersected;
    this.rows = this.gridView.rows;
    this.pagination = <IPagination>this.viewItem;
    this.galleryItems = this._getGalleryItems();
  }

  private _getGalleryItems() {
    let galleryItems = this.gridView.rows.filter((item) => {
      if ((item as IFile).viewerType === 'Image') {
        let img = (item as IFile);
        let url = `/api/file/contents?fileidentifier.organizationKey=${item.identifier.organizationKey}&fileidentifier.folderKey=${item.identifier.folderKey}&fileidentifier.fileKey=${item.identifier.fileKey}&open=true`;
        let thumbUrl = !!(<IFile>item).previewImageIdentifier ?
          `/api/file/contents?fileidentifier.organizationKey=${img.previewImageIdentifier.organizationKey}&fileidentifier.folderKey=${img.previewImageIdentifier.folderKey}&fileidentifier.fileKey=${img.previewImageIdentifier.fileKey}&open=true` : 
          '/assets/images/image-placeholder-120x70.png';
        item.safeUrl = url.replace(/ /gi, "%20"); //this.sanitizer.bypassSecurityTrustUrl(url);
        item.safeStyle = this.sanitizer.bypassSecurityTrustStyle(`url("${url}")`);
        item.safeThumbUrl = thumbUrl.replace(/ /gi, "%20"); // this.sanitizer.bypassSecurityTrustUrl(thumbUrl);
        // item.safeThumbStyle = this.sanitizer.bypassSecurityTrustUrl(`url("${thumbUrl}")`);
        return true;
      }
      return false;
    });
    return galleryItems;
  }

  private _setRowUIProperties(rows: ItemQueryType[]) {
    if (rows.length > 0) {
      rows.map((row) => {
        // Determine draggable droppable permissions
        row.dropSource = this.batchOperationService.isPathMoveIntoItem(row, 'sourcePath') ||
          this.batchOperationService.isPathMoveIntoItem(row, 'sourceFile');
        row.dropTarget = this.batchOperationService.isPathMoveIntoItem(row, 'targetPath');
        
        // Get buttons driven AO:
        row.allowedOperationsButtonDriven = this.gridView.getAllowedOperationsButtonDependent(row.allowedOperations);

        //Set row validators from server regex
        row.rowInlineValidators = (row.type === "ManagerPathModel") ? this.gridviewValidators["pathNameValidationPatterns"] :
          (row.type === "ManagerFileModel") ? this.gridviewValidators["fileNameValidationPatterns"] : [];

      });
    }
  }

  toggleSelectRow($event: { selected: boolean, row: ItemQueryType, index: number }) {
    this.gridView.changeSelectRow($event.selected, $event.row, $event.index);
    this.listService.previewRow($event.selected, $event.row, $event.index);
  }

  toggleAllItems(event: any) {
    let selected: boolean = event.target.checked;
    this.gridView.toggleAllItems(selected);
    this.listService.previewRow(selected, null, null);
  }

  // ----------------------------------------------------------------------
  // Lists events - multiple and single actions
  processMultipleBatchRequest(requestBatchData: IRequestBatchData, isMultiple: boolean = true) {
    if (isMultiple) {
      const multiBatchOperations = this.gridView.selectedBatchOperations[requestBatchData.requestType];
      (!!this.gridView.selectedBatchOperations[requestBatchData.requestType]) &&
        Object.assign(requestBatchData, { rows: this.gridView.selectedRowItems, batchOperations: multiBatchOperations });
    }
    return this.processBatchUiAction.emit(requestBatchData);
  }
}
