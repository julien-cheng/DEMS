import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import {
  IOrganizationIdentifier,
  IFolderIdentifier,
  IPathIdentifier,
  SearchService,
  IPagination,
  LoadingService,
  SearchFormComponent,
  ISearch,
  IFacetGroup,
  IFacet,
  ISearchRequest,
  ISearchPagination,
  ISearchFilter
} from '../index';
import * as _ from 'lodash';
const { isEqual } = _;

@Component({
  selector: 'app-search',
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.scss']
})
export class SearchComponent implements OnInit {
  public organizationIdentifier: IOrganizationIdentifier;
  public folderIdentifier: IFolderIdentifier;
  public pathIdentifier: IPathIdentifier;
  public keyword: string;
  public isReturnLinkDisabled = false;
  public searchResult: ISearch;
  public facetGroups: IFacetGroup[];
  public filters: Array<any> = [];
  public searchRequest: ISearchRequest;
  public activeFilters: ISearchFilter[] = [];
  public baseURL: string;
  public returnLink;

  // Paging:
  public pagination: ISearchPagination = {
    pageIndex: 0,
    pageSize: 10,
    sortfield: 'name',
    isAscending: true
  };

  public pageSize: number = this.pagination.pageSize;
  public pageFiltersParams: any;
  private pageFilterSubs: any;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private searchService: SearchService,
    public loadingService: LoadingService
  ) {}

  ngOnInit() {
    this.loadingService.setLoading(false);
    this.route.data.forEach(data => {
      this.setIdentifiers(this.route.snapshot.data.searchData);
      // this.keyword = this.route.snapshot.data['searchData'].keyword;
      this.pageFilterSubs = this.route.queryParams.subscribe(params => {
        this.pageFiltersParams = params;
        !!this.pageFiltersParams.disableReturn ? this.setReturnLink(this.pageFiltersParams.disableReturn) : this.setReturnLink(false);
        this.buildSearchRequest(this.route.snapshot.data.searchData, this.pageFiltersParams);
        return this.getSearchResults();
      });
    });
  }

  private setIdentifiers(searchData) {
    this.pathIdentifier = {
      organizationKey: searchData.organizationKey,
      folderKey: searchData.folderKey,
      pathKey: searchData.pathKey
    };

    this.folderIdentifier = {
      organizationKey: this.pathIdentifier.organizationKey,
      folderKey: this.pathIdentifier.folderKey
    };

    this.baseURL = '/search/' + searchData.organizationKey;
    !!searchData.folderKey && (this.baseURL += '/' + searchData.folderKey);
    !!searchData.pathKey && (this.baseURL += '/' + searchData.pathKey);
    // !!searchData.keyword && (this.baseURL += '/' + searchData.keyword);
  }

  // Description: set Return Link - disable if disableReturn = true
  private setReturnLink(isReturnLinkDisabled: any) {
    this.isReturnLinkDisabled = JSON.parse(isReturnLinkDisabled) || false; // Cast QS string to bool using  JSON.parse or default to false
    if (!this.isReturnLinkDisabled) {
      if (!!this.pathIdentifier.folderKey) {
        this.returnLink = ['/manager/'];
        !!this.pathIdentifier.organizationKey && this.returnLink.push(this.pathIdentifier.organizationKey);
        !!this.pathIdentifier.folderKey && this.returnLink.push(this.pathIdentifier.folderKey);
        !!this.pathIdentifier.pathKey && this.returnLink.push(this.pathIdentifier.pathKey);
      } else {
        this.returnLink = ['/case-list/', this.pathIdentifier.organizationKey];
      }
    }
  }

  // Description: Build the search Request object
  private buildSearchRequest(searchData, queryParams?: any) {
    const filterObject = {
      organizationKey: searchData.organizationKey,
      folderKey: searchData.folderKey,
      pathKey: searchData.pathKey,
      additionalFilters: Object.assign({}, searchData.queryParams, queryParams)
    };
    this.keyword = queryParams.keyword;

    // Fix pagination
    this.pagination.pageIndex = !!queryParams.pageIndex ? queryParams.pageIndex : 0;
    this.pagination.pageSize = !!queryParams.pageSize ? (this.pagination.pageSize = queryParams.pageSize) : 10;

    if (!!filterObject.organizationKey) {
      this.searchRequest = {
        filters: this.searchService.buildFilterObject(filterObject),
        keyword: this.keyword || '',
        paging: this.pagination,
        disableReturn: this.isReturnLinkDisabled
      };
      this.activeFilters = this.searchRequest.filters;
    } else {
      console.error('Cant get organizationKey');
      this.router.navigate(['/Error']);
    }
  }

  // Description: get search results
  private getSearchResults(): void {
    const self = this;
    this.loadingService.setLoading(true);
    this.searchService.getSearchResults(this.searchRequest).subscribe(
      response => {
        // get the result object
        this.searchResult = response.response;
        this.facetGroups = this.searchResult.facets;
        this.pageSize = this.searchResult.rows.length;
      },
      error => {
        console.error(error);
      },
      () => {
        this.loadingService.setLoading(false);
      }
    );
  }

  public removeActiveFilter(filter: ISearchFilter) {
    if (filter.name === 'pathKey') {
      this.router.navigate(['/search', this.folderIdentifier.organizationKey, this.folderIdentifier.folderKey], {
        queryParams: {
          keyword: this.keyword,
          disableReturn: this.searchRequest.disableReturn
        }
      });
    } else {
      const filters = this.searchRequest.filters.slice();
      filters.forEach((f, i) => {
        _.isEqual(filter, f) && filters.splice(i, 1);
      });
      const newQS = Object.assign({}, this.searchService.encodeAdditionalFilterObject(filters).additionalFilters, {
        keyword: this.keyword,
        disableReturn: this.searchRequest.disableReturn
      });
      this.router.navigate([this.baseURL], { queryParams: newQS });
    }
  }

  // Description: refresh result set with same keyword
  public refreshResults(event: any) {
    this.getSearchResults(); // Right now is only refreshing the current page including the filters
  }
}
