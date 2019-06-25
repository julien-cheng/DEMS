import { Component, Input, SimpleChanges } from '@angular/core';
import { IPagination, IPageLink, IPathIdentifier } from '../../index';
import { PathService } from '../../services/path.service';
@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.scss']
})
export class PaginationComponent {
  @Input() pagination: IPagination;
  @Input() pathIdentifier: IPathIdentifier;
  @Input() pageFiltersParams: any;
  public baseUrl: string;
  public pageArr: number[];
  public prevParams: {};
  public nextParams: {};
  
  // Hold pagination information:
  public pageIndex: number;
  public pageNumber: number; // pagination.pageIndex
  public pageSize: number; // pagination.pageSize
  public totalRecords: number; // pagination.totalRows => incorrect (not including paths) / use pagination.rows.length (hack)
  public totalPages: number; // pagination.pageCount
  public pagerObj: IPageLink[];

  constructor(
    private pathService: PathService) {
  }


  ngOnChanges(simpleChanges: SimpleChanges) {
    if (simpleChanges.pathIdentifier) {
      this.baseUrl = this.pathService.getBaseUrl(this.pathIdentifier);
    }

    if (simpleChanges.pagination) {
      this.pageNumber = this.pagination.pageIndex + 1;
      this.pageSize = this.pagination.pageSize;
      this.totalRecords = this.pagination.totalRows + 3; // TEMPORARY - not counting paths need a backend fix ***** FIX THIS
      this.totalPages = this.pagination.pageCount;
      this.pagerObj = this.buildPagerObj();
    }

    if(simpleChanges.pageFiltersParams){
      Object.assign(this, this.pageFiltersParams);
    }
  }


  // Description: Build the pager array to loop through
  buildPagerObj(): IPageLink[] {
    let pageLinkArr: IPageLink[] = [];
    // const baseUrl = '/manager/Folder:Defendant:17965176/';
    const d = (this.pageNumber % 5),
      diff = d > 0 ? 5 - d : 0;
    let max = this.pageNumber + diff;
    let min = max - 4;
    (max > this.totalPages) && (max = this.totalPages);
    for (let i = min; i <= max; i++) { // for (let i = 1; i <= this.totalPages; i++) {
      const pagelink: IPageLink = {
        pageNumber: i,
        pagerUrl: this.baseUrl,
        params: { pageSize: this.pageSize, pageIndex: (i - 1) },   // this.baseUrl +'?pageSize=' + this.pageSize +'&pageIndex=' + (i - 1),
        linkClass: (i === this.pageNumber ? 'active' : '')
      };
      pageLinkArr.push(pagelink);

    }

    this.prevParams =  { pageSize: this.pageSize, pageIndex: (this.pageIndex- 1) };
    this.nextParams =  { pageSize: this.pageSize, pageIndex: this.pageNumber };
    //  console.log(this.pagerObj);
    return pageLinkArr;
  }

  // Description: Display pagination? check if pagination is not undefined
  isPaginationVisible() {
    return (this.pagination && this.totalPages > 1); // change to 1 (do not show pager when there is only 1 page)
  }

}
