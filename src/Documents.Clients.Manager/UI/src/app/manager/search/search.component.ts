import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import {
  IFolderIdentifier,
  IPathIdentifier,
  IManager,
  ExplorerService,
  SearchService,
  PathService,
  IPagination,
  LoadingService,
  SearchFormComponent,
  ISearch
} from '../index';

@Component({
  selector: 'app-search',
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.scss']
})
export class SearchComponent implements OnInit, OnDestroy {
  @ViewChild(SearchFormComponent)
  private searchFormComponent: SearchFormComponent;
  private manager: IManager;
  public searchQuery: string;
  public folderIdentifier: IFolderIdentifier;
  public pathIdentifier: IPathIdentifier;
  public searchResult: ISearch;

  // Paging:
  public pagination: IPagination;
  public pageFiltersParams: any;
  private pageFilterSubs: any;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private pathService: PathService,
    private searchService: SearchService,
    public explorerService: ExplorerService,
    public loadingService: LoadingService
  ) {}

  ngOnInit() {
    this.route.data.forEach(data => {
      this.pathIdentifier = this.route.snapshot.data.pathIdentifier;
      this.folderIdentifier = {
        organizationKey: this.pathIdentifier.organizationKey,
        folderKey: this.pathIdentifier.folderKey
      };
      this.searchQuery = this.route.snapshot.params.query;

      if (!this.explorerService.fileExplorer.currentExplorer) {
        this.getExplorer();
      }

      if (!!this.searchQuery) {
        this.pageFilterSubs = this.route.queryParams.subscribe(params => {
          this.pageFiltersParams = params;
          return this.getSearchResults();
        });
      }
    });
  }

  // Description: get search results
  private getSearchResults(): void {
    const self = this;
    // this.searchService.getSearchResults(this.searchQuery, this.folderIdentifier, this.pageFiltersParams).subscribe(
    //   (response) => {  // get the result object
    //     this.searchResult = response.response;

    //   },
    //   (error) => {
    //     console.error(error);
    //   },
    //   () => {
    //     this.loadingService.setLoading(false);
    //     !!this.searchFormComponent && this.searchFormComponent.resetForm();
    //   }
    // );
  }

  // Description: updates explorer and main page
  getExplorer(): void {
    const self = this;
    if (!!this.pathIdentifier) {
      // Needs to be reworked with new Identifiers
      this.pathService.getPathPage(this.pathIdentifier).subscribe(
        response => {
          self.manager = response.response as IManager;
          if (self.manager !== undefined) {
            this.explorerService.setCurrentExplorer(this.manager, this.pathIdentifier);
          }
        },
        error => {
          throw new Error('Manager is undefined - redirect to error');
        }
      );
    } else {
      console.error("Manager's required Keys are undefined - redirect to error");
    }
  }

  // Description: destroy
  ngOnDestroy() {
    // this.pageFilterSubs.unsubscribe();
  }
}
