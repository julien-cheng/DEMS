import { Injectable } from '@angular/core';
// import { Headers, Http, RequestOptions, Response } from '@angular/http';
import 'axios';
import Axios, { AxiosResponse, AxiosRequestConfig } from 'axios';

export class FolderService {
  constructor() {}
  // From File API
  // ----------------------------------------------------------------------
  // Description: Gets a All folder objects
  getAllFolders(): Promise<AxiosResponse<any>> {
    const url = '/api/folder/all';
    return Axios.get(url); //.catch(this.handleError);
  }

  /**
   * @param pathKey
   * @param folderKey
   * Description: Save New Case Folder
   * Returns a batchResponse observable object
   */
  createNewCase(newFolder: any) {
    // Working on this
    let headers = { 'Content-type': 'application/json' },
      options = { headers: headers },
      url = '/api/folder';
    return Axios.put(url, newFolder, options);
  }
  deleteCase(dyingFolder: any) {
    // Working on this
    let url = '/api/folder/';
    let headers = { 'Content-type': 'application/json' },
      options = { headers: headers, body: JSON.stringify(dyingFolder) };
    return Axios.delete(url, options);
  }
  saveFolderData(folderData: any) {
    let headers = { 'Content-type': 'application/json' },
      options = { headers: headers },
      url = '/api/folder/saveSchemaData';
    return Axios.post(url, folderData, options);
  }

  // Description: Error handling - TBD
  handleError(error: AxiosResponse) {
    return console.error(error);
  }
}
