import { Component, Input, Inject, Output, EventEmitter, SimpleChanges, ViewChild, ElementRef, OnChanges } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { FormGroupDirective, FormGroup, FormControl, Validators, NgForm, COMPOSITION_BUFFER_MODE } from '@angular/forms';
import {
  IManager,
  IFolderIdentifier,
  IPathIdentifier,
  IAllowedOperation,
  IBatchRequestOperations,
  EventType,
  IBatchOperation
} from '../../index';
import {
  batchOperationsDefaults,
  BatchRequestType,
  IRequestBatchData,
  BatchRequest,
  BatchResponse,
  RequestRecipientType
} from '../../index';
import { excludePatternValidator, ValidationPatterns } from '../../index';
import { JQ_TOKEN } from '../../services/jQuery.service';
import { BatchOperationService } from '../../services/batch-operation.service';
import { ExplorerService } from '../../services/explorer.service';
import { PathService } from '../../services/path.service';
import { LoadingService } from '../../services/loading.service';
import * as _ from 'lodash';
const { isEqual } = _;

@Component({
  selector: 'app-batch-operations',
  templateUrl: './batch-operations.component.html',
  styleUrls: ['./batch-operations.component.scss']
})
export class BatchOperationsComponent implements OnChanges {
  constructor(
    private toastr: ToastrService,
    public batchOperationService: BatchOperationService,
    private pathService: PathService,
    private explorerService: ExplorerService,
    public loadingService: LoadingService,
    @Inject(JQ_TOKEN) private $: any
  ) {}
  @ViewChild('newFolderModalInput') newFolderModalInput: ElementRef;
  @ViewChild('modalFocusInput') modalFocusInput: ElementRef;
  @ViewChild('publishModalFocusInput') publishModalFocusInput: ElementRef;
  @ViewChild('clipModalFocusInput') clipModalFocusInput: ElementRef;
  @ViewChild('exportClipForm') exportClipForm: NgForm;
  @Input() allowedOperations: IAllowedOperation[];
  @Input() pathIdentifier: IPathIdentifier;
  @Input() validators: { [key: string]: ValidationPatterns[] };
  @Output() successsCallback = new EventEmitter();
  @Output() errorCallback = new EventEmitter();
  @Output() updateViewCallback = new EventEmitter();
  @Input() requestTypesFlags: any = {};
  public downloadPayload: IBatchRequestOperations;
  public modalTreeExplorer: any;
  public batchOperationsDefaults = batchOperationsDefaults;

  // Forms:
  public maxLength = 250;

  // Add new path
  public suggestedNames: any;
  public newPathForm: FormGroup;
  public newPathName: FormControl;
  public pathIdentifierCntrl: FormControl;

  // FORMS :
  // Add new recipient and Add new officer (LEO)
  public modalSaving = false;
  private patternRegex: string; // This may come from the server
  public newRecipientForm: FormGroup;
  public newRecipientFName: FormControl;
  public newRecipientLName: FormControl;
  public newRecipientEmail: FormControl;
  public addRecipientResponse: any = null;
  public recipientType: RequestRecipientType; // = RequestRecipientType.eDiscovery; //Default to eDiscovery
  public recipientModalTemplate; // = batchOperationsDefaults.eDiscoveryModalTemplate;

  // Publish Custom Name
  public publishCustomName: FormGroup;
  public customName: FormControl;
  public editDiscoveryCustomName: any = null;

  // Export Clip and Frame
  isFrameExport = false; // switch to change text on modal

  // Export Media clips
  // ----------------------------------------------------------------------------------
  public filename: string;

  ngOnChanges(simpleChanges: SimpleChanges) {
    this._initBatchOperationComponents();
  }

  // Description: initialize the components based on the requestTypes for this view
  private _initBatchOperationComponents() {
    // console.log('_initBatchOperationComponents: ', this.requestTypesFlags);
    let initFn: string;
    Object.keys(this.requestTypesFlags).forEach(key => {
      initFn = '_init' + key;
      typeof (this as any)[initFn] === 'function' && (this as any)[initFn].call(this);
    });
  }

  // Init forms and modals
  // ----------------------------
  private _initMoveIntoRequest() {
    this.modalTreeExplorer = this.explorerService.processTreeData(this.explorerService.fileExplorer.currentExplorer, '_modal');
  }

  private _initNewPathRequest() {
    // console.log('_initNewPathRequest', this.validators);
    // Add New Path in-modal Form (every change due to regex validator from server)
    let allowedPatternRegex: string, excludePatternRegex: string;
    if (!!this.validators.pathNameValidationPatterns && this.validators.pathNameValidationPatterns.length) {
      this.validators.pathNameValidationPatterns.map((val: ValidationPatterns) => {
        val.isAllowed && !!val.pattern && (allowedPatternRegex = val.pattern);
        !val.isAllowed && !!val.pattern && (excludePatternRegex = val.pattern);
      });
    }

    const validatorArr = [Validators.required, Validators.maxLength(this.maxLength)];

    !!allowedPatternRegex && validatorArr.push(Validators.pattern(allowedPatternRegex));
    !!excludePatternRegex && validatorArr.push(excludePatternValidator(excludePatternRegex));

    // Add form controls:
    this.newPathName = new FormControl('', Validators.compose(validatorArr));
    this.pathIdentifierCntrl = new FormControl('', Validators.required);
    this.newPathForm = new FormGroup({
      newPathName: this.newPathName,
      pathIdentifierCntrl: this.pathIdentifierCntrl
    });

    if (this.pathIdentifier) {
      this.newPathForm.patchValue({
        pathIdentifierCntrl: JSON.stringify(this.pathIdentifier)
      });
    }
  }

  // Initialize forms for new recipient onload
  private _initAddRecipientRequest() {
    this.recipientType = RequestRecipientType.eDiscovery;
    this.recipientModalTemplate = this.batchOperationsDefaults.eDiscoveryModalTemplate;
    this._initAddRecipientModalForm();
  }

  // Initialize forms for new recipient onload
  // private _initNewRecipientModalForms() {
  private _initAddOfficerRequest() {
    this.recipientType = RequestRecipientType.leo;
    this.recipientModalTemplate = this.batchOperationsDefaults.leoModalTemplate;
    this._initAddRecipientModalForm();
  }

  // Load modal for Exporting media cip
  // private _initExportClipRequest() {
  //   console.log('_initExportClipRequest');
  //   // this.customName = new FormControl('');
  //   // this.publishCustomName = new FormGroup({ clipName: this.customName });
  // }

  // MODAL METHODS:
  // ---------------------------------------------
  private _initAddRecipientModalForm() {
    this.newRecipientFName = new FormControl('', [
      Validators.required,
      Validators.maxLength(this.maxLength),
      Validators.pattern(this.patternRegex)
    ]);
    this.newRecipientLName = new FormControl('', [
      Validators.required,
      Validators.maxLength(this.maxLength),
      Validators.pattern(this.patternRegex)
    ]);
    this.newRecipientEmail = new FormControl('', [Validators.required, Validators.email]);
    this.newRecipientForm = new FormGroup({
      newRecipientFName: this.newRecipientFName,
      newRecipientLName: this.newRecipientLName,
      newRecipientEmail: this.newRecipientEmail
    });
  }

  // Initialize Turnover/Forms
  private _initEditPackageNameRequest() {
    this._initCustomNametModalForms();
  }

  private _initPublishRequest() {
    this._initCustomNametModalForms();
  }

  private _initCustomNametModalForms() {
    this.customName = new FormControl('');
    this.publishCustomName = new FormGroup({ customName: this.customName });
  }

  // --------------------------------------------------------------------------------------
  // Batch Operations
  // --------------------------------------------------------------------------------------
  // public processBatchUiAction($event:any) {
  public processBatchUiAction(requestBatchData: IRequestBatchData) {
    // console.log('BatchOperationsComponent processBatchUiAction: ', requestBatchData)
    if (!!requestBatchData) {
      const batchResponse = this.batchOperationService.processBatchRequestAction(requestBatchData),
        batchRequest = this.batchOperationService.batchRequest;

      if (!!batchRequest.observableRequest) {
        this.loadingService.setLoading(true);
        // Default to BaseRequest
        const batchRequestFns = !!this.batchOperationService.batchRequestFns[batchRequest.requestType]
          ? this.batchOperationService.batchRequestFns[batchRequest.requestType]
          : this.batchOperationService.batchRequestFns['BaseRequest'];

        if (!!batchRequestFns) {
          batchRequest.observableRequest.subscribe(
            result => {
              batchResponse.setBatchResponse(result);
              const isAllSuccess = this._examineChildrenSuccessStatus(batchResponse);
              batchResponse.success && isAllSuccess
                ? this.onSuccessProcessBatchUiAction(batchRequest, batchResponse)
                : this.onErrorProcessBatchUiAction(batchRequest, batchResponse);
            },
            error => {
              // console.error(error);
              this.loadingService.setLoading(false);
              this.onErrorProcessBatchUiAction(batchRequest, batchResponse, error);
            },
            () => {
              // console.log(!batchResponse.handleLoadingonCallback);
              !batchResponse.handleLoadingonCallback && this.loadingService.setLoading(false);
              this.onCompleteProcessBatchUiAction(batchRequest, batchResponse);
            }
          );
        } else {
          this.postMessage('There was a problem with request. Please, try again!', 'error');
          console.error('processBatchUiAction: There was an error - batchRequestFns is undefined');
        }
      } else {
        try {
          // Trigger success call - Set batchResponse response
          batchResponse.setBatchResponse(this.batchOperationService.response);

          // Trigger success
          this.onSuccessProcessBatchUiAction(batchRequest, batchResponse);
          // Trigger complete call
          this.onCompleteProcessBatchUiAction(batchRequest, batchResponse);
        } catch (error) {
          this.onErrorProcessBatchUiAction(batchRequest, batchResponse, error);
        }
      }
    } else {
      this.postMessage('There was a problem with request. Please, try again!', 'error');
      console.error('processBatchUiAction: There was an error with the batch request operations');
    }
  }

  // Description: determine if batchResponse items failed - multicall or single call
  // Return false if single call and child response object is in error; else return true and post any error on toasters
  // If single call (!batchResponse.isMultiresponse) and batchResponse.batchResponseResult.length === batchResponse.errorBatchResponseResult.length => FALSE
  // else if batchResponse.isMultiresponse === true and batchResponse.batchResponseResult.length > batchResponse.errorBatchResponseResult.length  => TRUE
  private _examineChildrenSuccessStatus(batchResponse: BatchResponse) {
    // console.log('_examineChildrenSuccessStatus: ', batchResponse.batchResponseResult, batchResponse.errorBatchResponseResult, batchResponse.isMultiresponse);
    let isAllSuccess = true; // Single call go by child success state -> pass true or false
    !batchResponse.isMultiresponse &&
      batchResponse.errorBatchResponseResult.length > 0 &&
      batchResponse.batchResponseResult.length === batchResponse.errorBatchResponseResult.length &&
      (isAllSuccess = false);
    return isAllSuccess;
  }

  // Description: On success call for observables and direct calls
  private onSuccessProcessBatchUiAction(batchRequest: BatchRequest, batchResponse: BatchResponse) {
    //  console.log('onSuccessProcessBatchUiAction', batchResponse.displayMessageOnSuccess)
    const successFn = !!this.batchOperationService.batchRequestFns[batchRequest.requestType]
      ? this.batchOperationService.batchRequestFns[batchRequest.requestType].success
      : this.batchOperationService.batchRequestFns['BaseRequest'].success;
    batchResponse.triggerSuccessEventOnSuccess &&
      (successFn && typeof successFn === 'function') &&
      successFn.call(this, batchRequest, batchResponse);

    // Call View Local pass the batchResponse and any custom - params ...arg ?
    if (batchResponse.callbackFunctionName && typeof (this as any)[batchResponse.callbackFunctionName] === 'function') {
      (this as any)[batchResponse.callbackFunctionName].call(this, batchResponse, batchRequest);
    }

    batchResponse.displayMessageOnSuccess && this.postMessage(batchRequest.successMessage, 'success'); // Will add custom messages to model later
    // Pass custom message for each failed object
    if (batchResponse.errorBatchResponseResult.length > 0) {
      const err = batchRequest.errorMessage;
      batchResponse.errorBatchResponseResult.forEach(operation => {
        !operation.success && this.postMessage(err, 'error');
      });
    }
  }

  // Description: On error call for observables and direct calls
  private onErrorProcessBatchUiAction(batchRequest: BatchRequest, batchResponse: BatchResponse, error?: any) {
    !!error && !!error.error && batchResponse.setBatchResponse(error.error);
    const errorFn = this.batchOperationService.batchRequestFns[batchRequest.requestType]
      ? this.batchOperationService.batchRequestFns[batchRequest.requestType].error
      : this.batchOperationService.batchRequestFns['BaseRequest'].error;
    errorFn && typeof errorFn === 'function' && errorFn.call(this, batchRequest, batchResponse);

    // Call View Local pass the batchResponse and any custom - params ...arg ?
    if (batchResponse.errorCallbackFunctionName && typeof (this as any)[batchResponse.errorCallbackFunctionName] === 'function') {
      (this as any)[batchResponse.errorCallbackFunctionName].call(this, batchResponse, batchRequest);
    }

    this.modalSaving = false; // Reset modals loading buttons
    const err = batchRequest.errorMessage; // + (!!error && !!error.error && !!error.error.exception ? (" - " + error.error.exception) : ''); // error.error.exception will come in in the error property - need to add to toaster object?
    this.postMessage(err, 'error');
  }

  // Description: On complete/done (error or success) call for observables and direct calls  - needs to be called properly
  private onCompleteProcessBatchUiAction(batchRequest: BatchRequest, batchResponse: BatchResponse) {
    const completeFn = !!this.batchOperationService.batchRequestFns[batchRequest.requestType]
      ? this.batchOperationService.batchRequestFns[batchRequest.requestType].complete
      : this.batchOperationService.batchRequestFns['BaseRequest'].complete;
    if (completeFn && typeof completeFn === 'function') {
      completeFn.call(this.batchOperationService, batchRequest);
    }
  }

  // Description: send toastr with message ***** Abstract to a component - TBD
  public postMessage(msg: string, type: string) {
    switch (type) {
      case 'success':
        this.toastr.success(msg);
        break;
      case 'error':
        this.toastr.error(msg);
        break;
      default:
        this.toastr.warning(msg);
        break;
    }
  }

  // Output calls:
  // ---------------------------------------------
  public updateView(batchResponse?: BatchResponse) {
    const arg = !!batchResponse && batchResponse.hasOwnProperty('callbackFunctionArg') ? batchResponse.callbackFunctionArg : batchResponse;
    this.updateViewCallback.emit(arg);
  }

  // Description: open a specific modal from BatchOperations
  public openModal(batchResponse: BatchResponse, batchRequest?: BatchRequest) {
    const arg: { modalId: string; data?: any } = batchResponse.callbackFunctionArg,
      modalId = arg.modalId;
    // Run methods before opening modal
    if (!!arg.data && arg.data.hasOwnProperty('modalBeforeShowCallback')) {
      arg.data.modalBeforeShowCallback &&
        !!this[arg.data.modalBeforeShowCallback] &&
        typeof this[arg.data.modalBeforeShowCallback] === 'function' &&
        this[arg.data.modalBeforeShowCallback].call(this, batchResponse, batchRequest);
    }

    return (
      !!this.$(modalId) &&
      this.$(modalId)
        .modal()
        .off('shown.bs.modal') // remove previous events
        .on('shown.bs.modal', e => {
          // Focus on the Add new input
          if (!!arg.data && arg.data.hasOwnProperty('modalFocusInput')) {
            const modalFocusInput = arg.data.modalFocusInput;
            this[modalFocusInput] && this[modalFocusInput].nativeElement.focus();
          }

          // Trigger modalOnShownCallback
          if (!!arg.data && arg.data.hasOwnProperty('modalOnShownCallback')) {
            arg.data.modalOnShownCallback &&
              !!this[arg.data.modalOnShownCallback] &&
              typeof this[arg.data.modalOnShownCallback] === 'function' &&
              this[arg.data.modalOnShownCallback].call(this, batchResponse);
          }
        })
        .off('hidden.bs.modal')
        .on('hidden.bs.modal', e => {
          // Reset Modal forms on close/hide  // console.log('OnHidden fn', this[modalForm]);
          if (!!arg.data && arg.data.hasOwnProperty('modalForm')) {
            const modalForm = arg.data.modalForm;
            this[modalForm] && this[modalForm].reset();
          }

          // Trigger OnHiddenCallback
          if (!!arg.data && arg.data.hasOwnProperty('modalOnHiddenCallback')) {
            arg.data.modalOnHiddenCallback &&
              !!this[arg.data.modalOnHiddenCallback] &&
              typeof this[arg.data.modalOnHiddenCallback] === 'function' &&
              this[arg.data.modalOnHiddenCallback].call(this, batchResponse);
          }
        })
    );
  }
  // Description: close a specific modal from BatchOperations
  public hideModal(arg: { modalId: string; data?: any }) {
    const modalId = arg.modalId;
    return !!this.$(modalId) && this.$(modalId).modal('hide');
  }

  // -----------------------------------------------------------------------------------
  // Specific Batch Operation Request Methods
  // -----------------------------------------------------------------------------------
  // Add New Path Modal:
  // -------------------------------------------------------------
  // Description: Create new path modal - Load Suggested path
  private loadCreateFolderModal(batchResponse: BatchResponse) {
    this.batchOperationService.modalLoading = true;
    const pathKey = this.pathIdentifier.pathKey ? this.pathIdentifier.pathKey : 'Path:';
    return this.pathService.pathSuggest(this.pathIdentifier).subscribe(batchResponse => {
      this.suggestedNames = batchResponse.response;
      this.$('#createPathModal')
        .modal()
        .on('shown.bs.modal', e => {
          this.newFolderModalInput.nativeElement.focus(); // Focus on the Add new input
        })
        .on('hidden.bs.modal', e => {
          this.newPathForm.reset();
          this.modalSaving = false;
          this.batchOperationService.modalLoading = false;
          this.batchOperationService.destroy();
        });
    });
  }

  // Description: Create new path - Modal methods
  public saveNewPath(formValues: any) {
    this.modalSaving = true;
    const newName = formValues.name !== undefined ? formValues.name : formValues.newPathName ? formValues.newPathName : undefined; // should be undefined to trigger error

    if (newName !== undefined) {
      // Save New Path here
      this.pathService.createPath(newName, this.pathIdentifier).subscribe(batchResponse => {
        this.$('#createPathModal').modal('hide');
        this.modalSaving = false;
        this.updateView(null);
      });
    } else {
      console.error('newName is undefined');
      this.modalSaving = false;
      throw new Error('saveNewPath Error- redirect to error');
    }
  }

  // Turn Over/Publish Request
  // -------------------------------------------------------------
  // Description: Save Publish request and save custom name  // (requestBatchData.requestType === 'EditPackageNameRequest') {
  public saveCustomName(formValues: any, requestBatchData: IRequestBatchData) {
    this.modalSaving = true;
    requestBatchData.batchOperations[0] = Object.assign(requestBatchData.batchOperations[0], formValues);
    this.loadingService.setLoading(true);
    this.hideModal({ modalId: '#turnOverModal' });
    return this.processBatchUiAction(requestBatchData);
  }

  // Download Zipped files (Batch native)
  // ---------------------------------------------------
  public submitbatchNativeForm(batchResponse: BatchResponse) {
    const operationsBatch: IBatchRequestOperations = batchResponse.callbackFunctionArg;
    this.downloadPayload = operationsBatch;
  }

  // AddRecipientRequest and AddOfficerRequest join methods and View Recipient
  // -------------------------------------------------------------
  // Form submit to save new recipient for Ediscovery and New Officer request for LEO
  // Needs to be passed as a separate (IE11 bugs with on enter submit)
  public onEnterSaveNewRecipient(form: FormGroup, requestType: BatchRequestType = 'AddRecipientRequest') {
    this.saveNewRecipient(form);
  }

  // Save New recipient for Ediscovery
  public saveNewRecipient(form: FormGroup) {
    // console.log(this.recipientType);
    if (form.valid) {
      this.modalSaving = true;
      const folderIdentifier: IFolderIdentifier = {
        organizationKey: this.pathIdentifier.organizationKey,
        folderKey: this.pathIdentifier.folderKey
      };
      if (!!form.value) {
        const data = Object.assign({}, form.value, { folderIdentifier });
        const requestBatchData: IRequestBatchData = {
          requestType: this.recipientType !== RequestRecipientType.eDiscovery ? 'AddOfficerRequest' : 'AddRecipientRequest',
          eventType: EventType.send,
          options: {
            apiUrl: this.recipientType !== RequestRecipientType.eDiscovery ? '/api/leoupload/addofficer' : '/api/ediscovery/addrecipient',
            params: data
          }
        };
        return this.processBatchUiAction(requestBatchData);
      }
    } else {
      return false;
    }
  }

  // Description: Change the status of the modal to show second step -> magic link and password also the DONE button
  public newRecipientCreatedCallback(batchResponse: BatchResponse) {
    // console.log('newRecipientCreatedCallback', batchResponse);
    this.modalSaving = false;
    this.loadRecipientModalData(batchResponse);
    this.updateView(batchResponse);
  }

  // Description: Populate the form with a default recipient
  public AddRecipientFormDefaults(batchResponse: BatchResponse, batchRequest: BatchRequest) {
    // console.log('AddRecipientFormDefaults', batchRequest.batchOperations[0].defaults);
    !!batchRequest.batchOperations[0].defaults.firstName &&
      this.newRecipientFName.patchValue(batchRequest.batchOperations[0].defaults.firstName);
    !!batchRequest.batchOperations[0].defaults.lastName &&
      this.newRecipientLName.patchValue(batchRequest.batchOperations[0].defaults.lastName);
    !!batchRequest.batchOperations[0].defaults.email && this.newRecipientEmail.patchValue(batchRequest.batchOperations[0].defaults.email);
  }

  // Description: Reset the AddRecipient data obj - need to be reset on close addRecipient on CLose ->onHidden
  private resetAddRecipientModal(batchResponse: BatchResponse) {
    // console.log('resetAddRecipientModal', batchResponse);
    this.modalSaving = false;
    this.newRecipientForm.reset();
    return (this.addRecipientResponse = null);
  }

  // Description: load Recipients Data in modal
  private loadRecipientModalData(batchResponse: BatchResponse) {
    // console.log('loadRecipientModalData', batchResponse);
    const batchResponseResult = batchResponse.batchResponseResult[0];
    !!batchResponseResult && !!batchResponseResult.response && (this.addRecipientResponse = batchResponseResult.response);
  }

  // Description: getst the mailto string for eRecipient
  public getMailtoUri(addRecipientResponse: any): string {
    return this.recipientType !== RequestRecipientType.eDiscovery
      ? this._buildLeoEmail(addRecipientResponse)
      : this._buildDiscoveryEmail(addRecipientResponse);
  }

  // Description: Build eDiscovery custom email template
  private _buildDiscoveryEmail(addRecipientResponse: any) {
    let subject = 'Discovery package for People v. __________________________',
      body = 'Please modify as necessary for your case or practice style: \r\n\r\n';
    body += !!addRecipientResponse.name
      ? 'Attorney Name: ' + addRecipientResponse.name + ' \r\n\r\n'
      : 'Attorney Name: ' + addRecipientResponse.firstName + ' ' + addRecipientResponse.lastName + ' \r\n\r\n';
    body +=
      'Pursuant to CPL Article 240, here is a link to the discovery package for People v. __________________________.   To access the discovery package:  \r\n\r\n' +
      '     1.  Click on this link:  ' +
      addRecipientResponse.magicLink +
      ' \r\n\r\n' +
      '     2.  Enter your e-mail address: ' +
      addRecipientResponse.email +
      '\r\n\r\n' +
      '     3.  Your password will follow in a separate e-mail.  \r\n\r\n\r\n' +
      'Once logged in, click on the discovery package to view the files.   \r\n\r\n' +
      'The discovery package also contains a PDF manifest that lists all the files in the discovery package. Most of the files should be viewable in your web browser.  ' +
      'If necessary, you may also download the files.  \r\n\r\n ';
    const uri = 'mailto:' + addRecipientResponse.email + '?subject=' + encodeURIComponent(subject) + '&body=' + encodeURIComponent(body);
    return uri;
  }

  // Description: Build LEO Officers custom email template
  private _buildLeoEmail(addRecipientResponse: any) {
    let subject = 'Upload Files for People v. __________________________',
      body = !!addRecipientResponse.name
        ? 'Officer ' + addRecipientResponse.name + ' \r\n\r\n'
        : 'Officer ' + addRecipientResponse.firstName + ' ' + addRecipientResponse.lastName + ' \r\n\r\n';
    body +=
      'As discussed, here is a link that will let you upload your files related to People v __________________________. \r\n\r\n Your password will follow in a separate e-mail.  \r\n\r\n' +
      'To access to the case People v __________________________.   \r\n\r\n' +
      '     1.  Click on this link:  ' +
      addRecipientResponse.magicLink +
      ' \r\n\r\n' +
      '     2.  Enter your e-mail address: ' +
      addRecipientResponse.email +
      '\r\n\r\n' +
      '     3.  Your password (will follow in a separate e-mail).  \r\n\r\n\r\n' +
      'Once logged in, you will see a blank screen asking you to upload files.   \r\n\r\n' +
      'Thank you, ' +
      '  \r\n\r\n ';
    const uri = 'mailto:' + addRecipientResponse.email + '?subject=' + encodeURIComponent(subject) + '&body=' + encodeURIComponent(body);
    return uri;
  }
  // Description: load Recipients Data in modal
  private loadDiscoveryCustomNameData(batchResponse: BatchResponse, batchRequest: BatchRequest) {
    this.editDiscoveryCustomName = {
      customName: batchRequest.rows[0].customName
    };
  }

  // Description: Reset the editDiscoveryCustomName data obj - need to be reset on close editDiscoveryCustomName on CLose ->onHidden
  private resetDiscoveryCustomNameModal(batchResponse: BatchResponse) {
    return (this.editDiscoveryCustomName = null);
  }
  public saveMediaClipName(form: NgForm, requestBatchData: IRequestBatchData) {
    // this.modalSaving = true;
    requestBatchData.batchOperations[0] = Object.assign(requestBatchData.batchOperations[0], form.value);
    this.loadingService.setLoading(true);
    this.hideModal({ modalId: '#exportClipModal' });
    return this.processBatchUiAction(requestBatchData);
  }

  // Description: Destroy the data from the form
  public resetExportClipModal() {
    // console.log('resetExportClipModal');
    // this.modalSaving = false;
    this.exportClipForm.reset();
  }

  // Description: Load proper text on modal
  public setModalText(batchResponse: BatchResponse, batchRequest: BatchRequest) {
    this.isFrameExport = batchRequest.requestType === 'ExportFrameRequest';
  }
}
