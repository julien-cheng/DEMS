import { Component, Input, SimpleChanges, Output, EventEmitter, OnChanges } from '@angular/core';
import { ISearchRequest, ISearchFilter, IFacetGroup, IFacet, SearchService, IPathIdentifier } from '..';
import * as _ from 'lodash';
const { isEqual } = _;

@Component({
  selector: 'app-search-filters',
  templateUrl: './search-filters.component.html',
  styleUrls: ['./search-filters.component.scss']
})
export class SearchFiltersComponent implements OnChanges {
  @Output() removeActiveFilter = new EventEmitter();
  @Input() facetGroups: IFacetGroup[];
  @Input() searchRequest: ISearchRequest;
  @Input() identifier: IPathIdentifier;
  @Input() activeFilters: ISearchFilter[];
  @Input() baseURL: string;

  public presetParams: any;

  constructor(public searchService: SearchService) {}

  ngOnChanges(simpleChanges: SimpleChanges) {
    if (!!simpleChanges.searchRequest) {
      !!this.searchRequest.filters && (this.presetParams = this.searchService.encodeAdditionalFilterObject(this.searchRequest.filters));
    }

    if (!!simpleChanges.facetGroups) {
      this.facetGroups.every(facetGroup => {
        return facetGroup.values.every(facet => {
          return (facet.additionalParams = this.buildQueryString(facetGroup.name, facet));
        });
      });
    }
  }

  buildQueryString(name: string, facet: IFacet) {
    const activeFilter = {
      name,
      value: facet.value,
      label: facet.label
    };
    facet.filter = activeFilter;
    const addFilter: any = this.searchService.encodeAdditionalFilterObject([activeFilter], this.presetParams.index);
    const newAdditionalFilters =
      !!addFilter && !!addFilter.additionalFilters && Object.assign({}, this.presetParams.additionalFilters, addFilter.additionalFilters);
    newAdditionalFilters.keyword = this.searchRequest.keyword;
    newAdditionalFilters.disableReturn = this.searchRequest.disableReturn;
    return newAdditionalFilters;
  }

  isActiveFilter(facet) {
    return (
      this.activeFilters.filter(filter => {
        return _.isEqual(filter, facet.filter);
      }).length > 0
    );
  }
}
