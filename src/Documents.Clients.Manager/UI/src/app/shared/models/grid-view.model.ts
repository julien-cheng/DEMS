import {
  IGridView,
  ItemQueryType,
  IAllowedOperation,
  IGridColumnSpecification,
  ViewMode,
  IGridViewProperties,
  IBatchOperation,
  BatchRequestType
} from '../index';
import * as _ from 'lodash';
const { intersection, intersectionBy, drop } = _;

export class GridViewModel {
  private _gridView: IGridView;
  public key: string;
  public title: string;
  public gridColumns: IGridColumnSpecification[];
  public allSelected: boolean;
  public selectedItem: ItemQueryType; // selectedRow deprecated to selectedItem
  public selectedItems: ItemQueryType[]; // An array of indexes of the selected items
  public viewMode: ViewMode;
  public totalRows: number;
  public collapsed: boolean;
  public hasRowsAllowedOperations: boolean;
  public rows: ItemQueryType[];
  public allowedOperations: IAllowedOperation[];
  public allowedOperationsIntersected: IAllowedOperation[];
  public selectedRowItems: ItemQueryType[] = [];
  public selectedBatchOperations: {
    [batchRequestType: string]: IBatchOperation[];
  };

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
      row.selected = selected;
    });
    this.selectedRowItems = this.selectedRowItems = selected ? this.rows.slice() : [];
    this.allowedOperationsIntersected = this.intersectRows(this.selectedRowItems);
    return this._setGridViewProperties({ allSelected: selected }, false);
  }

  // Triggered when a row has been selected/checked
  public changeSelectRow(selected: boolean, row: ItemQueryType, index?: number) {
    row.selected = selected;
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
      row.uiClass === uiClass && (row.uiClass = '');
    });
  }

  // Private Methods:
  // ------------------------------------------------------------
  private _setGridViewProperties(gridViewProperties?: IGridViewProperties, resetAllStatus: boolean = true) {
    if (resetAllStatus) {
      const defaultOAbyRow = this.rows.filter(row => !!row.allowedOperations && row.allowedOperations.length > 0),
        propDefaults = {
          allSelected: false,
          selectedItem: null,
          selectedItems: [],
          viewMode: this.viewMode,
          totalRows: this.rows.length | 0,
          collapsed: false,
          hasRowsAllowedOperations: defaultOAbyRow.length > 0
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
    const arrOfAO = rows.map(row => {
      return row.allowedOperationsButtonDriven;
    });
    // allowedoperations = intersectionBy(...arrOfAO, 'displayName');
    allowedoperations = intersectionBy(...arrOfAO, e => e.batchOperation.type && e.displayName);
    _.remove(allowedoperations, { isSingleton: true }); // Remove isSingleton
    this._setSelectedBatchOperations(allowedoperations);
    return allowedoperations;
  }

  private _setSelectedBatchOperations(allowedOperations: IAllowedOperation[]) {
    const rows = this.selectedRowItems;
    const selectedBatchOperations = {};
    allowedOperations.forEach(ao => {
      const batchOperation = this.retrieveRequestOperation(rows, ao.batchOperation.type, ao.displayName);
      batchOperation && batchOperation.length > 0 && (selectedBatchOperations[ao.batchOperation.type] = batchOperation);
    });
    this.selectedBatchOperations = selectedBatchOperations;
  }

  public retrieveRequestOperation(rows: ItemQueryType[], requestType: BatchRequestType, displayName: string): IBatchOperation[] {
    const objectTyped: IBatchOperation[] = [];
    if (!!rows) {
      rows.map(row => {
        row.allowedOperations.forEach(operation => {
          operation.batchOperation.type === requestType &&
            operation.displayName === displayName &&
            objectTyped.push(operation.batchOperation);
        });
      });
    }
    return objectTyped;
  }

  // Returns allowedOperationsButtonDriven for each row
  public getAllowedOperationsButtonDependent(operations: IAllowedOperation[]): IAllowedOperation[] {
    let showBtnGroup = [];
    if (!!operations) {
      showBtnGroup = operations.filter(operation => {
        const displayButton: boolean =
          operation.batchOperation.type === 'MoveIntoRequest' // Do not add menu buttons for non-source
            ? !!operation.batchOperation.sourceFileIdentifier || !!operation.batchOperation.sourcePathIdentifier
            : operation.batchOperation.type !== 'UploadRequest'
            ? true
            : false;
        return displayButton;
      });
    }
    return showBtnGroup;
  }
}
