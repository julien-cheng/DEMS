import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { IFileUploadOptions } from '../../file-upload/index';
import { IBatchResponse } from '../index';

export interface IAppConfiguration extends IFileUploadOptions {
  type: string;
  isTopNavigationVisible?: boolean;
  isSearchEnabled?: boolean;
  userTimeZone?: string;
  caseCreate?: string;
}

@Injectable()
export class AppConfigService {
  public configuration: IAppConfiguration = {
    type: '',
    isTopNavigationVisible: false,
    isSearchEnabled: true,
    caseCreate: ''
  };

  constructor(private http: HttpClient, private router: Router) {}

  public setAPIConfiguration() {
    const url = '/api/configuration';
    return this.http.get<IBatchResponse>(url).subscribe(
      response => {
        this.configuration = response.response as IAppConfiguration;
      },
      error => {
        this.router.navigate(['/error']);
      },
      () => {}
    );
  }

  public setTopNavVisible(isVisible: boolean) {
    this.configuration.isTopNavigationVisible = isVisible;
  }
}
