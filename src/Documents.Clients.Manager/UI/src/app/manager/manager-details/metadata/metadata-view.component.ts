import { Component, OnInit, Input } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { ISchemaFormOptions, Schema } from '../../../ng4-schema-forms';
import { IObjectView, IAllowedOperation, LoadingService } from '../../index';
import { FileService, IFile, batchOperationsDefaults } from 'app/auth';

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
  public collapsed = true;
  public columns = 1;
  public breakpoint = 'md';

  constructor(private toastr: ToastrService, private fileService: FileService, public loadingService: LoadingService) {}

  ngOnInit() {
    // This parses the metadata string format
    const hacky = Object.keys(this.file.dataModel.properties).reduce((result, key) => {
      result[key] = JSON.parse(this.file.attributes[key] || '""');
      return result;
    }, {});

    // Schema Options - set schema, initial values and ANY Custom validation
    this.schemaFormOptions = {
      schema: this.file.dataModel,
      initialFormValue: hacky, // this.file.attributes.Metadata.file,
      customValidators: null
    };

    if (!!this.allowedOperations) {
      this.setSaveAllowedOperation();
    }
  }

  private setSaveAllowedOperation() {
    console.log(this.allowedOperations);
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
    // This reverts everything to a string format
    const hacky = Object.keys(event.schemaFormValues).reduce((result, key) => {
      result[key] = JSON.stringify(event.schemaFormValues[key]);
      return result;
    }, {});

    if (!!this.saveFormAllowedOperation) {
      const data = Object.assign({}, this.saveFormAllowedOperation.batchOperation, { data: hacky });
      data.fileIdentifier = this.file.identifier;

      this.loadingService.setLoading(true);
      this.fileService.setFileMetadata(data).subscribe(
          (result) => {
              this.toastr.success('The case information was saved successfully');
          },
          (error) => {
              console.error(error);
              this.toastr.error(batchOperationsDefaults.errorMessages.BaseRequest);
          },
          () => {
              this.loadingService.setLoading(false);
          }
      );
    }
  }
}
