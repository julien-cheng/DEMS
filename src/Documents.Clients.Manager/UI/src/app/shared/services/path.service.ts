import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { IPathIdentifier, IPath, IManager, IBatchResponse, IFolderIdentifier } from '../index';
import { QuerystringPipe } from '../pipes/querystring.pipe';

@Injectable()
export class PathService {
    constructor(
        private http: HttpClient,
        private querystringPipe: QuerystringPipe) {

    }


    /**
     * @param pageIndex
     * @param pageSize
     * @param sortField
     * @param sortAscending
     * @param pathKey
     * @param folderKey
     * @param type
     */
    // Description: Gets a page of paths - pass filters
    // public pathItemQuery (pageIndex?: number, pageSize?: number, sortField?: string, sortAscending?: boolean, pathKey?: string, folderKey?: string, type?: string, extraHttpRequestParams?: any ) : Observable<models.APIResponseOfItemQueryResponse> {
    getPathPage(pathIdentifier: IPathIdentifier, params: any = {}): Observable<IBatchResponse> {
        // console.log('getPathPage - folderKey: ' + folderKey + ' - pathID: ' + pathId + ' - params: '); console.log(params);
        // New new build url querystring global method
        let organizationKey = pathIdentifier.organizationKey,
            folderKey = pathIdentifier.folderKey,
            pathKey = pathIdentifier.pathKey;
            
        if (!!organizationKey && !!folderKey) {
            let url = ('api/path/itemquery?' + 'pathIdentifier.organizationKey=' + organizationKey + '&pathIdentifier.folderKey=' + folderKey + '&pathIdentifier.pathKey=' + encodeURIComponent(pathKey) || '');
            
            // Build in the params if there are any:console.log("Build in the params if there are any:");
            // Example: url = url + '&pageSize=3&pageIndex=0';
            (Object.getOwnPropertyNames(params).length) &&
                (url = url + '&' + this.querystringPipe.transform(params));

            return this.http.get<IBatchResponse>(url).catch(this.handleError);
        }
    }

    

    /**
     * @param pathKey
     * @param folderKey
     * @param type (optional)
     * Description: Suggested Path Name for New Folders
     * Returns a batchResponse observable object
     */
    public pathSuggest(pathIdentifier: IPathIdentifier): Observable<IBatchResponse> {
        let url = '/api/path/suggest?organizationKey=' + pathIdentifier.organizationKey + '&folderKey=' + pathIdentifier.folderKey + '&pathKey=' + pathIdentifier.pathKey;
        return this.http.get<IBatchResponse>(url).catch(this.handleError);
    }

    /**
     * @param pathKey
     * @param folderKey
     * Description: Save New Path
     * Returns a batchResponse observable object
     */
    public createPath(newName: string, pathIdentifier: IPathIdentifier) {
        // console.log(newName +' - folderKey: '+ folderKey+' - pathKey: '+ pathKey)
        let headers = new HttpHeaders({ 'Content-type': 'application/json' }),
            options = { headers: headers },// new RequestOptions({ headers }),
            url = '/api/path/child',
            indata = {
                pathIdentifier: pathIdentifier,
                name: newName,
                type: 'NewPathRequest'
            };
        return this.http.put<IBatchResponse>(url, JSON.stringify(indata), options).catch(this.handleError);
    }
 
    // Description: Move File or Files to a new Path to new target path) -
    movePath(folderKey: string, sourcePaths: string, destinationPath: string) {
        let headers = new HttpHeaders({ 'Content-type': 'application/json' }),
            options = { headers: headers }; // new RequestOptions({ headers }),
        const indata = {
            folderKey: folderKey,
            sourcePaths: [sourcePaths],
            destinationPath: destinationPath
        };
        return this.http.post('api/path/move', indata, options).catch(this.handleError).subscribe();
    }

    // Description: gets the baseURL for manager list
    getBaseUrl(pathIdentifier: IPathIdentifier) {
        return ('/manager/' + pathIdentifier.organizationKey + '/' + pathIdentifier.folderKey + '/' + pathIdentifier.pathKey);
    }
    // Error handling - TBD
    handleError(error: Response) {
        console.error(error);
        return Observable.throw(error.statusText);
    }

}
