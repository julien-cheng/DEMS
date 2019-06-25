import { IBatchResponse } from '../index';

export class BatchResponse {
    batchResponseResult?: IBatchResponse[];
    callbackFunctionName?: string; //assign view local success callbacks (for custom methods within views)
    callbackFunctionArg?: any;
    callbackFunctionData?: any = null; //Use as temp data holder 
    errorCallbackFunctionName?: string; //assign view local error or success callbacks (for custom methods within views)
    displayMessageOnSuccess: boolean = true;
    handleLoadingonCallback: boolean = false; //Leave the loading indicator for the callback call - success or error
    success: boolean = true;
    triggerSuccessEventOnSuccess: boolean = true; //Block requestType_success function on specific calls (initialize or send)
    isMultiresponse: boolean = false;
    errorBatchResponseResult?: IBatchResponse[];
    constructor(values: Object = {}) {

        this.loadBatchResponse();
    }
    
    // Set up response handling - { callbackFunctionName?: string, batchResponseCallbackFn?: Function, batchResponseErrorFn?: Function }
    public loadBatchResponse(data?: any) {
        Object.assign(this, data);
    }

    // Sets the batch response from the results of the call
    public setBatchResponse(batchResponseResult: IBatchResponse): IBatchResponse[] {
       this.success = batchResponseResult.success;
       this.batchResponseResult = (!!batchResponseResult.response && !!batchResponseResult.response.operationResponses) ?
                batchResponseResult.response.operationResponses : !!batchResponseResult.response ? [{ response: batchResponseResult.response }] : [];
        this.isMultiresponse = this.batchResponseResult.length > 1;
        this.errorBatchResponseResult = this.batchResponseResult.filter((obj)=>{
            return (obj.success !== undefined) ? !obj.success:false;
        });
      
        return this.batchResponseResult;
    }
  

    public destroy() {
        Object.assign(this, {
            batchResponseResult: [],
            callbackFunctionArg: null,
            callbackFunctionName: null,
            callbackFunctionData: null,
            displayMessageOnSuccess: true,
            triggerSuccessEventOnSuccess: true,
            handleLoadingonCallback:false,
            success: true,
            errorBatchResponseResult: [],
            isMultiresponse: false
        });
    }
}
