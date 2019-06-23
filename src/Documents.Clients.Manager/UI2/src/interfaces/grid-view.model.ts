import * as _ from 'lodash';
import {
  IGridView,
  IGridColumnSpecification,
  ItemQueryType,
  ViewMode,
  IGridViewProperties,
} from './view.model';
import { IAllowedOperation } from './allowed-operation.model';
import { IBatchOperation, BatchRequestType } from './request-api';
const { intersection, intersectionBy, drop } = _;

export class GridViewModel {
  private _gridView: IGridView;
  public key: string;
  public title: string;
  public gridColumns?: IGridColumnSpecification[];
  public allSelected: boolean | undefined;
  public selectedItem: ItemQueryType | undefined; // selectedRow deprecated to selectedItem
  public selectedItems: ItemQueryType[] | undefined; // An array of indexes of the selected items
  public viewMode: ViewMode | undefined;
  public totalRows: number | undefined;
  public collapsed: boolean | undefined;
  public hasRowsAllowedOperations: boolean | undefined;
  public rows: ItemQueryType[];
  public allowedOperations?: IAllowedOperation[];
  public allowedOperationsIntersected: IAllowedOperation[];
  public selectedRowItems: ItemQueryType[] = [];
  public selectedBatchOperations:
    | {
        [batchRequestType: string]: IBatchOperation[];
      }
    | undefined;

  constructor(gridView: IGridView, key: string) {
    this._gridView = gridView;
    this.key = key;
    this.title = this._gridView.title;
    this.gridColumns = gridView.gridColumns;
    this.allowedOperations = this._gridView.allowedOperations;
    this.rows = this._gridView.rows;
    this.allowedOperationsIntersected = this.intersectRows(this.rows);
    this._setGridViewProperties();
  }

  public setGridViewProperties(gridViewProperties?: IGridViewProperties) {
    return this._setGridViewProperties(gridViewProperties, false);
  }

  public toggleAllItems(selected: boolean) {
    this.rows.forEach(row => {
      (row as any).selected = selected;
    });
    this.selectedRowItems = this.selectedRowItems = selected ? this.rows.slice() : [];
    this.allowedOperationsIntersected = this.intersectRows(this.selectedRowItems);
    return this._setGridViewProperties({ allSelected: selected }, false);
  }

  // Triggered when a row has been selected/checked
  public changeSelectRow(selected: boolean, row: ItemQueryType, index?: number) {
    (row as any).selected = selected;
    const selectedIndex = this.selectedRowItems.indexOf(row);
    let selectedRow = row;

    if (selected) {
      selectedIndex === -1 && this.selectedRowItems.push(row);
    } else {
      selectedIndex > -1 && this.selectedRowItems.splice(selectedIndex, 1);
      selectedRow = this.selectedRowItems[this.selectedRowItems.length - 1];
    }
    this.allowedOperationsIntersected = this.intersectRows(this.selectedRowItems);
  }

  public removeSelectedRowItemsUIClass(uiClass: string) {
    this.selectedRowItems.forEach(row => {
      (row as any).uiClass === uiClass && ((row as any).uiClass = '');
    });
  }

  // Private Methods:
  // ------------------------------------------------------------
  private _setGridViewProperties(
    gridViewProperties?: IGridViewProperties,
    resetAllStatus: boolean = true,
  ) {
    if (resetAllStatus) {
      let defaultOAbyRow = this.rows.filter(row => {
          return !!(row as any).allowedOperations && (row as any).allowedOperations.length > 0;
        }),
        propDefaults = {
          allSelected: false,
          selectedItem: null,
          selectedItems: [],
          viewMode: this.viewMode,
          totalRows: this.rows.length | 0,
          collapsed: false,
          hasRowsAllowedOperations: defaultOAbyRow.length > 0,
        };
      Object.assign(this, propDefaults);
    } else {
      Object.assign(this, gridViewProperties);
    }
    // resetAllStatus ? Object.assign(this, propDefaults) : Object.assign(this, gridViewProperties);
  }

  // Multiselect allowed operations
  // -------------------------------------------------------
  // -------------------------------------------------------
  // Description: Extract intersection of all grid rows allowed operations.
  public intersectRows(rows: ItemQueryType[]): IAllowedOperation[] {
    let allowedoperations = [];
    let arrOfAO = rows.map(row => {
      return (row as any).allowedOperationsButtonDriven;
    });
    //allowedoperations = intersectionBy(...arrOfAO, 'displayName');
    allowedoperations = intersectionBy(arrOfAO, (e: any) => {
      return e.batchOperation.type && e.displayName;
    });
    _.remove(allowedoperations, { isSingleton: true }); // Remove isSingleton
    this._setSelectedBatchOperations(allowedoperations);
    return allowedoperations;
  }

  private _setSelectedBatchOperations(allowedOperations: IAllowedOperation[]) {
    let rows = this.selectedRowItems;
    let selectedBatchOperations = {};
    allowedOperations.forEach(ao => {
      const batchOperation = this.retrieveRequestOperation(
        rows,
        ao.batchOperation.type,
        ao.displayName,
      );
      batchOperation &&
        batchOperation.length > 0 &&
        ((selectedBatchOperations as any)[ao.batchOperation.type] = batchOperation);
    });
    this.selectedBatchOperations = selectedBatchOperations;
  }

  public retrieveRequestOperation(
    rows: ItemQueryType[],
    requestType: BatchRequestType,
    displayName: string,
  ): IBatchOperation[] {
    let objectTyped: IBatchOperation[] = [];
    if (!!rows) {
      rows.map(row => {
        (row as any).allowedOperations.forEach(
          (operation: { batchOperation: IBatchOperation; displayName: string }) => {
            operation.batchOperation.type === requestType &&
              operation.displayName === displayName &&
              objectTyped.push(operation.batchOperation);
          },
        );
      });
    }
    return objectTyped;
  }

  //Returns allowedOperationsButtonDriven for each row
  public getAllowedOperationsButtonDependent(operations: IAllowedOperation[]): IAllowedOperation[] {
    let showBtnGroup: IAllowedOperation[] | never[] = [];
    if (!!operations) {
      showBtnGroup = operations.filter(operation => {
        let displayButton: boolean =
          operation.batchOperation.type === 'MoveIntoRequest' // Do not add menu buttons for non-source
            ? !!operation.batchOperation.sourceFileIdentifier ||
              !!operation.batchOperation.sourcePathIdentifier
            : operation.batchOperation.type !== 'UploadRequest'
            ? true
            : false;
        return displayButton;
      });
    }
    return showBtnGroup;
  }
}
