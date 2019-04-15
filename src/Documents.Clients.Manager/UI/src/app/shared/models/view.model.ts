import { IPagination, IFile, IPath, IRecipient, IAllowedOperation, TIdentifierType, ValidationPatterns } from '../index';

// Views:
// --------------------------------------------
export interface IView {
  type: string;
  title: string;
  allowedOperations?: IAllowedOperation[];
}

// View Types: Object and Grid Views
export type ViewType = IObjectView | IGridView;
// ObjectView:
export interface IObjectView extends IView {
  dataSchema?: any | null; // TBD
  dataModel?: any | null; // TBD
}

export interface IGridView extends IView, IPagination {
  gridColumns: IGridColumnSpecification[] | null;
  rows: ItemQueryType[];
}

// GridViews:
export interface IGridColumnSpecification {
  keyName: string;
  label: string;
  isSortable?: boolean;
}

// export type ViewMode = 'list' | 'details' | 'icons'; for Gridviews
export enum ViewMode {
  list = 'list',
  details = 'details',
  icons = 'icons'
}

// Properties of the Grid rows
export interface IGridViewProperties {
  allSelected?: boolean;
  selectedItem?: ItemQueryType; // selectedRow deprecated to selectedItem
  selectedItems?: ItemQueryType[]; // An array of indexes of the selected items
  viewMode?: ViewMode;
  totalRows?: number;
  collapsed?: boolean;
  hasRowsAllowedOperations?: boolean; // stores the visibility of the gridview action toolbar (row dependent)
}

export type ItemQueryType = IFile | IPath | IRecipient;

// Description: Holds clientside information for the itemQueries
export interface IRow {
  selected?: boolean; // used only in frontend
  uiClass?: string; // Pass extra action client side classes
  editMode?: boolean;
  dropSource?: boolean; // dropSource - draggable
  dropTarget?: boolean; // dropTarget droppable - Folders etc... dropTarget
  allowedOperationsButtonDriven?: IAllowedOperation[];
  rowInlineValidators?: ValidationPatterns[];
  isExpanded?: boolean; // For path objects in the explorer
  safeUrl?: any;
  safeStyle?: any;
  safeThumbUrl?: any;
}

export interface IItemQueryTypeBase extends IRow {
  type: string;
  name: string;
  customName?: string;
  identifier?: any;
  icons?: string[];
  dataModel?: any | null; // TBD
  allowedOperations?: IAllowedOperation[];
  fullPath?: string;
  attributes?: any; // For search Results
}

export enum LinkType {
  'ManagerFileModel' = 'file',
  'ManagerFileSearchResult' = 'file',
  'EDiscoveryManagerPathModel' = 'path',
  'ManagerPathModel' = 'path',
  'ManagerRecipientModel' = 'recipient'
}
