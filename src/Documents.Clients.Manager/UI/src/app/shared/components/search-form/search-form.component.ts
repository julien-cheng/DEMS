import { Component, OnInit, Input, ViewChild, Output, EventEmitter } from '@angular/core';
import { Location } from '@angular/common';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService, Toast, ToastrConfig } from 'ngx-toastr';
import { IFolderIdentifier, IPathIdentifier, ISearchRequest } from '../../index';
import { AppConfigService } from '../../services/app-config.service';

@Component({
  selector: 'app-search-form',
  templateUrl: './search-form.component.html',
  styleUrls: ['./search-form.component.scss']
})
export class SearchFormComponent implements OnInit {
  @Output() refreshResults = new EventEmitter();
  @Input() identifier: IFolderIdentifier | IPathIdentifier;
  // @Input() searchResultTerm: string; // Only in search resulpage
  @Input() searchRequest: ISearchRequest;
  // @Input() isSearchPage: boolean=false;
  @ViewChild('searchForm') searchForm: NgForm;
  public searchTerm = '';
  public disableReturn: boolean;

  constructor(
    public appConfigService: AppConfigService,
    private router: Router,
    private toastr: ToastrService,
    private location: Location
  ) {}

  ngOnInit() {
    // Preset the keyword to the search box if in result page
    !!this.searchRequest && (this.searchTerm = this.searchRequest.keyword);
  }

  onSearch(searchTerm: string) {
    if (!!this.identifier) {
      if (!!this.searchRequest && this.searchRequest.keyword === searchTerm) {
        this.refreshResults.emit(searchTerm);
      } else {
        const arr = ['/search/', this.identifier.organizationKey];
        !!this.identifier.folderKey && arr.push(this.identifier.folderKey);
        !!(this.identifier as IPathIdentifier).pathKey && arr.push((this.identifier as IPathIdentifier).pathKey);
        this.router.navigate(arr, {
          queryParams: {
            keyword: searchTerm,
            disableReturn: !!this.searchRequest ? this.searchRequest.disableReturn : false
          }
        }); // Redirect to search results - with searchterm
      }
    } else {
      this.toastr.warning('Please, enter the a search term. If this message persists, contact the site administrators.');
    }
  }

  // Description: trigger form reset from parent views (@ViewChild(SearchFormComponent) private searchFormComponent: SearchFormComponent;)
  public resetForm() {
    this.searchForm.reset();
  }
}
