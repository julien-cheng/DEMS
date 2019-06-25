import { HttpClient, HttpHeaders, HttpRequest, HttpResponse, HttpErrorResponse, HttpEvent, HttpEventType, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import {
    BatchRequestType,
    IBatchRequestOperations,
    IBatchOperation,
    IRequestOptions,
    ItemQueryType,
    IRequestBatchData,
    EventType,
    BatchRequestFn,
    IBatchResponse,
    batchOperationsDefaults,
    FileTypePipe
} from '../index';
// import * as api from "./request-api";

export class BatchRequest {
    operationsBatch: IBatchRequestOperations = null;
    options: IRequestOptions = {
        headers: new HttpHeaders({ 'Content-type': 'application/json' }),     //headers: HttpHeaders = new HttpHeaders({ 'Content-type': 'application/json' });
        apiUrl: 'api/batch',
        responseType: 'json'
    };
    requestType: BatchRequestType;
    eventType: EventType;
    rows: ItemQueryType[];
    observableRequest: Observable<IBatchResponse> = null;
    successMessage: string = batchOperationsDefaults.successMessages.BaseRequest;//'Your request was processed successfully!';
    errorMessage: string = batchOperationsDefaults.errorMessages.BaseRequest;//'There was a problem with request. Please, try again!';
    requestDialogConfirmation: { required: boolean; message?: string; isSendDisabled?: boolean } = { required: false }
    batchOperations: IBatchOperation[];

    constructor(requestBatchData: IRequestBatchData) {
        (!!requestBatchData.options) &&
            (requestBatchData.options = this._setRequestBatchOptions(requestBatchData.options));
        Object.assign(this, requestBatchData);
        this._retrieveRequestOperation();
        this.setRequireDialogConfirmation(requestBatchData.requestType);
    }

    // Gets the BatchOperation[] of Objects from the row item allowedOperations
    private _retrieveRequestOperation(): IBatchRequestOperations {
        if (this.batchOperations === undefined && !!this.rows && this.rows.length) {
            this.batchOperations = [];
            this.rows.map((row) => {
                !!row.allowedOperations &&
                    row.allowedOperations.forEach(
                        (operation) => {
                            (operation.batchOperation.type === this.requestType) && this.batchOperations.push(operation.batchOperation);
                        }
                    );
            });
        }
        
        return this.operationsBatch = {
            operations: (!!this.batchOperations && this.batchOperations.length) ? this.batchOperations : []
        };
    }

    // This request requires an intermediate confirmation dialog
    private setRequireDialogConfirmation(requestType: BatchRequestType) {
        //this.requireDialogConfirmation =(requestType === 'DeleteRequest' || requestType === 'RemoveRecipientRequest' );
        this.requestDialogConfirmation.required = !!batchOperationsDefaults.dialogMessages[requestType];
        if (!!batchOperationsDefaults.dialogMessages[requestType]) {
            let msg = batchOperationsDefaults.dialogMessages[requestType],
                typeLabel = !!this.rows && this.rows.length < 2 ? new FileTypePipe().transform(this.rows[0].type) : 'group of items';
            this.requestDialogConfirmation.message = msg.replace("{0}", typeLabel);
       
        }
    }


    //Merges the two options properly
    private _setRequestBatchOptions(options: IRequestOptions): IRequestOptions {
        options.apiUrl && (this.options.apiUrl = options.apiUrl);
        options.headers && (this.options.headers = options.headers);
        options.responseType && (this.options.responseType = options.responseType);
        options.params && (this.options.params = options.params);
        return this.options;
    }



    public destroy() {
        Object.assign(this, {
            requestType: null,
            eventType: null,
            rows: null,
            observableRequest: null
        });
    }
    // Set 'Content-type' header
    public setHeader(type?: string) {
        this.options.headers = type ? new HttpHeaders({ 'Content-type': type }) : new HttpHeaders({ 'Content-type': 'application/json' });
        return this.options.headers;
    }

    // Set misc request header
    public setMoreHeader(headers: HttpHeaders) {
        this.options.headers = headers;
        return this.options.headers;
    }

    // Base request url
    public setApiUrl(url?: string) {
        this.options.apiUrl = url ? url : 'api/batch';
        return this.options.apiUrl;
    }
}
