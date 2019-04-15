import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { IFolderIdentifier, IPathIdentifier, IBatchResponse, ISearchRequest, ISearchFilter } from '../index';
import * as _ from 'lodash';
const { isObject } = _;

@Injectable()
export class SearchService {
  constructor(private http: HttpClient) {}

  // Description: get the Search results
  getSearchResults(searchRequest: ISearchRequest): Observable<IBatchResponse> {
    const url = '/api/search?';
    let httpParams = new HttpParams();
    // Assemble filters
    for (const key in searchRequest) {
      const val = searchRequest[key];
      if (key === 'filters') {
        val.forEach((obj, i) => {
          for (const subkey in obj) {
            if (obj.hasOwnProperty(subkey)) {
              httpParams = httpParams.append(`searchRequest.${key}[${i}].${subkey}`, obj[subkey]);
            }
          }
        });
      } else if (key === 'paging') {
        for (const subkey in val) {
          if (val.hasOwnProperty(subkey)) {
            httpParams = httpParams.append(`searchRequest.${key}.${subkey}`, val[subkey]);
          }
        }
      } else {
        httpParams = httpParams.append(`searchRequest.${key}`, searchRequest[key]);
      }
    }

    return this.http.get<IBatchResponse>('/api/search?', { params: httpParams }).pipe(catchError(this.handleError));
  }

  // Description: build the ISearchFilter object from Querystring params
  public buildFilterObject(filterObj: any): ISearchFilter[] {
    let filters: ISearchFilter[] = [];
    if (filterObj.organizationKey) {
      filters.push({
        name: 'organizationKey',
        value: filterObj.organizationKey
      });
    }
    if (filterObj.folderKey) {
      filters.push({ name: 'folderKey', value: filterObj.folderKey });
    }
    if (filterObj.pathKey) {
      filters.push({ name: 'pathKey', value: filterObj.pathKey });
    } // replace with _path
    const additionalFiltersArr = this.decodeAdditionalFilterObject(filterObj.additionalFilters); //  [], subkey, index;
    filters = filters.concat(additionalFiltersArr);
    return filters;
  }

  // Description: Convert QS to Filter object
  decodeAdditionalFilterObject(additionalFilters: any) {
    const additionalFiltersArr = [];
    let subkey;
    let index;
    for (const key in additionalFilters) {
      if (additionalFilters.hasOwnProperty(key)) {
        index = key.split('.')[0];
        subkey = key.split('.')[1];
        index = index.substring(index.indexOf('[') + 1, index.indexOf(']'));
        if (!isNaN(index)) {
          let obj = {};
          obj[subkey] = additionalFilters[key];
          if (additionalFiltersArr[index]) {
            obj = Object.assign({}, obj, additionalFiltersArr[index]);
          }
          additionalFiltersArr[index] = obj;
        }
      }
    }
    return additionalFiltersArr;
  }

  // Description: Convert Filter object to QS
  encodeAdditionalFilterObject(filterObj: ISearchFilter[], i = 0): any {
    const additionalFiltersQS = {};
    const filterObject: any = {};
    filterObj.forEach(filter => {
      if (filter.name !== 'organizationKey' && filter.name !== 'folderKey' && filter.name !== 'pathKey') {
        // console.log(filter);
        for (const key in filter) {
          if (filter.hasOwnProperty(key)) {
            additionalFiltersQS[`filters[${i}].${key}`] = filter[key];
          }
        }
        i++;
      } else {
        filterObject[filter.name] = filter.value;
      }
    });

    filterObject.additionalFilters = additionalFiltersQS;
    filterObject.index = i;
    return filterObject;
  }

  handleError(error: Response) {
    console.error(error);
    return throwError(error.statusText);
  }
}
