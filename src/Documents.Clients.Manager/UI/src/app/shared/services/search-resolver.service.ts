import { Injectable } from '@angular/core';
import { Router, Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { SearchService } from './search.service';
import { LoadingService } from './loading.service';
import { IPathIdentifier, ISearchRequest } from '../index';
@Injectable()
export class SearchResolver implements Resolve<any> {

  constructor(
    private router: Router,
    public loadingService: LoadingService,
    public searchService: SearchService
  ) { }
  // Description: Resolve different path id's - need to refined this to pass ALL filtering params
  resolve(route: ActivatedRouteSnapshot): any {
    let obj= {
      organizationKey: route.params['organizationKey'],
      folderKey: route.params['folderKey'],
      pathKey: route.params['pathKey'],
      additionalFilters: route.queryParams,
    };
   
    return obj;
  }
}
