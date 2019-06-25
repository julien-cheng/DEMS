import { Inject, Injectable, Optional } from '@angular/core';
import { HttpClient, HttpHeaders, HttpRequest, HttpResponse, HttpErrorResponse, HttpEvent, HttpEventType, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { ToastsManager } from 'ng2-toastr';
import { ItemQueryType, IBatchResponse, IRequestOptions, IRecipient, IAllowedOperation, IBatchOperation, batchOperationsDefaults, NoButtonRequestTypes } from '../index';
import { BatchRequest, BatchResponse, BatchRequestType, EventType, EventTypes, BatchRequestTypes, BatchRequestFn, IRequestBatchData } from '../index';
import * as _ from 'lodash';
const { includes, pick } = _;



@Injectable()
export class BatchOperationService {
    private batchOperationsDefaults = batchOperationsDefaults;
    public modalLoading: boolean = false;
    public response: IBatchResponse = { success: true };
    batchRequestFns?: BatchRequestFn; // Store: all methods for the life cycle of each batchrequesttype
    batchResponse: BatchResponse;
    batchRequest: BatchRequest;
    dialogRequestBatchData: IRequestBatchData = null; //pass data to dialog
    destroyBatchOperationOnComplete: boolean = true; //set to true on complete of the request type cycle - default to no intermediate step

    constructor(
        private toastr: ToastsManager,
        protected http: HttpClient
    ) {
        this.response = { success: true }; // TBD
        this.batchRequestFns = this.setBatchOperationEvents();
    }

    public setBatchOperationEvents(): any {
        let _self = this, filter: (batchRequest: BatchRequest) => boolean;//BatchRequestFn;
        let actionObj = {};
        BatchRequestTypes.forEach((batchRequestType: BatchRequestType) => {
            let eventObj = {};
            EventTypes.forEach((eventType: EventType) => {
                filter = null;
                let fnName = batchRequestType + '_' + eventType;
                filter = (!!(this as any)[fnName]) ? (this as any)[fnName] :
                    eventType === 'initialize' ? _self.BaseRequest_initialize :
                        eventType === 'send' ? _self.BaseRequest_send :
                            eventType === 'success' ? _self.BaseRequest_success :
                                eventType === 'error' ? _self.BaseRequest_error :
                                    eventType === 'complete' ? _self.BaseRequest_complete : null;
                !!filter && (eventObj[eventType] = filter);
            });
            actionObj[batchRequestType] = eventObj;
        });
        return actionObj;
    }

    // Description: Initialize and Process the batch request action by requestType & eventType
    // Returns a batchResponse instance
    public processBatchRequestAction(requestBatchData: IRequestBatchData) {
        //    console.log('processBatchRequestAction: ', requestBatchData);
        this.batchRequest = new BatchRequest(requestBatchData);
        this.batchResponse = new BatchResponse();
        if (!!this.batchRequestFns[this.batchRequest.requestType]) {
            let response = (!!this.batchRequestFns[this.batchRequest.requestType][this.batchRequest.eventType]) &&
                this.batchRequestFns[this.batchRequest.requestType][this.batchRequest.eventType].call(this, this.batchRequest);
        } else {
            // Default to a base request:
            let response = (!!this.batchRequestFns['BaseRequest'][this.batchRequest.eventType]) &&
                this.batchRequestFns['BaseRequest'][this.batchRequest.eventType].call(this, this.batchRequest);
            // let response = this.BaseRequest_error.call(this, this.batchRequest);
            // console.error("ABORT REQUEST: error in batch request model events");
        }

        return this.batchResponse;
    }

    // Description:  Destroy instances of request and response and reset obj values
    public destroy() {
        // console.log('destroy');
        this.modalLoading = false;
        this.destroyBatchOperationOnComplete = true;
        this.dialogRequestBatchData = null;
        this.batchRequest.destroy();
        this.batchResponse.destroy();
    }

    // Individual Actions:  defaultAction: defaultAction/BaseRequest, deleteAction, downloadAction ...
    // --------------------------------------------------------------------------------------------
    // Populate batchRequest with the right response type / action
    private BaseRequest_initialize(batchRequest: BatchRequest) {
        // console.log('BaseRequest_initialize', batchRequest);
        if (batchRequest.requestDialogConfirmation.required) {   // Most delete/remove actions will go through here: "DeleteRequest", "RemoveRecipientRequest"
            this.dialogRequestBatchData = {
                requestType: batchRequest.requestType,
                eventType: EventType.send,
                batchOperations: batchRequest.batchOperations  // rows: batchRequest.rows
            }
            this.destroyBatchOperationOnComplete = false; // This is an intermediate - keep instance
            this.batchResponse.loadBatchResponse({
                callbackFunctionName: 'openModal',
                callbackFunctionArg: {
                    modalId: '#confirmationDialog',
                },
                displayMessageOnSuccess: false,
                triggerSuccessEventOnSuccess: false,
            });
        } else {
            // Otherwise send to server directly
            return (!!batchRequest.operationsBatch.operations) ? this.BaseRequest_send(batchRequest) : true;
        }
    }

    // Send to server request
    private BaseRequest_send(batchRequest: BatchRequest): Observable<any> {
        //  console.log('BaseRequest_send', batchRequest); //Catch all server call here
        if (!!batchRequest.operationsBatch) { // console.log("batchRequest.operationsBatch is not null");
            this.destroyBatchOperationOnComplete = true;  // Destroy instance after send call
            this.batchResponse.loadBatchResponse({
                callbackFunctionName: 'updateView',
                callbackFunctionArg: { successMessage: batchRequest.successMessage },
                displayMessageOnSuccess: false,
                triggerSuccessEventOnSuccess: true,
                handleLoadingonCallback: true
            });
            return this.batchRequest.observableRequest = this.batchPost(batchRequest);
        } else {
            console.error("batchRequest.operationsBatch is null ERROR OUT");
            return this.batchRequest.observableRequest = this.handleError('ERROR'); //RETURN ERROR
        }
    }

    private BaseRequest_success(batchRequest: BatchRequest, batchResponse: BatchResponse): boolean {
        //  console.log('BaseRequest_success', batchResponse);
        return true;
    }

    private BaseRequest_error(batchRequest: BatchRequest, batchResponse: BatchResponse) {
        // console.log('BaseRequest_error');
        return false;
    }

    //Remove instances of request and response
    private BaseRequest_complete(batchRequest: BatchRequest) {
        //  console.log('BaseRequest_complete', batchRequest, this.batchResponse, this.destroyBatchOperationOnComplete); //Catch all server call here
        (this.destroyBatchOperationOnComplete) && this.destroy.call(this);
    }

    // New Path Operations
    // -------------------------
    private NewPathRequest_initialize(batchRequest: BatchRequest) {
        this.destroyBatchOperationOnComplete = false;
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'loadCreateFolderModal',
            displayMessageOnSuccess: false,
            triggerSuccessEventOnSuccess: false
        });
        return true;
    }

    // Rename Request Events:
    // -------------------------
    private RenameRequest_initialize(batchRequest: BatchRequest): any {
        // console.log('RenameRequest_initialize');
        this.batchResponse.loadBatchResponse({ displayMessageOnSuccess: false, triggerSuccessEventOnSuccess: false });
        batchRequest.rows.map((row) => { row.editMode = true; });
        return true;
    }

    private RenameRequest_send(batchRequest: BatchRequest): Observable<any> {
        // console.log('RenameRequest_send', batchRequest);
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'updateView',
            callbackFunctionArg: { successMessage: batchRequest.successMessage },
            displayMessageOnSuccess: false,
            handleLoadingonCallback: true
        });

        return this.batchRequest.observableRequest = this.batchPost(batchRequest);
    }

    private RenameRequest_success(batchRequest: BatchRequest): boolean {
        // console.log('RenameRequest_success');
        batchRequest.rows.map((row) => {
            row.editMode = false;
            row.name = 'Saving changes...';
        });
        return true;
    }

    private RenameRequest_error(batchRequest: BatchRequest, batchResponse: BatchResponse): boolean {
        batchRequest.rows.map((row) => {
            row.editMode = false;
        });
        return true;
    }

    // Download Request Events: This will change to implement new file-download component  ***** Working  *****
    // **** only handles singles right now mulitupload to be implemented
    // -------------------------
    private DownloadRequest_initialize(batchRequest: BatchRequest): boolean {
        // console.log('DownloadRequest_initialize', batchRequest);
        let fileURL: string = '';
        // let operations= <IDownloadRequest[]>batchRequest.operationsBatch.operations;
        (batchRequest.operationsBatch.operations).map((operation) => {
            fileURL = `/api/file/contents?fileidentifier.organizationKey=${operation.fileIdentifier.organizationKey}&fileidentifier.folderKey=${operation.fileIdentifier.folderKey}&fileidentifier.fileKey=${operation.fileIdentifier.fileKey}`;
        });
        this.response.success = true;  // HARD CODED //(!!operation.folderKey && !!operation.fileKey);

        (this.response.success) ?
            (window.location.href = fileURL) : console.error('The file download url is malformed');
        this.batchRequest.observableRequest = Observable.of(this.response);
        return true;
    }

    // Download zipped files - need to use the 
    private DownloadZipFileRequest_initialize(batchRequest: BatchRequest) {
        // console.log('DownloadZipFileRequest_initialize', batchRequest);
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'submitbatchNativeForm',
            callbackFunctionArg: { operations: batchRequest.operationsBatch.operations },
            displayMessageOnSuccess: true,
            triggerSuccessEventOnSuccess: false
        });
        return Observable.of(this.response); //this.batchRequest.observableRequest = Observable.of(this.response);
    }

    // MoveInto Request Events:
    // -------------------------
    private MoveIntoRequest_initialize(batchRequest: BatchRequest) {
        // console.log('MoveIntoRequest_initialize', batchRequest);
        this.destroyBatchOperationOnComplete = false;
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#moveExplorerModal'
            },
            displayMessageOnSuccess: false,
            triggerSuccessEventOnSuccess: false,
        });
        return this.batchRequest.observableRequest = Observable.of(this.response);
    }

    private MoveIntoRequest_send(batchRequest: BatchRequest): Observable<any> {
        // console.log('MoveIntoRequest_send', batchRequest);
        this.destroyBatchOperationOnComplete = true;
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'updateView',
            callbackFunctionArg: { successMessage: batchRequest.successMessage },
            displayMessageOnSuccess: false,
            handleLoadingonCallback: true
        });
        return this.batchRequest.observableRequest = this.batchPost(batchRequest);
    }

    // Recipient Actions: Add, display, delete, regenerate password: 
    // ------------------------------------------------------------
    // Rename Request Events: 
    // modalFocusInput input is the input ViewChild that will receive focus on open
    // modalForm Form name to reset on close of the modal
    private AddRecipientRequest_initialize(batchRequest: BatchRequest): any {
        // console.log('AddRecipientRequest_initialize', batchRequest);
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#addRecipientModal',
                data: {
                    modalFocusInput: 'modalFocusInput',
                    modalForm: 'newRecipientForm',
                    modalOnHiddenCallback: 'resetAddRecipientModal',
                    modalBeforeShowCallback: 'AddRecipientFormDefaults'
                }
            },
            displayMessageOnSuccess: false,
            triggerSuccessEventOnSuccess: false,
        });
        return this.batchRequest.observableRequest = Observable.of(this.response);
    }

    private AddRecipientRequest_send(batchRequest: BatchRequest): Observable<any> {
        // console.log('AddRecipientRequest_send');
        let options = batchRequest.options,
            headers = new HttpHeaders({ 'Content-type': 'application/json' }),
            requestOptions = { headers: headers },// new RequestOptions({ headers }),
            body = {
                FirstName: options.params['newRecipientFName'],
                LastName: options.params['newRecipientLName'],
                RecipientEmail: options.params['newRecipientEmail'],
                FolderIdentifier: options.params['folderIdentifier'],
                type: 'AddRecipientRequest'
            };
        let arg = Object.assign(this.batchResponse, this.batchResponse);
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'newRecipientCreatedCallback',
            errorCallbackFunctionName: 'resetAddRecipientModal',
            displayMessageOnSuccess: false
        });
        return this.batchRequest.observableRequest = this.http.post<IBatchResponse>(batchRequest.options.apiUrl, body, requestOptions);
    }

    // Regenerates password 
    private RegenerateRecipientPasswordRequest_initialize(batchRequest: BatchRequest) {
        // console.log('RegenerateRecipientPasswordRequest_initialize', batchRequest);
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#addRecipientModal',
                data: {
                    modalBeforeShowCallback: 'loadRecipientModalData',
                    modalOnHiddenCallback: 'resetAddRecipientModal'
                }
            },
            displayMessageOnSuccess: false
        });
        return this.batchRequest.observableRequest = this.batchPost(batchRequest);
    }


    // Recipient Events Display and edit:
    private DisplayRecipientRequest_initialize(batchRequest: BatchRequest) {
        // console.log('DisplayRecipientRequest_initialize', batchRequest);
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#addRecipientModal',
                data: {
                    // destroyBatchOperation: true,
                    modalBeforeShowCallback: 'loadRecipientModalData',
                    modalOnHiddenCallback: 'resetAddRecipientModal'
                }
            },
            displayMessageOnSuccess: false,
            triggerSuccessEventOnSuccess: false
        });

        const row = <IRecipient>batchRequest.rows[0];
        return this.batchRequest.observableRequest = Observable.of({
            success: true,
            response: {
                name: row.name,
                email: row.email,
                password: null,
                magicLink: row.magicLink,
                expirationDate: row.expirationDate,
                type: 'RecipientResponse'
            }
        });
    }

    // Turn over / Publish Actions: Modal and custom Name edit 
    // ------------------------------------------------------------
    private PublishRequest_initialize(batchRequest: BatchRequest) {
        // console.log('PublishRequest_initialize', batchRequest);
        (this.batchRequest.requestDialogConfirmation.isSendDisabled = this.batchRequest.batchOperations[0].eDiscoveryRecipientCount === 0);
        this.dialogRequestBatchData = {
            requestType: batchRequest.requestType,
            eventType: EventType.send,
            batchOperations: batchRequest.batchOperations  // rows: batchRequest.rows
        };
        this.destroyBatchOperationOnComplete = false; // This is an intermediate - keep instance
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#turnOverModal',
                data: {
                    modalFocusInput: 'publishModalFocusInput',
                    modalForm: 'publishCustomName'
                }
            },
            displayMessageOnSuccess: false,
            triggerSuccessEventOnSuccess: false,
        });
        return this.batchRequest.observableRequest = Observable.of(this.response);
    }

    // Edit Package Name Request
    private EditPackageNameRequest_initialize(batchRequest: BatchRequest) {
        // console.log('EditPackageNameRequest_initialize', batchRequest);
        (this.batchRequest.requestDialogConfirmation.isSendDisabled = this.batchRequest.batchOperations[0].eDiscoveryRecipientCount === 0);
        this.dialogRequestBatchData = {
            requestType: batchRequest.requestType,
            eventType: EventType.send,
            batchOperations: batchRequest.batchOperations  // rows: batchRequest.rows
        };
        this.destroyBatchOperationOnComplete = false; // This is an intermediate - keep instance
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#turnOverModal',
                data: {
                    modalFocusInput: 'publishModalFocusInput',
                    modalForm: 'publishCustomName',
                    modalBeforeShowCallback: 'loadDiscoveryCustomNameData',
                    modalOnHiddenCallback: 'resetDiscoveryCustomNameModal'
                }
            },
            displayMessageOnSuccess: false,
            triggerSuccessEventOnSuccess: false,
        });
        return this.batchRequest.observableRequest = Observable.of(this.response);
    }

    // LEO Uploads / Publish Actions: Modal and custom Name edit 
    // ------------------------------------------------------------
    private AddOfficerRequest_initialize(batchRequest: BatchRequest): any {
        // console.log('AddOfficerRequest_initialize', batchRequest.requestType);
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#addRecipientModal',
                data: {
                    modalFocusInput: 'modalFocusInput',
                    modalForm: 'newRecipientForm',
                    modalOnHiddenCallback: 'resetAddRecipientModal' 
                    // modalBeforeShowCallback: 'AddRecipientFormDefaults' // - NOTE: Uncomment this line to add defaults to LEO 
                }
            },
            displayMessageOnSuccess: false,
            triggerSuccessEventOnSuccess: false,
        });
        return this.batchRequest.observableRequest = Observable.of(this.response);
    }

    private AddOfficerRequest_send(batchRequest: BatchRequest): Observable<any> {
        let options = batchRequest.options,
            headers = new HttpHeaders({ 'Content-type': 'application/json' }),
            requestOptions = { headers: headers },// new RequestOptions({ headers }),
            body = {
                FirstName: options.params['newRecipientFName'],
                LastName: options.params['newRecipientLName'],
                RecipientEmail: options.params['newRecipientEmail'],
                FolderIdentifier: options.params['folderIdentifier'],
                type: 'AddOfficerRequest'
            };
        let arg = Object.assign(this.batchResponse, this.batchResponse);
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'newRecipientCreatedCallback',
            errorCallbackFunctionName: 'resetAddRecipientModal',
            displayMessageOnSuccess: false
        });
        return this.batchRequest.observableRequest = this.http.post<IBatchResponse>(batchRequest.options.apiUrl, body, requestOptions);
    }

    private RegenerateOfficerRequest_initialize(batchRequest: BatchRequest) {
        // console.log('RegenerateOfficerRequest_initialize', batchRequest);
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#addRecipientModal',
                data: {
                    modalBeforeShowCallback: 'loadRecipientModalData',
                    modalOnHiddenCallback: 'resetAddRecipientModal'
                }
            },
            displayMessageOnSuccess: false
        });
        return this.batchRequest.observableRequest = this.batchPost(batchRequest);
    }

    // Media Clips:
    // ------------------------------------------------------------
    // Export Clip
    private ExportClipRequest_initialize(batchRequest: BatchRequest) {
        //  console.log('ExportClipRequest_initialize: ', batchRequest);
        this.dialogRequestBatchData = {
            requestType: batchRequest.requestType,
            eventType: EventType.send,
            batchOperations: batchRequest.batchOperations  // rows: batchRequest.rows
        };
        this.destroyBatchOperationOnComplete = false; // This is an intermediate - keep instance
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#exportClipModal',
                data: {
                    modalFocusInput: 'clipModalFocusInput',
                    modalForm: 'exportClipForm',
                    modalBeforeShowCallback: 'setModalText',
                    modalOnHiddenCallback: 'resetExportClipModal'
                }
            },
            displayMessageOnSuccess: false,
            triggerSuccessEventOnSuccess: false
        });
        return this.batchRequest.observableRequest = Observable.of(this.response);
    }

    // Send to server request
    private ExportClipRequest_send(batchRequest: BatchRequest): Observable<any> {
        // console.log('ExportClipRequest_send', batchRequest); //Catch all server call here
        if (!!batchRequest.operationsBatch) { // console.log("batchRequest.operationsBatch is not null");
            this.destroyBatchOperationOnComplete = true;  // Destroy instance after send call
            this.batchResponse.loadBatchResponse({
                callbackFunctionName: 'updateView',
                callbackFunctionArg: { successMessage: batchRequest.successMessage },
                displayMessageOnSuccess: true
            });
            return this.batchRequest.observableRequest = this.batchPost(batchRequest);
        } else {
            console.error("batchRequest.operationsBatch is null ERROR OUT");
            return this.batchRequest.observableRequest = this.handleError('ERROR'); //RETURN ERROR
        }
    }

    // Export Frame / segment
    private ExportFrameRequest_initialize(batchRequest: BatchRequest) {
        // console.log('ExportClipRequest_initialize: ', batchRequest);
        this.dialogRequestBatchData = {
            requestType: batchRequest.requestType,
            eventType: EventType.send,
            batchOperations: batchRequest.batchOperations  // rows: batchRequest.rows
        };
        this.destroyBatchOperationOnComplete = false; // This is an intermediate - keep instance
        this.batchResponse.loadBatchResponse({
            callbackFunctionName: 'openModal',
            callbackFunctionArg: {
                modalId: '#exportClipModal',
                data: {
                    modalFocusInput: 'clipModalFocusInput',
                    modalForm: 'exportClipForm',
                    modalBeforeShowCallback: 'setModalText',
                    modalOnHiddenCallback: 'resetExportClipModal'
                }
            },
            displayMessageOnSuccess: false,
            triggerSuccessEventOnSuccess: false
        });
        return this.batchRequest.observableRequest = Observable.of(this.response);
    }

    // Send to server request
    private ExportFrameRequest_send(batchRequest: BatchRequest): Observable<any> {
        //  console.log('ExportClipRequest_send', batchRequest); //Catch all server call here
        if (!!batchRequest.operationsBatch) {
            this.destroyBatchOperationOnComplete = true;  // Destroy instance after send call
            this.batchResponse.loadBatchResponse({
                displayMessageOnSuccess: true
            });
            return this.batchRequest.observableRequest = this.batchPost(batchRequest);
        } else {
            console.error("batchRequest.operationsBatch is null ERROR OUT");
            return this.batchRequest.observableRequest = this.handleError('ERROR'); //RETURN ERROR
        }
    }

    // Public methods
    //---------------------------------------------------------------------------------
    //---------------------------------------------------------------------------------
    // Description: Returns whether a row/itemQueryType is a MoveIntoDrop target (drop or click)
    // Source is the element being dragged/moved (draggable)
    // Target is the path/item recieving the item being moved (droppable)
    isPathMoveIntoItem(itemQueryType: ItemQueryType, moveintoType: 'sourceFile' | 'sourcePath' | 'targetPath'): boolean {
        let isSourceOrTarget = false;
        if (!!itemQueryType.allowedOperations) {
            let key = moveintoType + 'Identifier';
            let allowedOperationsArr = this.retrieveRequestOperation([itemQueryType], 'MoveIntoRequest');
            isSourceOrTarget = (!!allowedOperationsArr && allowedOperationsArr.length > 0) && !!(allowedOperationsArr[0])[key];
        }
        return isSourceOrTarget;
    }

    // Description: build an array of BatchRequestType to make components available ***** working might need a different obj/array
    public extractrequestTypesFlags(allowedOperations: IAllowedOperation[]) {
        let requestTypesFlags = {};
        allowedOperations.forEach((allowedOperation) => {
            requestTypesFlags[allowedOperation.batchOperation.type] = true;
        });
        return requestTypesFlags;
    }

    // Description: UI sort actions according to batch request type
    // Notes: Future adjustments to post - extraHttpRequestParams and headers
    processBatchRequestPost(batchRequest: BatchRequest): Observable<any> {
        //console.log('processBatchRequestPost', batchRequest);
        return this.batchPost(batchRequest);
    }

    /**
     * Execute a set of operations
     *
     * @param request
     */
    public batchPost(batchRequest?: BatchRequest, extraHttpRequestParams?: any): Observable<IBatchResponse> {
        // console.log(this.batchPostWithHttpInfo(batchRequest, extraHttpRequestParams));
        return this.batchPostWithHttpInfo(batchRequest, extraHttpRequestParams)
            .map((response: IBatchResponse) => {
                if (response.statusCode === 204) {
                    return undefined;
                } else {
                    return response;
                }
            });
        //  return Observable.of({success: true});
    }

    /**
     * Execute a set of operations
     *
     * @param request
     */
    public batchPostWithHttpInfo(batchRequest?: BatchRequest, extraHttpRequestParams?: any): Observable<IBatchResponse> {
        // const queryParameters = new URLSearchParams();// Bug in IE 11 - not needed
        const responseType = batchRequest.options.responseType; //'json' as 'json'; 
        const headers = batchRequest.options.headers || new HttpHeaders();
        const body = !!batchRequest && JSON.stringify(batchRequest.operationsBatch) || '';

        // to determine the Content-Type header
        const consumes: string[] = [
            'application/json'
        ];

        // to determine the Accept header
        const produces: string[] = [
            'application/json'
        ];

        let requestOptions = {
            headers: headers,
            responseType: responseType as 'json'// issue with typescript typing of the responseType option object: https://github.com/angular/angular/issues/18586
        };

        if (extraHttpRequestParams) {
            requestOptions = (<any>Object).assign(requestOptions, extraHttpRequestParams);
        }

        return this.http.post<IBatchResponse>(batchRequest.options.apiUrl, body, requestOptions);
    }

    // Misc Supporting methods
    //----------------------------------------------------------
    // Description: Is this a button driven operation? (for displaying buttons in menus)
    public displayButtonByOperation(allowedOperation: IAllowedOperation): boolean {
        return NoButtonRequestTypes.filter(function (el) {
            return el.indexOf(allowedOperation.batchOperation.type) > -1;
        }).length <= 0;

        //return allowedOperation.batchOperation.type !== 'UploadRequest' && allowedOperation.batchOperation.type !== 'SearchRequest';
    }


    public retrieveRequestOperation(rows: ItemQueryType[], requestType: BatchRequestType): IBatchOperation[] {
        let objectTyped: IBatchOperation[] = [];
        if (!!rows) {
            rows.map((row) => {
                row.allowedOperations.forEach(
                    (operation) => {
                        (operation.batchOperation.type === requestType) && objectTyped.push(operation.batchOperation);
                    }
                );
            });
        }
        return objectTyped;
    }

    handleError(error?: Response | string) {
        if (typeof error === 'string') {
            return Observable.throw(error);
        } else {
            return Observable.throw(error.statusText);
        }
    }
}

