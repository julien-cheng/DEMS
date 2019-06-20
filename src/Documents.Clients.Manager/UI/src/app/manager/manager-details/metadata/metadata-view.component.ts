import { Component, OnInit, Input } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { ISchemaFormOptions, Schema } from '../../../ng4-schema-forms';
import { IObjectView, IAllowedOperation, LoadingService } from '../../index';
import { FileService, IFile } from 'app/auth';

@Component({
  selector: 'app-metadata',
  templateUrl: './metadata-view.component.html',
  styleUrls: ['./metadata-view.component.scss']
})
export class MetadataViewComponent implements OnInit {
  @Input() allowedOperations: IAllowedOperation[];
  @Input() file: IFile;
  @Input() viewItemId = 'metadataView';
  // Pass Schema Form Options:
  public metadataViewTitle = '';

  public saveFormAllowedOperation: IAllowedOperation;
  public schemaFormOptions: ISchemaFormOptions;
  public collapsed = false;
  public columns = 1;
  public breakpoint = 'md';

  constructor(private toastr: ToastrService, private fileService: FileService, public loadingService: LoadingService) {}

  ngOnInit() {
    const schemaThing: Schema = {
      type: 'object',
      title: 'METADATA:',
      description: 'Edit file metadata',
      properties: {}
    };
    for (const key of Object.keys(this.file.attributes.Metadata.file)) {
      // Some simple detection of properties that we want
      if (key.startsWith('attribute.')) {
        schemaThing.properties[key] = {
          type: typeof JSON.parse(this.file.attributes.Metadata.file[key]) as any,
          title: key,
          description: key,
          placeholder: key
        };
      }
    }

    // This parses the metadata string format
    const hacky = Object.keys(this.file.attributes.Metadata.file).reduce((result, key) => {
      if (key.startsWith('attribute.')) {
        result[key] = JSON.parse(this.file.attributes.Metadata.file[key]);
      }
      return result;
    }, {});

    // Schema Options - set schema, initial values and ANY Custom validation
    this.schemaFormOptions = {
      schema: schemaThing,
      initialFormValue: hacky, // this.file.attributes.Metadata.file,
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

    // This reverts everything to a string format
    const hacky = Object.keys(event.schemaFormValues).reduce((result, key) => {
      if (key.startsWith('attribute.')) {
        result[key] = JSON.stringify(event.schemaFormValues[key]);
      }
      return result;
    }, {});
    console.log();
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
