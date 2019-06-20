import { Component, OnInit, Input } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { SchemaForm, ICustomValidator, ISchemaFormOptions, Schema, SchemaFormsComponent } from '../../../ng4-schema-forms';
import { IObjectView, IAllowedOperation, batchOperationsDefaults, LoadingService, FolderService } from '../../index';
import { FileService, IFile } from 'app/auth';

const sampleSchema = {
  type: 'object',
  title: 'GUI:',
  description: 'Add New Case',
  properties: {
    folderKey: {
      type: 'string',
      title: 'Case Identifier',
      description: 'Unique case identifier',
      placeholder: 'Case Identifier',
      validators: [
        {
          type: 'required',
          value: null,
          errorMessage: 'The field Case Identifier is required'
        }
      ]
    }
  }
};

@Component({
  selector: 'app-metadata',
  templateUrl: './metadata-view.component.html',
  styleUrls: ['./metadata-view.component.scss']
})
export class MetadataViewComponent implements OnInit {
  @Input() allowedOperations: IAllowedOperation[];
  @Input() viewItem: IObjectView;
  @Input() file: any;
  @Input() viewItemId = 'viewItemId';
  // Pass Schema Form Options:
  public metadataViewTitle = '';

  public saveFormAllowedOperation: IAllowedOperation;
  public schemaFormOptions: ISchemaFormOptions;
  public schema: any;
  public collapsed = false;
  public columns = 1;
  public breakpoint = 'md';

  constructor(private toastr: ToastrService, private fileService: FileService, public loadingService: LoadingService) {}

  ngOnInit() {
    // this.metadataViewTitle = this.viewItem.title;
    this.schema = sampleSchema; // || this.viewItem.dataSchema;

    console.log(this.file);
    // Schema Options - set schema, initial values and ANY Custom validation
    this.schemaFormOptions = {
      schema: this.schema,
      initialFormValue: null, // this.viewItem.dataModel,
      customValidators: null
    };

    if (!!this.allowedOperations) {
      this.setSaveAllowedOperation();
    }
  }

  private setSaveAllowedOperation() {
    this.allowedOperations.forEach(operation => {
      if (operation.batchOperation.type === 'SaveRequest') {
        this.saveFormAllowedOperation = operation;
      }
    });
  }

  // Bind events - Exposed methods
  // -------------------------------------------------------------------------
  // Add form instance specific code if needed (public method exposed)
  onSchemaFormSubmit(event: any) {
    this.toastr.error('THIS IS NOT IMPLEMENTED YET');
    if (!!this.saveFormAllowedOperation) {
      const data = Object.assign({}, this.saveFormAllowedOperation.batchOperation, { data: event.schemaFormValues });
      this.loadingService.setLoading(true);

      // this.fileService.setFileMetadata(data).subscribe(
      //     (result) => {
      //         this.toastr.success('The case information was saved successfully');
      //     },
      //     (error) => {
      //         console.error(error);
      //         this.toastr.error(batchOperationsDefaults.errorMessages.BaseRequest);
      //     },
      //     () => {
      //         this.loadingService.setLoading(false);
      //     }
      // );
    }
  }
}
