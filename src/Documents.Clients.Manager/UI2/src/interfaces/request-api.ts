// import { Observable } from 'rxjs/Observable';
// import { HttpHeaders, HttpParams } from '@angular/common/http';
import 'axios';
import Axios, { AxiosResponse, AxiosRequestConfig } from 'axios';
import { BatchRequest } from './batch-request.model';
import { ItemQueryType } from './view.model';
import {
  TIdentifierType,
  IFileIdentifier,
  IFolderIdentifier,
  IOrganizationIdentifier,
  IPathIdentifier,
} from './identifiers.model';

// Types:
// ---------------------------------------------
// export type EventType = 'initialize' | 'beforeSend' | 'send' | 'success' | 'error' | 'complete' | 'cancel';
export type BatchRequestFn = {
  BatchRequestType: { EventType: { fn: (batchRequest: BatchRequest) => boolean } };
};
export enum EventType {
  initialize = 'initialize',
  beforeSend = 'beforeSend',
  send = 'send',
  success = 'success',
  error = 'error',
  complete = 'complete',
  cancel = 'cancel',
}

// Interfaces
// ---------------------------------------------
export interface IBatchResponse<T> {
  success: boolean;
  response?: T; //{operationResponses: IBatchResponse[]}
  statusCode?: number; // TEMP - will be HttpStatusCode
  exception?: string;
  exceptionType?: string;
  exceptionStack?: string;
  elapsed?: number;
}

export interface IRequestBatchData {
  requestType: BatchRequestType;
  eventType: EventType; // request lifecycle event type: 'beforeSend' | 'send' | 'success' | 'error' | 'complete '
  batchOperations?: IBatchOperation[];
  options?: IRequestOptions;
  rows?: ItemQueryType[];
}

export interface IRequestOptions {
  apiUrl: string;
  headers?: object;
  observe?: 'body';
  params?: AxiosRequestConfig;
  reportProgress?: boolean;
  responseType?: 'arraybuffer' | 'blob' | 'json' | 'text';
  withCredentials?: boolean;
}

// Sent to the server as an array of operations (type->BatchOperation)
export interface IBatchRequestOperations {
  operations: IBatchOperation[];
}

/// Requests models:
export interface IBatchOperation {
  type: BatchRequestType;
  identifier?: TIdentifierType;
  fileIdentifier?: IFileIdentifier;
  folderIdentifier?: IFolderIdentifier;
  organizationIdentifier?: IOrganizationIdentifier;
  pathIdentifier?: IPathIdentifier;
  sourceFileIdentifier?: IFileIdentifier;
  sourcePathIdentifier: IPathIdentifier;
  targetPathIdentifier: IPathIdentifier;
  defaults?: any;
  newName?: string;
  viewerType?: string;
  Open?: boolean;
  shared?: boolean;
  eDiscoveryRecipientCount?: number; // Will be removed after integrating warning props
  milliseconds?: number; //for media clips
  clip?: any; //for media clips
  fileName?: string; //for media clips
}

// Store UI Action type in this batchRequestEvents
// -------------------------------------------------------------------------------------------------
export type BatchRequestType =
  | 'BaseRequest'
  | 'RenameRequest'
  | 'DeleteRequest'
  | 'DownloadRequest'
  | 'UploadRequest'
  | 'MoveIntoRequest'
  | 'ExtractRequest'
  | 'ShareRequest'
  | 'PublishRequest'
  | 'AddRecipientRequest'
  | 'DisplayRecipientRequest'
  | 'RemoveRecipientRequest'
  | 'RegenerateRecipientPasswordRequest'
  | 'SendToEArraignmentRequest'
  | 'NewPathRequest'
  | 'DownloadZipFileRequest'
  | 'SaveRequest'
  | 'EditPackageNameRequest'
  | 'SearchRequest'
  | 'AddOfficerRequest'
  | 'RegenerateOfficerRequest'
  | 'RemoveOfficerRequest'
  | 'ExportClipRequest'
  | 'ExportFrameRequest';

export const BatchRequestTypes: BatchRequestType[] = [
  'BaseRequest',
  'RenameRequest',
  'DeleteRequest',
  'DownloadRequest',
  'UploadRequest',
  'MoveIntoRequest',
  'ExtractRequest',
  'ShareRequest',
  'PublishRequest',
  'AddRecipientRequest',
  'DisplayRecipientRequest',
  'RemoveRecipientRequest',
  'RegenerateRecipientPasswordRequest',
  'SendToEArraignmentRequest',
  'NewPathRequest',
  'DownloadZipFileRequest',
  'SaveRequest',
  'EditPackageNameRequest',
  'AddOfficerRequest',
  'RegenerateOfficerRequest',
  'RemoveOfficerRequest',
  'ExportClipRequest',
  'ExportFrameRequest',
];

// Stores the type or recipients: ediscovery, leo ... add more
export enum RequestRecipientType {
  eDiscovery = 'eDiscovery',
  leo = 'leo',
}

// Add the request types that should not have a button or link associated with it
export const NoButtonRequestTypes: BatchRequestType[] = ['UploadRequest', 'SearchRequest'];

export const EventTypes = [
  'initialize',
  'beforeSend',
  'send',
  'success',
  'error',
  'complete',
  'cancel',
];
export const batchOperationsDefaults = {
  labels: {
    BaseRequest: 'Save',
    DownloadRequest: 'Download',
    RenameRequest: 'Rename',
    MoveIntoRequest: 'Move',
    ExtractRequest: 'Extract',
    DeleteRequest: 'Delete',
    UploadRequest: 'Upload',
    ShareRequest: 'Share',
    PublishRequest: 'Publish',
    AddRecipientRequest: 'AddRecipientRequest',
    DisplayRecipientRequest: 'DisplayRecipientRequest',
    RemoveRecipientRequest: 'RemoveRecipientRequest',
    RegenerateRecipientPasswordRequest: 'RegenerateRecipientPasswordRequest',
    SendToEArraignmentRequest: 'SendToEArraignmentRequest',
    NewPathRequest: 'NewPathRequest',
    RemoveOfficerRequest: 'RemoveOfficerRequest',
    RegenerateOfficerRequest: 'RegenerateOfficerRequest',
  },
  icons: {
    BaseRequest: 'fa-floppy-o',
    DownloadRequest: 'fa-download',
    RenameRequest: 'fa-pencil-square-o',
    MoveIntoRequest: 'fa-sign-in',
    ExtractRequest: 'fa-magic',
    DeleteRequest: 'fa-trash-o',
    UploadRequest: 'fa-upload',
    ShareRequest: 'fa-share-square-o ',
    PublishRequest: 'fa-list-alt',
    AddRecipientRequest: 'fa-user-plus',
    RemoveRecipientRequest: 'fa-user-times',
    RegenerateRecipientPasswordRequest: 'fa-key',
    SendToEArraignmentRequest: 'fa-pencil',
    NewPathRequest: 'fa-folder',
    DownloadZipFileRequest: 'fa-download',
    RemoveOfficerRequest: 'fa-user-times',
    RegenerateOfficerRequest: 'fa-key',
    AddOfficerRequest: 'fa-user-plus',
    ExportClipRequest: 'fa-share-square-o',
    ExportFrameRequest: 'fa-picture-o',
  },
  dialogMessages: {
    // If message exists here there will be a confirmation dialog.
    DeleteRequest: 'Are you sure you want to delete this {0}?',
    RemoveRecipientRequest: 'Are you sure you want to remove this recipient?',
    PublishRequest:
      'You are about to share all the files in this package with all listed recipients. It is recommended that you draft a follow-up email to make these recipients aware of the new discovery package.',
    EditPackageNameRequest: 'Change the package description:',
    RemoveOfficerRequest: 'Are you sure you want to remove this officer?',
    // ExportClipRequest: 'Are you sure you want to export frame: {0}' // To be debugged
  },
  successMessages: {
    // To be used if needed
    BaseRequest: 'Your request was processed successfully!',
    DownloadRequest: 'Item download was processed successfully',
  },
  errorMessages: {
    BaseRequest: 'There was a problem with request. Please, try again!',
    DownloadRequest: 'fa-download',
  },
  eDiscoveryModalTemplate: {
    title: ["Recipient's Information", 'Add Recipient'],
    instructions: 'Add new recipient for this case eDiscovery Folder',
    labels: {
      firstName: "Recipient's First Name",
      lastName: "Recipient's Last Name",
      email: "Recipient's Email",
      name: "Recipient's Name",
      password: "Recipient's Password",
      magicLink: "Recipient's Magic Link",
    },
    saveButtonText: 'Add Recipient',
  },
  leoModalTemplate: {
    title: ["Officer's Information", 'Add Officer'],
    instructions: 'Authorize a new officer to add case files.',
    labels: {
      firstName: "Officers's First Name",
      lastName: "Officers's Last Name",
      email: "Officers's Email",
      name: "Officers's Name",
      password: "Officers's Password",
      magicLink: "Officers's Magic Link",
    },
    saveButtonText: 'Add Officer',
  },
};
