import { Component, Input, EventEmitter, Output, SimpleChanges } from '@angular/core';

export interface IMediaPagination {
  pageIndex: number; // pageNumber -1
  pageSize: number;
  pagerCount: number;
  totalItems: number;
  isLastPage: boolean;
}


@Component({
  selector: 'app-media-pager',
  templateUrl: './pager.component.html',
  styleUrls: ['./pager.component.scss']
})
export class PagerComponent implements IMediaPagination {
  @Output() onPageChanged = new EventEmitter<number>();
  @Input() public pageNumber: number;
  @Input() public pageSize: number; // How many items per page 
  @Input() public items: any[]; // SearchResult Items array
  pageIndex: number; // pageNumber - 1
  pagerCount: number = 5; //Number of pager links per segments = between the << and >> 
  totalItems: number; // Total number of items
  totalPages: number; // Total Number of pages => total items / pageSize
  isLastPage: boolean = false;
  public pagerObj: Array<any> = [];

  constructor() { }

  ngOnChanges(simpleChanges: SimpleChanges) {
    if (!!simpleChanges.items) {
      this.totalItems = this.items.length;
      this.totalPages = Math.ceil(this.totalItems / this.pageSize);
    }

    (!!simpleChanges.pageNumber) && (this.pageIndex = Number(this.pageNumber - 1));
    this._buildPagerObj();
  }

  private _buildPagerObj() {
    this.pagerObj = [];
    const d = (this.pageNumber % 5),
      diff = d > 0 ? 5 - d : 0;
    let max = this.pageIndex + diff;
    let min = max - 4;
    (max >= this.totalPages) && (max = this.totalPages - 1);
    for (let i = min; i <= max; i++) {
      this.pagerObj.push(i + 1);
    }
  }

  public numberOfPages() {
    if (this.items) {
      return Math.ceil(this.items.length / this.pageSize);
    }
    return 0;
  };

  public goToPage(pageNumber: number) {
    this.onPageChanged.emit(pageNumber);
  }

}
