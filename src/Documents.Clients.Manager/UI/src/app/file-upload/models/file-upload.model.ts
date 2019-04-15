import { Injectable } from '@angular/core';
import {
  HttpClient,
  HttpHeaders,
  HttpRequest,
  HttpResponse,
  HttpErrorResponse,
  HttpEvent,
  HttpEventType,
  HttpParams
} from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { FileObject, IUploadedFile, IfileObjectStatus, IFileObjectOptions } from '../index';
import { FileSizePipe } from '../../shared/pipes/file-size.pipe';
import { FILEUPLOAD_EVENTS } from './events';
import * as _ from 'lodash';
const { isObject } = _;

export interface FilterFunction {
  name: string;
  errorMessage: string;
  fn: (file?: IUploadedFile, options?: IFileUploadOptions) => boolean;
}

export interface IUploadedItemEntry {
  name: string;
  isDirectory: boolean;
  isFile: boolean;
  fullPath?: string;
  filesystem?: any;
}

// Place UI params in this option object (changeable on init)
export interface IFileUploadOptions {
  allowedMimeType?: Array<string>;
  allowedFileType?: Array<string>;
  autoUpload?: boolean;
  dataType?: string; // 'json', 'Html', // Maynot be needed
  disableMultipart?: boolean;
  concurrentUploadLimit?: number;
  enableGlobalChunkedUpload?: boolean;
  isHTML5?: boolean;
  maxChunkSize?: number;
  maxFileSize?: number;
  maxNumberRetries?: number;
  method?: string;
  queueLimit?: number;
  removeAfterUpload?: boolean;
  reportProgress?: boolean;
  uploadedBytes?: number;
  url?: string;
  filters?: Array<FilterFunction>; // Pass filters to restrict file type, size and mimetype and other custom filters
}

@Injectable()
export class FileUpload {
    public authToken: string;
    public authTokenHeader: string;
    public isQueueUploading = false; // Labels the current state of the fileUpload (globally)
    public queue: Array<FileObject> = [];
    public failedQueue: Array<FileObject> = [];
    public progress = 0;
    public isIE11 = false;
    public options: IFileUploadOptions = {
        autoUpload: true, // change to true for autoupload
        dataType: 'json',
        disableMultipart: true, // Current API Expects to work with Multipart form data - option for future dev
        concurrentUploadLimit: 5, // Number of files that can be uploaded at the same time
        enableGlobalChunkedUpload: true, // Will always send files through chunked uploads transport
        isHTML5: true, // TO BE SET BY BROWSERS TYPE - option for future dev ----- NEED TO USE MODERNIZER FOR BROWER DETCTION OF XHR HTML5 and FileReaders ***** TBD
        maxChunkSize: 10485760, // 10 * 1024 * 1024 - bytes (5MB in binary)
        maxFileSize: 5368709120, // 5*1024*1024*1024 - - bytes (5GB in binary)  can set file size max in this options object
        method: 'POST',
        removeAfterUpload: true,
        queueLimit: 50000, // hard limit set
        filters: [],
        reportProgress: true,
        uploadedBytes: 0,
        maxNumberRetries: 3,
    };

    public constructor(
        private http: HttpClient,
        private fileSizePipe: FileSizePipe
    ) {
        this._setIsIE11Flag();
    }

    protected _setIsIE11Flag() {
        const msie = parseInt((/trident\/.*; rv:(\d+)/.exec(navigator.userAgent.toLowerCase()) || [])[1]); // parseInt((/msie (\d+)/.exec(navigator.userAgent.toLowerCase()) || [])[1]);
        !isNaN(msie) && (this.isIE11 = true); // Set IE11 flags
    }

    public setFileUploadData({ options = null, events = null }: { options: any, events: any }) {
        // Registers the options:
        if (options) {
            this._setFileUploadOptions(options);
        }

        // Registers the events exposed:
        if (events) {
            this.events = events;
        }
    }

    // Description: Exposed events: stored in events.s (FILEUPLOAD_EVENTS)
    private events: any;
    eventNames = Object.keys(FILEUPLOAD_EVENTS);
    public fireEvent(event: any) {
        try {
            this.events[event.eventName].emit(event); // this.events.event.emit(event);
        } catch (m) {
            console.error(m);
        }

    }

    // Description: Set up options and filters for this instance of the file upload
    protected _setFileUploadOptions(options: IFileUploadOptions): void {
        this.options = Object.assign(this.options, options);
        this.options.filters.unshift({ name: 'queueLimit', errorMessage: 'The number of files in queue exceed the maximum allowed.', fn: this._queueLimitFilter });

        if (this.options.maxFileSize) {
            const max = this.fileSizePipe.transform(this.options.maxFileSize, true);
            this.options.filters.unshift({ name: 'fileSize', errorMessage: 'File size exceeds the maximum file size allowed of ' + max, fn: this._fileSizeFilter });
        }

        if (this.options.allowedFileType) {
            this.options.filters.unshift({ name: 'fileType', errorMessage: 'This file type is not allowed.', fn: this._fileTypeFilter });
        }

        if (this.options.allowedMimeType) {
            this.options.filters.unshift({ name: 'mimeType', errorMessage: 'This file mime type is not allowed.', fn: this._mimeTypeFilter });
        }
    }

    // ------------------------------------------------------------------------------------------------------
    // 1) PREPARING FOR UPLOAD: File ADDING TO QUEUE and DATA INITIALIZATION
    // ------------------------------------------------------------------------------------------------------
    // Description: Uploading first steps - Sorting item types: File(s) vs directory upload
    // Handles directory structure and file path
    public prepareUploadQueueItems(dataTransferList: DataTransferItemList): Observable<any[]> {
        const _self = this;
        return new Observable(observer => {
            function traverseFileTree(item: any, fullPath?: string) {
                if (item.isFile) {
                    return item.file((file) => {
                        observer.next(_self.onBeforeItemsAddtoQueue([file], fullPath));
                    });
                } else {
                    const dirReader = item.createReader();
                    dirReader.readEntries(entries => {
                        for (let i = 0; i < entries.length; i++) {
                            traverseFileTree(entries[i], entries[i].fullPath);
                        }
                    });
                }
            }

            if (dataTransferList.length) {
                for (let i = 0; i < dataTransferList.length; i++) {
                    const item: DataTransferItem = dataTransferList[i];
                    const itemWebkitEntry = item.webkitGetAsEntry();
                    traverseFileTree(itemWebkitEntry, itemWebkitEntry.fullPath);
                }
            }
          });
        }
      

      

  // Description: Queue methods
  // -------------------------------------------------------
  // Bulk methods:
  public addToUploadQueue(files: IUploadedFile[], options?: IFileUploadOptions) {
    // console.log('Add File to Queue: ');
    // console.log(files);
    const list: IUploadedFile[] = [];
    for (const file of files) {
      list.push(file);
    }
    // console.log(list);
    const count = this.queue.length;
    list.map((file: IUploadedFile) => {
      if (!options) {
        options = this.options;
      }
      const fileObject = new FileObject(file, options); // Convert File to FileObject
      if (this._isValidFile(fileObject, options)) {
        fileObject.setFileObjectStatus({ isReady: true });
        this.queue.push(fileObject);
      } else {
        this.addToFailedQueue(fileObject);
        console.error('ERROR IN addToUploadQueue: invalid file!');
      }
    });
  }

  // Description: initialize AutoUpload if set to true (directives drop and select)
  public _initAutoUpload() {
    this._setPausedUploadedFiles();
    if (this.options.autoUpload) {
      this._uploadAllFiles();
    }
  }

  // Descriptios: trigger upload of all files in queue (user triggered) - restrict by concurrentUploadLimit
  public uploadAllFiles(): void {
    this.queue.forEach((fileObject: FileObject, index: number) => {
      fileObject.options.autoUpload = true;
    });
    this._uploadAllFiles();
  }

  // Description: Upload All Files
  public _uploadAllFiles(): void {
    this.queue.forEach((fileObject: FileObject, index: number) => {
      if (this._fileisReadyForUpload(fileObject)) {
        this.processFileforUpload(fileObject);
      }
    });
  }

  // Description: Cancels all ongoing uploads - Cancel All
  public cancelAllFiles(): void {
    const fileObjects = this._getNotUploadedFiles();
    fileObjects.map((fileObject: FileObject) => {
      // console.log(fileObject);
      this.cancelFile(fileObject);
    });
  }

  // Description: removes each and all FileObjects from Queue + cancels ongoing uploads
  // Clears ALL FileObjects in queue - or Remove ALL
  public clearUploadQueue(): void {
    while (this.queue.length) {
      this.removeFromUploadQueue(this.queue[0]);
    }
    this.progress = 0;
  }

  // Description: Returns status true if this is file ready to be uploaded
  public _fileisReadyForUpload(fileObject: FileObject): boolean {
    return (
      !fileObject.fileObjectStatus.isUploading &&
      !fileObject.fileObjectStatus.isUploaded &&
      !fileObject.fileObjectStatus.isPaused &&
      fileObject.fileObjectStatus.isReady
    );
  }

  public _getIndexOfItem(value: any): number {
    return typeof value === 'number' ? value : this.queue.indexOf(value);
  }

  public _getNotUploadedFiles(): Array<any> {
    return this.queue.filter((fileObject: FileObject) => !fileObject.fileObjectStatus.isUploaded);
  }

  // Description: Pause items that are over the concurrentUploadLimit by order in queue
  public _setPausedUploadedFiles(): Array<any> {
    return this.queue.filter((fileObject: FileObject, index: number) => {
      if (index >= this.options.concurrentUploadLimit) {
        fileObject.setFileObjectStatus({ isPaused: true });
      }
    });
  }

  // Description:  Unpause next item in queue
  public _nextReadyForUploadFile(): FileObject {
    const nextFO = this.queue.find(fileObject => {
      return !fileObject.fileObjectStatus.isUploaded && fileObject.fileObjectStatus.isPaused;
    });
    if (nextFO) {
      nextFO.setFileObjectStatus({ isReady: true, isPaused: false });
    }
    return nextFO;
  }

  public _getPausedUploadedFiles(): Array<any> {
    return this.queue.filter((fileObject: FileObject) => fileObject.fileObjectStatus.isPaused);
  }

  // Add to add to failedQueue and remove form upload queue
  public addToFailedQueue(fileObject: FileObject): void {
    this.failedQueue.push(fileObject);
    this.removeFromUploadQueue(fileObject);
  }

  // Description: File Upload methods
  // -------------------------------------------------------
  // Single methods:
  public uploadFile(fileObject: FileObject): void {
    try {
      this.processFileforUpload(fileObject);
    } catch (e) {
      console.error(e);
      this._onErrorCallback(fileObject, null, 0, null);
    }
  }

  // Single Cancel file: Stop the file uploading
  public cancelFile(fileObject: FileObject): void {
    if (fileObject && fileObject.fileObjectStatus.isUploading) {
      if (fileObject._uploadProcess) {
        fileObject._uploadProcess.unsubscribe();
      }
      this._onCancelCallback(fileObject);
    }
  }

  // Single remove file
  // Description: Remove single file from Queue and Stops any uploads
  public removeFromUploadQueue(fileObject: FileObject): void {
    const index = this._getIndexOfItem(fileObject);
    const fo = this.queue[index];
    if (fo) {
      if (fo.fileObjectStatus.isUploading) {
        this.cancelFile(fo);
      }
      this.queue.splice(index, 1);
      this.progress = this._getTotalProgress();
    }
  }

  public destroy(): void {
    return void 0;
  }

  // Description: Add fullPath attr to a fileList object
  public onBeforeItemsAddtoQueue(files: IUploadedFile[], fullPath?: string): IUploadedFile[] {
    let webkitRelativeFullPath;
    let fullpath = '';
    for (const file of files) {
      fullpath = fullPath;
      if (fullPath === null) {
        // This is an input upload and fullpath is null
        webkitRelativeFullPath = file.fullPathName;
        fullpath = '/' + (!!webkitRelativeFullPath && webkitRelativeFullPath.length ? webkitRelativeFullPath : file.name);
      }
      file.fullPathName = fullpath;
    }
    return files;
  }

  // Description: Public callback methods
  // -------------------------------------------------------------
  public onBeforeUploadFileObject(fileObject: FileObject): any {
    return { fileObject };
  }

  // Description: Public callback methods (used within the module)
  // -------------------------------------------------------------
  public _onSuccessCallback(fileObject: FileObject, response: any, status: number, headers: HttpHeaders): void {
    // console.log('_onSuccessCallback');
    fileObject._onSuccess(response, status, headers);
    this._onProgressFileObject(fileObject, 100);
    if (this.options.removeAfterUpload) {
      this.removeFromUploadQueue(fileObject);
    }

    this._onCompleteCallback(fileObject, response, status, headers);
  }

  public _onErrorCallback(fileObject: FileObject, response: any, status: number, headers: HttpHeaders): void {
    // console.log('_onErrorCallback');
    fileObject._onError(response, status, headers);
    this._onProgressFileObject(fileObject, 0);
    if (fileObject.options.retries >= this.options.maxNumberRetries) {
      this.addToFailedQueue(fileObject);
    }
  }

  public _onCompleteCallback(fileObject: FileObject, response: any, status: number, headers: HttpHeaders): void {
    // console.log('_onCompleteCallback');
    fileObject._onComplete(response, status, headers);
    this.isQueueUploading = false;
    this.progress = this._getTotalProgress();
    this.fireEvent({ eventName: FILEUPLOAD_EVENTS.complete, fileObject });

    // Reinitialize upload if more items remain
    if (this._getPausedUploadedFiles().length > 0) {
      const next = this._nextReadyForUploadFile();
      if (this.options.autoUpload || next.options.autoUpload) {
        this.uploadFile(next);
      }
    }
  }

  public getReadyItems(): Array<any> {
    return this.queue.filter((fileObject: FileObject) => fileObject.fileObjectStatus.isReady && !fileObject.fileObjectStatus.isUploading);
  }

  public _onCancelCallback(fileObject: FileObject): void {
    fileObject._onCancel();
    this.isQueueUploading = false;
    this._onProgressFileObject(fileObject, 0);
    this.progress = this._getTotalProgress();
  }

  // Description: File Upload protected methods
  // -------------------------------------------------------------
  protected _isValidFile(fileObject: FileObject, options: IFileUploadOptions): boolean {
    const file: File = fileObject.file;
    const filters = this.options.filters;

    // Validate file and compliance with options - file type
    let _failFilterIndex = -1;
    return !filters.length
      ? true
      : filters.every((filter: FilterFunction) => {
          _failFilterIndex++;
          const filterTest = filter.fn.call(this, file, options);
          if (!filterTest) {
            const opt: any = { isError: true };
            fileObject.setFileObjectStatus(opt);
            fileObject.setFileObjectMessaging(filter.errorMessage, true);
          }
          return filterTest;
        });
  }

  // ------------------------------------------------------------------------------------------------------
  // 2) PROCESSING and SENDING FILES
  // Description: actual upload tranfer for most browsers supporting Cross-site XMLHttpRequest file uploads
  // ------------------------------------------------------------------------------------------------------
  // Prepare file for upload  ***** Called from file.upload() class
  // --------------------------------------------------------------------------------------------------
  // PREPARATION PER FILE:
  // 1. Inspect File Size (use maxchunksize and misc flags for chunked upload)
  // 2. Fine # chunks
  // 3. Chop fileblobslice -> TBD
  // 4. Optional: Encrypt blob or file
  // 5. Send full file or Chunked upload ***** Working
  //    a. Concurrent upload - max number flag -> TBD
  //    b. Sequential chunk upload -> token dependent
  //    c. Retries on error methods -> flag for MaxNumberOfRetries
  //    d. Progress methods
  //    e. OnComplete messaging and error handling
  //    f. Headers and authorization tokens
  //    g. Other flags: Limit number of files to upload at once limitMultiFileUploadSize, limitConcurrentUploads, sequentialUploads:boolean
  //    h. forceIframeTransport and _iframeTransport methods ***** TBD if needed
  // --------------------------------------------------------------------------------------------------
  public processFileforUpload(fileObject: FileObject): void {
    const transport =
      this.options.isHTML5 && fileObject.options.isChunkedUpload
        ? '_chunkedUpload'
        : this.options.isHTML5 && !fileObject.options.isChunkedUpload
        ? '_xhrTransport'
        : '_iframeTransport';

    if (!fileObject.fileObjectStatus.isUploading) {
      fileObject.setFileObjectStatus({ isUploading: true });
      fileObject.errorMessage = []; // Reset error message
      fileObject._increaseRetry(); // Add to retries number
      this.isQueueUploading = true;
      (this as any)[transport](fileObject); // Trigger upload
    }
  }

  // Description: actual upload tranfer for most browsers supporting Cross-site XMLHttpRequest file uploads
  // ------------------------------------------------------------------------------------------------------
  protected _xhrTransport(fileObject: FileObject): any {
    const self = this;
    const file = fileObject.file;
    const options = fileObject.options;
    const url = options.url;
    self._initRequestData(file, options);
    const requestOptions = {
      headers: this._initRequestHeader(options.headers, file, options),
      params: this._initRequestParams(options),
      reportProgress: options.reportProgress || false,
      responseType: options.dataType as any,
      withCredentials: options.withCredentials || false
    };

    const req = new HttpRequest(options.method, options.url, options.data, requestOptions);

    fileObject._uploadProcess = this.http.request(req).subscribe(
      event => {
        switch (event.type) {
          case HttpEventType.Sent:
            self._onProgressFileObject(fileObject, 0);
            break;
          case HttpEventType.DownloadProgress:
          case HttpEventType.UploadProgress:
            if (event.total) {
              let progress = Math.round((event.loaded / event.total) * 100);
              if (progress === 100) {
                progress = 98;
              } // Let the final 100 be set by the actual respons
              self._onProgressFileObject(fileObject, progress);
            }
            break;
          case HttpEventType.Response:
            self._onSuccessCallback(fileObject, event.body, event.status, event.headers);
            break;
        }
      },
      error => {
        // Handle errors:
        self._onErrorCallback(fileObject, error.body, error.status, error.headers);
      }
    );
  }

  // Description: actual upload tranfer for older browsers not supporting  Cross-site XMLHttpRequest file uploads
  // The Iframe Transport requires a file input selection to upload files. To be implemented later as requested
  protected _iframeTransport(file: FileObject): any {
    console.error("This browser doesn't support HTML5 file upload or XHR Transport");
  }

  // Description: Chunked File Uploads
  // Flags: maxChunkSize, multipart
  // ------------------------------------------------------------------------------------------------------
  // Description: Uploads a file in multiple, sequential requests by splitting the file up in multiple blob chunks.
  protected _chunkedUpload(fileObject: FileObject) {
    const self = this,
      file = fileObject.file;
    const fileSize = file.size;
    const options = fileObject.options;
    let progress = fileObject.progress;
    let url = options.url;
    let uploadedBytes = options.uploadedBytes; // start
    const maxChunkSize = options.maxChunkSize || fileSize; // end
    const isMultiChunkedFile = fileSize >= options.maxChunkSize; // Local to store muliple blob uploads
    const numberOfChunks = Math.max(Math.ceil(fileSize / maxChunkSize), 1);
    let end;

    self._onProgressFileObject(fileObject, 0);

    function upload() {
      end = uploadedBytes + maxChunkSize;
      options.blob = file.slice(uploadedBytes, end);
      options.chunkSize = options.blob.size;
      options.contentRange = 'bytes ' + uploadedBytes + '-' + (uploadedBytes + options.chunkSize - 1) + '/' + fileSize;
      // Disable retries if this is the last chunk of a large file, being uploaded in multiple blobs
      const _disableRetries = uploadedBytes + options.chunkSize === fileSize && isMultiChunkedFile;

      // Process the upload data (the blob and potential form data):
      self._initRequestData(file, options);
      const requestOptions: any = {
          headers: self._initRequestHeader(options.headers, file, options),
          params: self._initRequestParams(options),
          reportProgress: options.reportProgress || false,
          responseType: options.dataType as any,
          withCredentials: options.withCredentials || false
        },
        req = new HttpRequest(options.method, url, options.data, requestOptions);

      fileObject._uploadProcess = self.http.request(req).subscribe(
        event => {
          switch (event.type) {
            case HttpEventType.Sent:
              break;
            case HttpEventType.DownloadProgress:
            case HttpEventType.UploadProgress:
              if (event.total) {
                let progress = Math.round(((event.loaded + uploadedBytes) / fileSize) * 100);
                if (progress === 100) {
                  progress = 98;
                } // Let the final 100 be set by the actual respons
                self._onProgressFileObject(fileObject, progress);
              }
              break;
            case HttpEventType.Response:
              // Handle Successes:
              const data = event.body as any,
                headers = event.headers;
              uploadedBytes = self._getUploadedBytes(headers) || uploadedBytes + options.chunkSize;
              progress = Math.round((uploadedBytes / fileSize) * 100);

              // Set progress:
              self._onProgressFileObject(fileObject, progress);

              // Use this line if additional data has to be passed with every chucnk
              const token =
                !!data.files && data.files[0].token
                  ? data.files[0].token
                  : !!data.response.token
                  ? data.response.token
                  : !!data.token
                  ? data.token
                  : null;

              url = options.url + '&token=' + encodeURIComponent(token);
              uploadedBytes < fileSize ? upload() : self._onSuccessCallback(fileObject, data, event.status, headers);
              break;
          }
        },
        error => {
          console.error('POST call in error', error);
          if (!_disableRetries && self.options.maxNumberRetries > fileObject.options.retries) {
            fileObject._increaseRetry();
            upload();
          } else {
            self._onErrorCallback(fileObject, error.body, error.status, error.headers);
          }
        }
      );
    } // end of upload();
    upload();
  }

  // Description: Add addtional parameters to the upload (need to expose this for additinal data capabilities)
  protected _initRequestHeader(headers: HttpHeaders | null, file: File, options: IFileObjectOptions): HttpHeaders {
    const self = this;
    headers = headers || new HttpHeaders();
    headers = headers.set('Accept', 'application/json');
    if (options.isChunkedUpload) {
      if (options.contentRange) {
        headers = headers.set('Content-Range', options.contentRange);
      }

      headers = headers.set('Content-Disposition', 'attachment; filename="' + encodeURI(file.name) + '"');
    }
    return headers;
  }

  // Description: Sets up headers, and data and for upload Post requests
  protected _initRequestData(file: File, options: IFileObjectOptions): IFileObjectOptions {
    const self = this;
    let formData;
    let inData;
    const _blob = options.isChunkedUpload ? options.blob : file;
    if (!this.options.disableMultipart) {
      inData = new FormData();
      inData.append('files[]', _blob, file.name);
    } else {
      inData = _blob;
    }

    options.data = inData;
    return options;
  }

  // Description: Add addtional parameters to the upload (need to expose this for additinal data capabilities)
  protected _initRequestParams(options: IFileObjectOptions): HttpParams {
    let params = new HttpParams();
    if (options.additionalParameter !== undefined) {
      Object.keys(options.additionalParameter).forEach((key: string) => {
        const paramValue = options.additionalParameter[key];
        if (isObject(paramValue)) {
          Object.keys(paramValue).forEach(k => {
            params = params.set(key + '.' + k, paramValue[k]);
          });
        } else {
          params = params.set(key, paramValue);
        }
      });
    }

    return params;
  }

  // Description: Parses the Range header from the server response and returns the uploaded bytes:
  _getUploadedBytes(responseHeaders: HttpHeaders) {
    const range = responseHeaders.get('Range'),
      parts = range && range.split('-'),
      upperBytesPos = parts && parts.length > 1 && parseInt(parts[1], 10);

    return upperBytesPos && upperBytesPos + 1;
  }

  // Description: Detect support for Blob slicing (required for chunked uploads):
  _supportBlobSlice() {
    return window.Blob && Blob.prototype.slice;
  }

  _isInstanceOf(type, obj) {
    // Cross-frame instanceof check
    return Object.prototype.toString.call(obj) === '[object ' + type + ']';
  }

  // ------------------------------------------------------------------------------------------------------
  // Description: Filters and supporting functions - return true/false for valid / invalid files
  // ------------------------------------------------------------------------------------------------------
  protected _queueLimitFilter(): boolean {
    return this.options.queueLimit === undefined || this.queue.length < this.options.queueLimit;
  }

  public _mimeTypeFilter(file: IUploadedFile): boolean {
    return !(this.options.allowedMimeType && this.options.allowedMimeType.indexOf(file.type) === -1);
  }

  public _fileSizeFilter(file: IUploadedFile): boolean {
    return !(this.options.maxFileSize && file.size > this.options.maxFileSize);
  }

  public _fileTypeFilter(file: IUploadedFile): boolean {
    // TO BE DONE ***** Working
    return true;
    // return !(this.options.allowedFileType &&
    // this.options.allowedFileType.indexOf(FileType.getMimeClass(item)) === -1);
  }

  // Description: Progress methods
  // ------------------------------------------------------------------------------------------------------
  public onProgressFileObject(fileObject: FileObject, progress: number): any {
    return { fileObject, progress };
  }

  public onProgressAll(progress: any): any {
    return { progress };
  }

  protected _onProgressFileObject(fileObject: FileObject, progress: any): void {
    fileObject._onProgress(progress);
    this.onProgressFileObject(fileObject, progress);
  }

  protected _getTotalProgress(value: number = 0): number {
    if (this.options.removeAfterUpload) {
      return value;
    }

    const notUploaded = this._getNotUploadedFiles().length,
      uploaded = notUploaded ? this.queue.length - notUploaded : this.queue.length,
      ratio = this.queue.length > 0 ? 100 / this.queue.length : 0,
      current = (value * ratio) / 100;
    return Math.round(uploaded * ratio + current);
  }

  // Description: Error handling - TBD
  // ------------------------------------------------------------------------------------------------------
  handleError(error: Response) {
    return Observable.throw(error.statusText);
  }
}
