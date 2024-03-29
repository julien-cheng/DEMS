import { Component, OnInit, Input } from '@angular/core';
import { ToastsManager } from 'ng2-toastr';
import { SchemaForm, ICustomValidator, ISchemaFormOptions, Schema, SchemaFormsComponent } from '../../../ng4-schema-forms';
import { IObjectView, IAllowedOperation, batchOperationsDefaults, LoadingService, FolderService } from '../../index';

@Component({
    selector: 'object-view',
    templateUrl: './object-view.component.html',
    styleUrls: ['./view-types.component.scss']
})
export class ObjectViewComponent implements OnInit {
    @Input() viewItem: IObjectView;
    @Input() viewItemId: string = 'viewItemId';
    // Pass Schema Form Options:
    public objectViewTitle: string = '';
    public allowedOperations: IAllowedOperation[];
    public saveFormAllowedOperation: IAllowedOperation;
    public schemaFormOptions: ISchemaFormOptions;
    public schema: any;
    public collapsed?: boolean = false;
    public columns: number = 1;
    public breakpoint: string = 'md';

    constructor(
        private toastr: ToastsManager,
        private folderService: FolderService,
        public loadingService: LoadingService) { }

    ngOnInit() {
        this.objectViewTitle = this.viewItem.title;
        this.allowedOperations = this.viewItem.allowedOperations;
        this.schema = this.viewItem.dataSchema;
        this.collapsed = this.viewItem.dataSchema.isCollapsed;
        this.columns = this.viewItem.dataSchema.columns || this.columns;
        this.breakpoint = this.viewItem.dataSchema.breakpoint || this.breakpoint;
        // console.log(this.viewItem);

        // Schema Options - set schema, initial values and ANY Custom validation
        this.schemaFormOptions = {
            schema: this.schema,
            initialFormValue: this.viewItem.dataModel,
            customValidators: null
        };

        !!this.allowedOperations && this.setSaveAllowedOperation();
    }

    private setSaveAllowedOperation() {
        this.allowedOperations.forEach(
            (operation) => {
                (operation.batchOperation.type === 'SaveRequest') && (this.saveFormAllowedOperation = operation);
            }
        );
    }

    // Bind events - Exposed methods
    // -------------------------------------------------------------------------
    // Add form instance specific code if needed (public method exposed)
    onSchemaFormSubmit(event: any) {
        if (!!this.saveFormAllowedOperation) {
            let data = Object.assign({}, this.saveFormAllowedOperation.batchOperation, { data: event.schemaFormValues });
            this.loadingService.setLoading(true);
            this.folderService.saveFolderData(data).subscribe(
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
