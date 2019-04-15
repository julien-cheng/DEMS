import { Component, OnInit, Input, Inject, ViewContainerRef, ViewChild, ElementRef, NgZone, OnDestroy } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { FormGroupDirective, FormGroup, FormControl, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import {
  IPathIdentifier,
  IAutodownloadKeys,
  IManager,
  IPath,
  ViewType,
  ViewMode,
  ItemQueryType,
  IGridView,
  IFolderIdentifier,
  EventType
} from '../index';
import { IRequestBatchData, BatchRequest, BatchResponse, IAllowedOperation, IBatchRequestOperations } from '../index';
import { JQ_TOKEN, AppConfigService, LoadingService, ExplorerService, ListService, BatchOperationsComponent } from '../index';
import { PathService, BatchOperationService, excludePatternValidator, ValidationPatterns } from '../index'; // Probably will be removed

import * as _ from 'lodash';
const { includes, pick, intersectionBy } = _;

@Component({
  selector: 'app-manager-view',
  templateUrl: './manager-view.component.html',
  styleUrls: ['./manager-view.component.scss']
})
export class ManagerViewComponent implements OnInit, OnDestroy {
  @ViewChild(BatchOperationsComponent)
  BatchOperationsComponent: BatchOperationsComponent;
  public viewMode: ViewMode;
  public pathIdentifier: IPathIdentifier;
  public autodownloadKeys: IAutodownloadKeys;
  public manager: IManager;
  public views: ViewType[];
  public gridviewValidators: { [key: string]: ValidationPatterns[] };

  // Allowed Operations and Request Types Flags - Pass to BatchOperationsComponent
  private allowedOperations: IAllowedOperation[];
  public requestTypesFlags: any;

  // Paging:
  public pageFiltersParams: any;
  private pageFilterSubs: any;

  // Explorer and Move modal:
  private pathKey: string;
  public mainCurrentTreeExplorer: any;
  public modalTreeExplorer: any;

  constructor(
    private zone: NgZone,
    private route: ActivatedRoute,
    private router: Router,
    private toastr: ToastrService,
    private pathService: PathService,
    public batchOperationService: BatchOperationService,
    public explorerService: ExplorerService,
    public listService: ListService,
    public appConfigService: AppConfigService,
    public loadingService: LoadingService,
    @Inject(JQ_TOKEN) private $: any
  ) {}

  ngOnInit() {
    this.route.data.forEach(data => {
      this.pathIdentifier = this.route.snapshot.data.identifiers.pathIdentifier;
      this.autodownloadKeys = this.route.snapshot.data.identifiers.autodownloadKeys;

      // Explorer data:
      this.explorerService.fileExplorer.isCollapsed = false;
      this.pathKey = this.pathIdentifier.pathKey;

      if (!this.viewMode) {
        this.viewMode = this.explorerService.fileExplorer.listviewMode
          ? this.explorerService.fileExplorer.listviewMode
          : this.getDefaulViewMode();
      }

      // Subscribe to the filter params for paging and other filters
      this.pageFilterSubs = this.route.queryParams.subscribe(params => {
        this.pageFiltersParams = params;
        return this.getFiles();
      });
      if (this.$('.modal.show').length > 0) {
        this.$('.modal.show').modal('hide');
      }

      window.scroll(0, 0); // Scroll top
    });
  }

  /// Description: Adjust default view for the path depending on viewport width
  private getDefaulViewMode() {
    return window.innerWidth >= 992 ? ViewMode.details : ViewMode.list; // ViewMode.details default]
  }

  // Description: updates explorer and main page
  private getFiles(successMessage?: string): void {
    const self = this;
    this.zone.run(() => {
      this.loadingService.setLoading(true);
      this.pathService.getPathPage(this.pathIdentifier, this.pageFiltersParams).subscribe(
        response => {
          this.manager = response.response as IManager;
          if (this.manager !== undefined) {
            this.listService.resetMultiselectMode();
            this.explorerService.setCurrentExplorer(this.manager, this.pathIdentifier);
            this.views = this.manager.views;
            this.gridviewValidators = {
              fileNameValidationPatterns: this.manager.fileNameValidationPatterns,
              pathNameValidationPatterns: this.manager.pathNameValidationPatterns
            };

            // Set Global AO and RequestTypes:
            this._setViewsAllowedOperations();

            // Modals Trees:
            this.mainCurrentTreeExplorer = this.explorerService.fileExplorer.currentTreeExplorer;
            if (!!successMessage) {
              self.postMessage(successMessage, 'success');
            }
          }
        },
        error => {
          this.postMessage('There was an error with your request', error);
          throw new Error('Manager is undefined - redirect to error');
        },
        () => {
          // On complete
          this.loadingService.setLoading(false);
        }
      );
    });
  }

  // Description: Pass the set of allowedOperations for this view to the BatchOperationsComponent
  private _setViewsAllowedOperations() {
    this.allowedOperations = [];
    if (!!this.manager.allowedOperations) {
      this.allowedOperations = this.allowedOperations.concat(this.manager.allowedOperations);
    }
    let viewAO = {},
      gridAO = {};
    this.manager.views.forEach(view => {
      if (view.type.toLowerCase() === 'grid') {
        if (!!view.allowedOperations && view.allowedOperations.length > 0) {
          this.allowedOperations = this.allowedOperations.concat(view.allowedOperations);
        }
        (view as IGridView).rows.forEach(row => {
          if (!!row.allowedOperations) {
            gridAO = this.batchOperationService.extractrequestTypesFlags(row.allowedOperations);
          }
        });
        viewAO = Object.assign({}, viewAO, gridAO);
      }
    });
    this.requestTypesFlags = Object.assign({}, viewAO, this.batchOperationService.extractrequestTypesFlags(this.allowedOperations));
  }

  // Description: Remove AutodownloadKeys from pathIdentifier obj when down with the download
  ngOnDestroy() {
    this.pageFilterSubs.unsubscribe();
    this.autodownloadKeys = null;
    if (this.$('.modal.show').length > 0) {
      this.$('.modal.show').modal('hide');
    }
  }

  // Description: send toastr with message
  public postMessage(msg: string, type: string) {
    switch (type) {
      case 'success':
        this.toastr.success(msg);
        break;
      case 'error':
        this.toastr.error(msg);
        break;
      default:
        this.toastr.warning(msg);
        break;
    }
  }

  // ----------------------------------------------------------------
  // Batch Operations component:
  // ----------------------------------------------------------------
  public processBatchUiAction($event: any) {
    return this.BatchOperationsComponent.processBatchUiAction($event);
  }

  // ----------------------------------------------------------------------
  // Events throug Emitters and batchOperations models/services, Output call:
  public updateView(arg?: any) {
    const msg = !!arg && !!arg.hasOwnProperty('successMessage') ? arg.successMessage : null;
    this.getFiles(msg);
  }
}
