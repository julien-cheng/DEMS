import { Injectable } from '@angular/core';
// import { Headers, Http, RequestOptions, Response } from '@angular/http';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { BatchRequest } from '../models/batch-request.model';
import { IBatchOperation, IBatchResponse } from '../index';

@Injectable()
export class FolderService {

  constructor(
    private http: HttpClient
  ) { }
  // From File API
  // ----------------------------------------------------------------------
  // Description: Gets a All folder objects
  getAllFolders(): Observable<IBatchResponse> {
    const url = '/api/folder/all';
    return this.http.get<IBatchResponse>(url)
      .catch(this.handleError);
  }

  /**
    * @param pathKey
    * @param folderKey
    * Description: Save New Case Folder
    * Returns a batchResponse observable object
    */
  createNewCase(newFolder: any) { // Working on this
    let headers = new HttpHeaders({ 'Content-type': 'application/json' }),
      options = { headers: headers },
      url = '/api/folder';
    return this.http.put<IBatchResponse>(url, JSON.stringify(newFolder), options)
      .catch(this.handleError);
  }

  saveFolderData(folderData: any) {
    let headers = new HttpHeaders({ 'Content-type': 'application/json' }),
      options = { headers: headers },
      url = '/api/folder/saveSchemaData';
    return this.http.post<IBatchResponse>(url, JSON.stringify(folderData), options)
      .catch(this.handleError);
  }


  // Description: Error handling - TBD
  handleError(error: Response) {
    return Observable.throw(error.statusText);
  }
}
