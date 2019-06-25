import { ItemQueryType } from "..";

export interface ISearch {
    rows: ItemQueryType[];
    facets: IFacetGroup[];
    totalMatches: number;
}

export interface IFacetGroup {
    name: string;
    values: IFacet[];
    label?: string;
}

export interface IFacet {
    value: string;
    count: number;
    label?: string; 
    filter?: ISearchFilter; // UI
    additionalParams?:any; // UI
}


export interface ISearchRequest { // [{name: string; value: string}];
    filters: ISearchFilter[]; 
    keyword: string;
    paging: ISearchPagination;
    disableReturn: boolean;
}

export interface ISearchFilter{
    name: string; 
    value: string;
    label?: string;
}

export interface ISearchPagination{
    pageIndex: number;
    pageSize: number;
    sortfield: string;
    isAscending: boolean;
   
}

// export interface ISearchPaginationUI extends ISearchPagination{
//     totalRows?:number; // UI
//     pageCount?: number; // UI
// }