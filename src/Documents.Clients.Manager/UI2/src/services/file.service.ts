import { BatchRequest } from '../models/batch-request.model';
import { ExplorerService } from './explorer.service';
import 'axios';
import Axios, { AxiosResponse, AxiosRequestConfig } from 'axios';
import { IFileIdentifier, IPathIdentifier } from '@/models/identifiers.model';
import { IFile } from '@/models/file.model';
import { IView, IGridView, IRow } from '@/models/view.model';
import * as _ from 'lodash';
const { isEqual } = _;
// import { IFileIdentifier, IPathIdentifier, IFile, IPath, IRow, IManager, IBatchResponse, IGridView, IView, FileSetTypes, IMediaSegment, IMediaSet } from '../index';

export class FileService {
  constructor(public explorerService: ExplorerService) {}

  // From File API
  // ----------------------------------------------------------------------
  // Description: Gets a All file object from a parent IPath
  //fileGet(folderKey: string, fileKey: string): Observable<IBatchResponse> {
  fileGet(
    fileIdentifier: IFileIdentifier,
    pathIdentifier: IPathIdentifier,
  ): Promise<AxiosResponse<any>> {
    const url =
      '/api/file?organizationKey=' +
      fileIdentifier.organizationKey +
      '&folderKey=' +
      fileIdentifier.folderKey +
      '&pathKey=' +
      pathIdentifier.pathKey +
      '&fileKey=' +
      fileIdentifier.fileKey;
    if (
      !!fileIdentifier.organizationKey &&
      !!fileIdentifier.folderKey &&
      !!fileIdentifier.fileKey
    ) {
      this.explorerService.fileExplorer.fileIdentifier = fileIdentifier;
      this.explorerService.fileExplorer.pathIdentifier = pathIdentifier;
      return Axios.get(url);
    } else {
      return Promise.reject(new Error('File error in fileGet'));
    }
  }

  getFileMediaSet(
    fileIdentifier: IFileIdentifier,
    viewertype: string,
  ): Promise<AxiosResponse<any>> {
    const apiKey = this.getApiMediaKey(viewertype), // Needs to be dynamic : mediaset for video/audio, imageset for images, textset, documentset for documents and pdf
      url = `/api/views/${apiKey}?fileIdentifier.organizationKey=${fileIdentifier.organizationKey}&fileIdentifier.folderKey=${fileIdentifier.folderKey}&fileIdentifier.fileKey=${fileIdentifier.fileKey}`;
    if (
      !!fileIdentifier.organizationKey &&
      !!fileIdentifier.folderKey &&
      !!fileIdentifier.fileKey
    ) {
      return Axios.get(url);
    } else {
      return Promise.reject(new Error('Error in Get File Media Set'));
    }
  }

  // Description: map viewertype to api endpoint
  getApiMediaKey(viewertype: string): string {
    switch (viewertype) {
      case 'image':
        return 'imageset';
      case 'video':
      case 'audio':
        return 'mediaset';
      case 'text':
        return 'textset';
      case 'transcript':
        return 'transcriptset';
      case 'clip':
        return 'clipset';
      case 'unknown':
        return 'unknownset';
      default:
        return 'documentset';
    }
  }

  // Description: Get the DetailViewType: for the component views in manager-detail
  getDetailViewType(file: IFile): string {
    const detailViewType =
      file && file.viewerType !== 'none' && file.viewerType !== 'Unknown'
        ? file.viewerType
        : file && (file.viewerType === 'none' || file.viewerType === 'Unknown')
        ? this.getFileTypefromExt(file)
        : 'default';
    return detailViewType;
  }

  // Description: set detailViewType for files set to 'none' using file extension - build on this or remove ***** Working TBD *****
  getFileTypefromExt(file: IFile) {
    let extension = this.getFileExtension(file),
      detailViewType = 'none';
    switch (extension) {
      case 'mp3':
        detailViewType = 'audio';
        break;
      case 'jpg':
      case 'jpeg':
      case 'png':
      case 'gif':
        detailViewType = 'image';
        break;
      default:
        break;
    }
    return detailViewType;
  }

  // Description: retrive file extension
  getFileExtension(file: IFile) {
    return file.name
      .toLowerCase()
      .split('.')
      .pop();
  }

  getFileFromViews(views: IView[], fileIdentifier: any) {
    let aFile: IFile[] = [];
    views.forEach((view: IView) => {
      if ((<IGridView>view).rows) {
        aFile = <IFile[]>(<IGridView>view).rows.filter(row => {
          return _.isEqual(row.identifier, fileIdentifier);
        });
      }
    });
    //  this.getObjects(views, 'fileKey', fileKey);
    return aFile.length ? aFile[0] : null;
  }

  // Description: Extract a file object from IRows[]
  getFileFromPathTree(rows: IRow[], fileKey: string): IFile | null {
    const aFile: IFile[] = this.getObjects(rows, 'fileKey', fileKey);
    return aFile.length ? aFile[0] : null;
  }

  // Description: get a child object with the key value pair
  getObjects(obj: any, key: any, val: any): any {
    let objects: (any | never)[] = [];
    const _self = this;
    for (const i in obj) {
      if (!obj.hasOwnProperty(i)) {
        continue;
      }
      if (typeof obj[i] === 'object') {
        objects = objects.concat(_self.getObjects(obj[i], key, val));
      } else if (i === key && obj[key] === val) {
        objects.push(obj);
      }
    }
    return objects;
  }
}

// deleteFile(folderKey: string, fileKey: string) {
// const indata = {
//     fileKeys: [fileKey],
//     folderKey:
// };
// const headers = new Headers({ 'Content-type': 'application/json' });
// this.http.delete('/api/file', new RequestOptions({
//     headers: headers,
//     body: indata
// })).catch(this.handleError).subscribe(
//     response => {
//         return response.json();
//     });
// };

// Description: Save File - TBD
// saveFile(File): Observable<IFile> {
//     const headers = new HttpHeaders({ 'Content-type': 'application/json' });
//     // const options = new RequestOptions({ headers });
//     const options = {
//         headers: headers
//     };
//     return this.http.post<IFile>('/api/files', JSON.stringify(File), options)
//         // .map((response: Response) => {
//         //     return response.json();
//         // })
//         .catch(this.handleError);
// }

// Accessory methods
// ----------------------------------------------------------------------
// Description: Search for Files - NEEDS REWORK WIHT HTTPCLIENT
// searchFiles(searchTerm: string): Observable<any> {
//     // This will change for a search api call:
//     const url = '/api/Folder?folderKey=Defendant:14015529&search=' + searchTerm;
//     return this.http.get(url).catch(this.handleError);
//     // .map((response: Response) => {
//     //     const manager = <IManager>response.json();
//     //     // NEEDS CLEAN UP
//     //     let rows = manager.views.filter((viewObject) => {
//     //         return viewObject.type === 'Grid';
//     //     });
//     //     return (<IGridView>rows[0]).rows;
//     // })
//     // .catch(this.handleError);
// }
