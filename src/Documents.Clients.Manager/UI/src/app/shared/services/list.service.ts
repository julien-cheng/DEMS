import { Injectable } from '@angular/core';
import { IManager, IGridView, ItemQueryType, ViewMode, IAllowedOperation } from '../index';
import * as _ from 'lodash';
const { intersection, intersectionBy, drop } = _;

@Injectable()
export class ListService {
  // Path specific props
  public previewVisible: boolean; // Preview panel is visible

  // GridViewModel
  // public totalRows: number = 0;
  public selectedRowItems: ItemQueryType[]; // An array of indexes of the selected items
  public allSelected: boolean; // A flag to indicate that all list items are selected
  public selectMode: boolean; // Select All checkbox is checked and individual checkboxes are visible
  public selectedRow: ItemQueryType; // File displayed in preview panel
  public gridsViewMode?: { [key: string]: ViewMode }; // Hold the view mode of the listview { gridViewKey: viewmode}
  public draggingRowItems?: ItemQueryType[]; // Hold the items being dragged for moveinto batchoperation -

  constructor() {
    this.previewVisible = false;
    this.selectedRowItems = [];
    // this.selectMode = false;
    this.allSelected = false;
  }

  // Hold the gridviewMode when avail - previously used
  setGridsViewMode() {
    return (this.gridsViewMode = {});
  }

  // Additional List Item FUNCTIONALITY
  // -------------------------------------------------------
  // SELECT/UNSELECT FUNCTIONALITY
  // -------------------------------------------------------
  // Description: Select or unselect single item  -> Deprecated (will be file-sortable.directive need to move to GridviewModel)
  toggleSingleItem(selected: boolean, file: ItemQueryType) {
    // Adjust the preview panel:
    const index = this.selectedRowItems.length - 1;
    this.previewRow(selected, file, index);
  }

  // // Description: Determines if this line is selected -> Deprecated (will be file-sortable.directive need to move to GridviewModel)
  isSelected(row: ItemQueryType) {
    return row.selected !== undefined ? row.selected : false;
  }

  // PREVIEW FUNCTIONALITY
  // -------------------------------------------------------
  // Description: show preview panel on the right
  togglePreviewPanel() {
    // console.log('show preview panel on the right: ' + this.previewVisible);
    this.previewVisible = !this.previewVisible;
  }

  // Description: adds line to array if checked and loads the preview in the panel
  previewRow(selected: boolean, row: ItemQueryType, index?: number) {
    if (row === null) {
      return (this.selectedRow = null);
    }

    const selectedIndex = this.selectedRowItems.indexOf(row);
    let selectedRow = row;
    // row.selected = selected;
    if (selected) {
      // console.log('this is a selection:' +selectedIndex);
      if (selectedIndex === -1) {
        this.selectedRowItems.push(row);
      }
    } else {
      // console.log('this is unchecking: ' + selectedIndex);
      if (selectedIndex > -1) {
        this.selectedRowItems.splice(selectedIndex, 1);
      }
      selectedRow = this.selectedRowItems[this.selectedRowItems.length - 1];
    }
    // this.selectMode = this.selectedRowItems.length >= 1;
    this.selectedRow = selectedRow;
    // this.listAllowedOperations = this.intersectRows(this.selectedRowItems);
    // this.intersectedRows = this.selectedRowItems;
  }

  // Reset the multiselect action (for navigation and misc)
  resetMultiselectMode(): void {
    // this.selectMode = false;
    this.allSelected = false;
    this.resetSelectedItems();
  }
  // Description: Removes all selections
  resetSelectedItems(): void {
    this.selectedRowItems = [];
    this.selectedRow = null;
  }
}

// Multiselect allowed operations
// -------------------------------------------------------
// Description: Extract intersection of all grid rows allowed operations.
// Returns an array of commonly avail. Allowed operations
// public listAllowedOperationsIntersect(gridview: IGridView[]) {
//   let allowedoperations = [],
//     rows: ItemQueryType[] = [];
//   gridview.forEach((grid) => {
//     allowedoperations = allowedoperations.concat(this.intersectRows(grid.rows));
//     rows = grid.rows;
//   });

//   // Might need to remove duplicate if more than one gridview use something like:
//   // Needs testing with more than one gridview in a page
//   allowedoperations = allowedoperations.filter((elem, index) => allowedoperations.indexOf(elem) === index);
//   // console.log(allowedoperations);
//   this.listAllowedOperations = allowedoperations;
// }

// // https://lodash.com/docs/4.17.4#intersectionWith, https://lodash.com/docs/4.17.4#intersection
// public intersectRows(rows: ItemQueryType[]): IAllowedOperation[] {
//   let allowedoperations = [];
//   let arrOfAO = rows.map((row) => {
//     return row.allowedOperations;
//   });

//   allowedoperations = intersectionBy(...arrOfAO, 'displayName');
//   // TEMP:  Remove when isSingleton is added to IAllowedOperation
//   let removeSingleton = _.remove(allowedoperations, { displayName: "Rename" }); //REMOVE THIS ONCE issue #60 is done
//   _.remove(allowedoperations, {isSingleton: true});

//   return allowedoperations;
// }

// VIEW MODE FUNCTIONALITY
// -------------------------------------------------------
// Description: Updates the viewmode (list,details, etc)
// UpdateViewmode(viewMode: string) {
//   console.log('UpdateViewmode:'  + viewMode);
//   this.viewMode = viewMode;
// }
// Description: show preview panel on the right
// toggleSelectMode() {
//   // console.log('toggleSelectMode: ' + this.selectMode);
//   this.selectMode = !this.selectMode;
// }

// Description: show preview panel on the right
// toggleAllItems(selected: boolean, view: IGridView) {
//   let rows: ItemQueryType[] = view.rows;
//   this.allSelected = selected;
//   rows.forEach(row => { row.selected = selected; });
//   this.selectedRowItems = (selected) ? rows.slice() : [];
//   // Adjust the preview panel:
//   const index = this.selectedRowItems.length - 1,
//     selectedRow = this.selectedRowItems.length ? this.selectedRowItems[index] : null;
//   this.previewRow(selected, selectedRow, index);
// }

// toggleAllItems(rows: ItemQueryType[], selected: boolean) {
//   this.allSelected = selected;
//   rows.forEach(row => {row.selected = selected;});
//   this.selectedRowItems = (selected) ? rows.slice() : [];
//   // Adjust the preview panel:
//    const index = this.selectedRowItems.length - 1,
//      selectedRow = this.selectedRowItems.length ? this.selectedRowItems[index] : null;
//    this.previewRow(selected, selectedRow, index);
// }
