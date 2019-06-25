import { Component, OnInit, Input, SimpleChanges } from '@angular/core';
import { ISearchPagination, IPageLink, IPathIdentifier, ISearchRequest, SearchService } from '..';

@Component({
  selector: 'app-search-pagination',
  templateUrl: './search-pagination.component.html',
  styleUrls: ['./search-pagination.component.scss']
})
export class SearchPaginationComponent implements OnInit {
  // @Input() pagination: ISearchPagination;
  @Input() searchRequest: ISearchRequest;
  @Input() totalRecords: number;
  @Input() baseURL: string;
  public presetParams: any;
  public pagination: ISearchPagination;
  public prevParams: {};
  public nextParams: {};

  // Hold pagination information:
  public pagerSize: number = 5;
  public pageIndex: number;
  public pageNumber: number; // pagination.pageIndex
  public pageSize: number; // pagination.pageSize
  public totalPages: number; // pagination.pageCount
  public pagerObj: IPageLink[];
  constructor(public searchService: SearchService) { }

  ngOnInit() {
  }

  ngOnChanges(simpleChanges: SimpleChanges) {
      this.presetParams =this.searchService.encodeAdditionalFilterObject(this.searchRequest.filters);
      this.pagination = this.searchRequest.paging;
      this.pageNumber = Number(this.pagination.pageIndex) + 1;
      this.pageSize = Number(this.pagination.pageSize);
      this.pageIndex = this.pagination.pageIndex;
      this.totalPages = this._getTotalPages();//this.pagination.pageCount;
      this.pagerObj = this.buildPagerObj();
  }

  private _getTotalPages() {
    return Math.ceil(this.totalRecords / this.pagination.pageSize);
  }

  // Description: Build the pager array to loop through
  buildPagerObj(): IPageLink[] {
    // console.log(this.searchRequest, this.pagination, this.presetParams.additionalFilters);  
    let newQS =Object.assign({}, this.presetParams.additionalFilters, {keyword: this.searchRequest.keyword, disableReturn: this.searchRequest.disableReturn}),
        pageLinkArr: IPageLink[] = [];
    const d = (this.pageNumber % 5),
      diff = d > 0 ? 5 - d : 0;
    let max = this.pageNumber + diff;
    let min = max - 4;
    (max > this.totalPages) && (max = this.totalPages);

    for (let i = min; i <= max; i++) {
      const pagelink: IPageLink = {
        pageNumber: i,
        pagerUrl: this.baseURL,
        params: Object.assign({}, newQS, { pageSize: this.pageSize, pageIndex: (i - 1) }),
        linkClass: (i === this.pageNumber ? 'active' : '')
      };
      pageLinkArr.push(pagelink);
    }

    this.prevParams = Object.assign({}, newQS, { pageSize: this.pageSize, pageIndex: (this.pageIndex - 1) });//{ pageSize: this.pageSize, pageIndex: (this.pageIndex - 1) };
    this.nextParams = Object.assign({}, newQS, { pageSize: this.pageSize, pageIndex: this.pageNumber }); //{ pageSize: this.pageSize, pageIndex: this.pageNumber };
    return pageLinkArr;
  }

  // Description: Display pagination? check if pagination is not undefined
  isPaginationVisible() {
    return (this.pagination && this.totalPages > 1); // change to 1 (do not show pager when there is only 1 page)
    // return true;
  }

}
