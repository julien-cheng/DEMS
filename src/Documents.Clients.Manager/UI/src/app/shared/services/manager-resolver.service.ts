import { Injectable } from '@angular/core';
import { Router, Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { ExplorerService } from './explorer.service';
import { IPathIdentifier, IAutodownloadKeys } from '../index';

@Injectable()
export class ManagerResolver implements Resolve<any> {
  constructor(private router: Router, public explorerService: ExplorerService) {}

  // Description: Resolve different path id's - need to refined this to pass ALL filtering params
  resolve(route: ActivatedRouteSnapshot): any {
    const organizationKey = route.params.organizationKey;
    const folderKey = route.params.folderKey;
    if (!!folderKey && !!organizationKey) {
      const pathIdentifier: IPathIdentifier = {
        organizationKey,
        folderKey,
        pathKey: route.params.pathKey !== undefined && route.params.pathKey.length ? route.params.pathKey : ''
      };
      // this.explorerService.fileExplorer.folderKey = folderKey;
      this.explorerService.fileExplorer.pathIdentifier = pathIdentifier;
      // Get and set the autodownload string
      const autodownloadKeys: IAutodownloadKeys = {
        autodownloadString: route.queryParams.autodownload || null
      };

      return {
        pathIdentifier,
        autodownloadKeys
      };
    } else {
      console.error('Cant get folderKey');
      this.router.navigate(['/Error']);
    }
  }
}
