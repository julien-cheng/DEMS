import { Injectable } from '@angular/core';
import { Router, Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { IFolderIdentifier, IPathIdentifier, IFileIdentifier } from '../index';

@Injectable()
export class MediaResolverService {

  constructor(
    private router: Router,

    //public explorerService: ExplorerService
  ) { }

  // Description: Resolve different path id's - need to refined this to pass ALL filtering params
  resolve(route: ActivatedRouteSnapshot): any {
    const organizationKey = route.params['organizationKey'],
      folderKey = route.params['folderKey'],
      pathKey = (route.params['pathKey'] !== undefined && route.params['pathKey'].length) ?route.params['pathKey'] : '',
      fileKey =route.params['fileKey'];
      
    if (!!folderKey && !!organizationKey) {
      const folderIdentifier: IFolderIdentifier = {
        organizationKey: organizationKey,
        folderKey: folderKey,
      },
        pathIdentifier: IPathIdentifier = {
          organizationKey: organizationKey,
          folderKey: folderKey,
          pathKey: pathKey
        },
        fileIdentifier: IFileIdentifier = {
          organizationKey: organizationKey,
          folderKey: folderKey,
          fileKey: fileKey
        };

      return {
        folderIdentifier: folderIdentifier,
        pathIdentifier: pathIdentifier,
        fileIdentifier: fileIdentifier
      };
    }
    else {
      console.error('Cant get folderKey');
      this.router.navigate(['/Error']);
    }
  }

}