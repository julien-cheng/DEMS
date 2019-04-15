import { IRow, ItemQueryType } from '../index';

// Holds information about paging
export interface IPagination {
  pathName: string;
  pageIndex: number;
  pageSize: number;
  pageCount: number;
  rowsInPage: number;
  totalRows: number;
  isLastPage: boolean;
  rows?: ItemQueryType[];
}

export interface IPageLink {
  pagerUrl: string;
  params?: any;
  pageNumber: number;
  linkClass?: string;
}
