import { Component, OnInit, Inject, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { NgForm, FormGroup, FormControl, Validators, FormBuilder } from '@angular/forms';
import { JQ_TOKEN, IOrganizationIdentifier, AuthService, FolderService, IFolder, AppConfigService, LoadingService } from '../index';
import { SchemaForm, ICustomValidator, ISchemaFormOptions, Schema, SchemaFormsComponent } from '../../ng4-schema-forms/index';

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
    } /*,
    docketNumber: {
      type: 'string',
      title: 'Docket Number',
      description: 'Docket Number: YEARNYXXXXXX',
      placeholder: 'Docket Number',
      validators: [
        {
          type: 'required',
          value: null,
          errorMessage: 'The field stringExample is required'
        }
      ]
    },
    indictmentNumber: {
      type: 'string',
      title: 'Indictment Number',
      description: 'Indictment Number: XXXXX/YEAR',
      placeholder: 'Indictment Number',
    },
    arrestNumber: {
      type: 'string',
      title: 'Arrest Number',
      description: 'Arrest Number: MYRXXXXXX',
      placeholder: 'Arrest Number',
    },
    trialNumber: {
      type: 'string',
      title: 'Trial Number',
      description: 'Trial Division Investigation Number: FYEAR-XXXXXXXX',
      placeholder: 'Trial Number',
    },
    ICMSNumber: {
      type: 'string',
      title: 'ICMS Number',
      description: 'ICMS Number (format post 2017): XXXXXX',
      placeholder: 'ICMS Number',
    },*/
  }
};

@Component({
  selector: 'app-case-list',
  templateUrl: './case-list.component.html',
  styleUrls: ['./case-list.component.scss']
})
export class CaseListComponent implements OnInit, AfterViewInit {
  @ViewChild(SchemaFormsComponent) schemaFormComponent: SchemaFormsComponent;
  public schemaForm: NgForm;
  public folderList: IFolder[];
  public organizationKey: IOrganizationIdentifier;
  // Pass Schema Form Options:
  public schemaFormOptions: ISchemaFormOptions;
  public schema: any;

  constructor(
    private folderService: FolderService,
    private route: ActivatedRoute,
    private router: Router,
    public appConfigService: AppConfigService,
    public loadingService: LoadingService,
    @Inject(JQ_TOKEN) private $: any
  ) {
    // Set top navigation to visible ***** Force it to true
    // this.appConfigService.setTopNavVisible(true);
  }

  ngOnInit() {
    this.route.params.subscribe((params: Params) => {
      this.organizationKey = params.organizationKey;
      this.getFolders();
      if (this.appConfigService.configuration.caseCreate) {
        this.schema = JSON.parse(this.appConfigService.configuration.caseCreate);
      } else {
        this.schema = sampleSchema;
      }
      this.schemaFormOptions = {
        schema: this.schema,
        initialFormValue: null,
        customValidators: null
      };
      this.schemaForm = this.schemaFormComponent.form;
    });
  }

  ngAfterViewInit() {
    const self = this;
    this.$('#createCaseModal')
      .on('shown.bs.modal', e => {
        self.schemaFormComponent.setFocus();
      })
      .on('hidden.bs.modal', e => {
        self.schemaForm.reset();
      });
  }

  // Description: gets folder list
  getFolders(): void {
    this.loadingService.setLoading(true);
    this.folderService.getAllFolders().subscribe(
      response => {
        this.folderList = response.response as IFolder[];
      },
      error => {
        throw new Error('Manager is undefined - redirect to error');
      },
      () => {
        this.loadingService.setLoading(false);
      }
    );
  }

  // Description: Submit form with new case folder information
  // Need to add regex validation for the different numbers and validation for folderkey formValues: any
  submitForm($event: Event) {
    // console.log(this.schemaForm);
    this.schemaFormComponent.triggerFormSubmit($event);
    // console.log('***** onSubmit at component level', this.schemaForm.schemaFormValues);
    // console.log(this.schemaForm.schemaFormValues);
    if (!!this.organizationKey) {
      const formValues = this.schemaFormComponent.schemaFormValues;
      const folderKey = formValues.folderKey !== undefined ? 'Defendant:' + formValues.folderKey : undefined;
      if (folderKey !== undefined) {
        const newFolder = {
          type: 'ManagerFolderModel',
          name: folderKey,
          identifier: {
            folderKey,
            organizationKey: this.organizationKey
          },
          fields: {
            docketNumber: formValues.docketNumber || null,
            arrestNumber: formValues.arrestNumber || null,
            trialNumber: formValues.trialNumber || null,
            icmsNumber: formValues.icmsNumber || null,
            indictmentNumber: formValues.indictmentNumber || null
          }
        };
        // Save New Case
        this.folderService.createNewCase(newFolder).subscribe(batchResponse => {
          // this.getFolders();
          // console.log('UPDATE FOLDER LIST', batchResponse.response.folderKey);
          this.$('#createCaseModal').modal('hide');
          const newFK = batchResponse.response.identifier.folderKey;
          const orgKey = batchResponse.response.identifier.organizationKey;
          this.router.navigate(['/manager/', orgKey, newFK]);
        });
      } else {
        console.error('new Case is undefined');
        throw new Error('submitForm Error- redirect to error');
      }
    } else {
      console.error('Organization Key is empty');
    }
  }
}
