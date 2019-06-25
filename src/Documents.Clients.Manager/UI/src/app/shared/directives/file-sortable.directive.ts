import { Directive, OnInit, Inject, ElementRef, Input, Output, EventEmitter } from '@angular/core';
import { ToastsManager } from 'ng2-toastr';
import { IconPipe } from '../pipes/icon.pipe';
import { IRow, IItemQueryTypeBase, ItemQueryType } from '../models/view.model';
import { IAllowedOperation } from '../models/allowed-operation.model';
import { ListService } from '../services/list.service';
import { AuthService } from '../services/auth.service';
import { BatchOperationService } from '../services/batch-operation.service';
import { IBatchOperation, GridViewModel } from '../index';
import * as _ from 'lodash';
const { isEqual } = _;

@Directive({
  selector: '[appFileSortable]'
})
export class FileSortableDirective implements OnInit {
  private el: HTMLElement;
  @Input('appFileSortable') row: ItemQueryType;
  @Input() dropSource: boolean = false;
  @Input() dropTarget: boolean = false;
  @Input() gridView: GridViewModel;
  @Output() saveMove = new EventEmitter();
  // private isDraggingHelper:boolean= false;
  constructor(
    ref: ElementRef,
    public auth: AuthService,
    private batchOperationService: BatchOperationService,
    private iconPipe: IconPipe,
    private toastr: ToastsManager,
    private listService: ListService) {
    this.el = ref.nativeElement;
  }

  ngOnInit() {
    // Do not allow Drag for this row
    const _self = this;
    const iconClass = 'file'; // this.iconPipe.transform(_self.file.icons, (!_self.file.isPath ? 'file' : 'folder')); //***** NEEDS REVISITING
    // Save data for Jquery interaction
    $(this.el).data('row', _self.row);
    // This item is dropSource:
    if (this.dropSource) {
      let draggingRowItems: any[] = [];
      $(this.el).draggable({
        cancel: '.dropdown, :input', // fixes the IE11 bug with the dropdown menu and editmode inputs
        cursor: 'move',
        cursorAt: { top: 5, left: 10 },
        delay: 200,
        helper: function (event) { 
          let numberFiles: number = 1;
          // If multiple files selected then these are the ones being dragged
          // Count how many are being dragged and add the number to the badge, skip the NON-dropsource rows that may be checked
          (!_self.row.selected) && _self.gridView.changeSelectRow(true, _self.row);
          _self.listService.draggingRowItems = _self.gridView.selectedRowItems.filter((row) => {
            (!row.dropSource) && (row.uiClass = 'not-draggable');//_self.gridView.changeSelectRow(false, row);
            return row.dropSource;
          });

          draggingRowItems = _self.listService.draggingRowItems;
          numberFiles = draggingRowItems.length;

          // Add the dragging class
          draggingRowItems.forEach(f => { f.uiClass = 'ui-dragging'; });
          const helper = $(`<div class="dragging-helper"><span class="badge">${numberFiles}</span>
            <i class="icon ${iconClass}"></i></div>`);
          return helper;
        },
        stop: function (event, ui) {
          // Remove the dragging class and non draggable items
          // Use _self.listService.draggingRowItems.length> 0  to pass a toastr of failed drag and drop
          draggingRowItems.forEach(f => { f.uiClass = ''; });
          draggingRowItems = [];
          setTimeout(() => {
            $('.ui-dragging').removeClass('ui-dragging');
            $('.not-draggable').removeClass('not-draggable');
            _self.gridView.removeSelectedRowItemsUIClass('not-draggable');
          }, 1000);
          _self.listService.draggingRowItems = [];
        }
      }).addClass('dropSource');
    }

    // This item is dropTarget:
    if (this.dropTarget) {
      $(this.el).droppable({
        accept: '.dropSource',
        tolerance: 'pointer',
        over: function (event, ui) {
          let highlightClass = 'ui-state-highlight',
            isProhibited = _self.isProhibitedDropTarget(_self.listService.draggingRowItems, $(event.target).data('row'));
          if (isProhibited) {
            highlightClass = 'not-dropabble';
            $('.dragging-helper').addClass('inError');
          }

          $(event.target).closest('.node-wrapper').length ? $(event.target).closest('.node-wrapper').addClass(highlightClass) :
            $(event.target).addClass(highlightClass);
        },
        out: function (event, ui) {
          $('.dragging-helper').removeClass('inError');
          $(event.target).closest('.node-wrapper').length ? $(event.target).closest('.node-wrapper').removeClass('ui-state-highlight not-dropabble') :
            $(event.target).removeClass('ui-state-highlight not-dropabble');
        },
        drop: function (event, ui) {
          $('.dragging-helper').removeClass('inError');
          $(event.target).closest('.node-wrapper').length ? $(event.target).closest('.node-wrapper').removeClass('ui-state-highlight not-dropabble') :
            $(event.target).removeClass('ui-state-highlight not-dropabble');
          // Handle save on correct drop
          // If multiple files selected then these are the ones being dragged - add the object as source
          let sourceArr = _self.listService.draggingRowItems, //_self.listService.selectMode ? _self.listService.draggingRowItems : [$(ui.draggable).data('row')],
            identifier = $(event.target).data('row').identifier,
            isProhibited = _self.isProhibitedDropTarget(_self.listService.draggingRowItems, $(event.target).data('row'));
          if (!!sourceArr && sourceArr.length <= 1 && isProhibited) {
            _self.toastr.warning('The source folder is the same as the destination folder');
            return;
          } else {
            if (isProhibited) {
              _self.toastr.warning('One of the folders being moved, is the same as the destination folder');
              sourceArr = _self.listService.draggingRowItems.filter((item) => {
                return !_.isEqual(item.identifier, identifier);
              });
            }
            // Pull sourceArr as the rows array to pass back: fix the rows target key in the batch operation
            if (!!identifier) {
              let batchoperations = <IBatchOperation[]>(_self.batchOperationService.retrieveRequestOperation(sourceArr, 'MoveIntoRequest'));
              batchoperations.filter((batchoperation) => {
                batchoperation.targetPathIdentifier = identifier;
                return true;
              });
            };
            // Emit save
            _self.listService.draggingRowItems = [];
            return _self.saveMove.emit(sourceArr);
          }
        }
      });
    }
  }

  // Description: Check to see there is a draggable with the same identifier as the droppable
  isProhibitedDropTarget(draggables: ItemQueryType[], droppable: any) {
    let isProhibited = false;
    for (let item of draggables) {
      isProhibited = (_.isEqual(item.identifier, droppable.identifier));
      if (isProhibited) {
        break;
      }
    }
    return isProhibited;
  }
}
