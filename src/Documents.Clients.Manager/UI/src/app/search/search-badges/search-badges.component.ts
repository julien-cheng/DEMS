import { Component, Input, Output, EventEmitter, SimpleChanges, OnChanges } from '@angular/core';
import { ISearchRequest } from '..';

@Component({
  selector: 'app-search-badges',
  templateUrl: './search-badges.component.html',
  styleUrls: ['./search-badges.component.scss']
})
export class SearchBadgesComponent implements OnChanges {
  @Output() removeActiveFilter = new EventEmitter();
  @Input() searchRequest: ISearchRequest;
  public showFilters = true;

  constructor() {}

  ngOnChanges(simpleChanges: SimpleChanges) {
    this.showFilters =
      this.searchRequest.filters.filter(f => {
        return f.name !== 'organizationKey' && f.name !== 'folderKey';
      }).length > 0;
  }

  isIdentifier(filterName: string) {
    return filterName === 'organizationKey' || filterName === 'folderKey';
  }
}
