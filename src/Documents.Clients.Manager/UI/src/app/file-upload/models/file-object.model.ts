import { HttpHeaders } from '@angular/common/http';
import { Subscription } from 'rxjs/Subscription';
import { FileUpload, IFileUploadOptions } from './file-upload.model';

// Pass more data with this interface - extends File (name, size, type, lastModifiedDate, lastModified, webkitRelativePath)
export interface IUploadedFile extends File {
  fullPathName?: string; // full path + file name or webkitRelativePath when available
  lastModified: number;
}

export interface IfileObjectStatus {
  isReady: boolean;
  isPaused: boolean;
  isUploading: boolean;
  isUploaded: boolean;
  isSuccess: boolean;
  isCancel: boolean;
  isError: boolean;
  isRetryExpired: boolean; // If the retries >== maxNumberRetries set to true
}

export interface IFileObjectOptions extends IFileUploadOptions {
  isChunkedUpload: boolean;
  multipart: boolean;
  retries: number;
  // Individual fileobject options
  additionalParameter?: { [key: string]: any }; // exposed to add additional params to call
  blob?: Blob;
  chunkSize?: number;
  contentRange?: string;
  data?: any; // FormData || File
  headers?: HttpHeaders;
  paramName?: string; // Defaults to - ["files[]"],
  withCredentials?: boolean;
}

export class FileObject {
  public _file: IUploadedFile;
  public file: File;
  public progress = 0;
  public fileObjectStatus: IfileObjectStatus;
  public options: IFileObjectOptions;
  public _uploadProcess: Subscription;
  public message: Array<string> = [];
  public errorMessage: Array<string> = [];

  public constructor(Ifile: IUploadedFile, options: IFileUploadOptions) {
    this.file = Ifile as File;
    this._setFileObjectOptions(options, Ifile);
    this.setFileObjectStatus();
  }

  public onProgress(progress: number): any {
    return { progress };
  }

  public _onProgress(progress: number): void {
    this.progress = progress;
    this.onProgress(progress);
  }

  public _onSuccess(response: string, status: number, headers: HttpHeaders): void {
    this.setFileObjectStatus({ isSuccess: true, isUploaded: true });
    this.setFileObjectMessaging('File: ' + this.file.name + ' was uploaded successfully!', false, true);
  }

  public _onError(response: string, status: number, headers: any): void {
    this.setFileObjectStatus({ isError: true });
    this.setFileObjectMessaging('There was an error with ' + this.file.name + ': This file was not uploaded!', true, true);
  }

  public _onCancel(): void {
    this.setFileObjectStatus({ isCancel: true });
    this.setFileObjectMessaging('Upload of File: ' + this.file.name + ' was canceled.', true);
  }

  public _onComplete(response: string, status: number, headers: HttpHeaders): void {
    // To be compeleted
  }

  public _increaseRetry(): number {
    // console.log('_increaseRetry: ', this.options.retries);
    this.options.retries++;
    return this.options.retries;
  }

  /// Description pass it an object to override the state of the FileObject
  // pass resetAllStatus=false if no reset is required but override the object directly as is (keep current values)
  public setFileObjectStatus(state?: any, resetAllStatus: boolean = true): void {
    const reset = {
      isPaused: false,
      isReady: false,
      isUploading: false,
      isUploaded: false,
      isSuccess: false,
      isCancel: false,
      isError: false,
      isRetryExpired: false
    };

    this.fileObjectStatus = resetAllStatus ? Object.assign(reset, state) : Object.assign(this.fileObjectStatus, state); // { ...stateObj, ...state };
  }

  public setFileObjectMessaging(message: string, isError: boolean, resetMessageArray: boolean = false): void {
    if (resetMessageArray) {
      if (isError) {
        this.errorMessage = [message];
      } else {
        this.message = [message];
      }
    } else {
      isError ? this.errorMessage.push(message) : this.message.push(message);
    }
  }

  protected _setFileObjectOptions(options: IFileUploadOptions, Ifile: IUploadedFile): void {
    this.options = Object.assign(
      {
        isChunkedUpload: options.enableGlobalChunkedUpload || this._isChunkedFileUpload(options),
        multipart: this._isMultipartFileUpload(options),
        retries: 0,
        additionalParameter: {
          fileInformation: {
            // Add all file params to the fileInformation object
            lastModified: Ifile.lastModified,
            lastModifiedDate: Ifile.lastModified,
            name: Ifile.name,
            size: Ifile.size,
            type: Ifile.type,
            fullPath: this._extractFullPath(Ifile.fullPathName) || '',
            webkitRelativePath: Ifile.fullPathName
          }
        }
      },
      options
    );
  }

  protected _extractFullPath(fullPathName: string): string {
    const index = fullPathName.lastIndexOf('/');
    const fullPath = index > 0 ? fullPathName.substr(0, index) : '/';
    return fullPath;
  }

  // Description: Chunked File Uploads
  // Flags: maxChunkSize, multipart
  // ------------------------------------------------------------------------------------------------------
  // To upload large files in smaller chunks, set the following option: multipart: true
  // If set to 0, null or undefined, or the browser does not support the required Blob API, files will be uploaded as a whole.
  // to a preferred maximum chunk size: maxChunkSize && Is maxChunkSize larger or equal to fileSize?
  protected _isChunkedFileUpload(options: IFileUploadOptions): boolean {
    return !(!options.maxChunkSize || options.maxChunkSize >= this.file.size);
  }
  // returns whether this is a multipart upload - by browser lack of support or manual disabling (disableMultipart set to true)
  protected _isMultipartFileUpload(options: IFileUploadOptions): boolean {
    return !options.disableMultipart && options.isHTML5;
  }
}
