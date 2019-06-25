import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { IManager, IFileIdentifier, IPathIdentifier } from '../index';
import { ExplorerService, IExplorer } from './explorer.service';
import { LoadingService } from './loading.service';
import { PathService } from './path.service';
import { FileService } from './file.service';


@Injectable()
export class FileResolver implements Resolve<any> {
    constructor(private fileService: FileService,
        private pathService: PathService,
        public loadingService: LoadingService,
        private explorerService: ExplorerService) {
    }

    resolve(route: ActivatedRouteSnapshot) {
        this.loadingService.setLoading(true);
        let viewerType = route.params['viewerType'] !== undefined ? route.params['viewerType'].toLowerCase() : 'default',
            fileIdentifier: IFileIdentifier = {
                organizationKey: route.params['organizationKey'],
                folderKey: route.params['folderKey'],
                fileKey: route.params['fileKey'] !== undefined ? route.params['fileKey'] : null,
            },
            pathIdentifier: IPathIdentifier = {
                organizationKey: route.params['organizationKey'],
                folderKey: route.params['folderKey'],
                pathKey: route.params['pathKey'] || ""
            };

        return {
            fileIdentifier: fileIdentifier,
            pathIdentifier: pathIdentifier,
            viewerType: viewerType
        }
    }

}
