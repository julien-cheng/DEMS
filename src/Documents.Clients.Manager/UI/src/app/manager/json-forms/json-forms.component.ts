import { Component, OnInit, ViewContainerRef, ViewChild } from '@angular/core';
import { ToastrService, Toast, ToastrConfig } from 'ngx-toastr';
import { SchemaForm, ICustomValidator, ISchemaFormOptions, Schema, SchemaFormsComponent } from '../../ng4-schema-forms/index';
import { sampleSchema, sampleSchemaData } from './sample-schema';
import { FormControl } from '@angular/forms/src/model';

@Component({
  templateUrl: './json-forms.component.html',
  styleUrls: ['./json-forms.component.scss']
})
export class JsonFormsComponent implements OnInit {
  public folderKey: string;

  // Pass Schema Form Options:
  public schemaFormOptions: ISchemaFormOptions;
  public initialFormValue: any;
  public schema: any;
  customValidators: Array<ICustomValidator> = [];
  @ViewChild(SchemaFormsComponent) schemaFormComponent: SchemaFormsComponent;
  // public schemaForm: SchemaForm;
  constructor(private schemaForm: SchemaForm, private toastr: ToastrService) {}

  ngOnInit() {
    // Set Schema forms options - TEMP - retrieve from db
    this.schema = sampleSchema;

    // Set Value: Test Data to work with (pass data)
    this.initialFormValue = sampleSchemaData; // schemaData;

    // Add any custom validation in the option object:
    this.customValidators = [
      {
        name: 'customDateValidation',
        errorMessage: 'Test _customDateValidation error message - json-form.component',
        fn: this._customDateValidation
      },
      {
        name: 'customStringValidation',
        // errorMessage: null, //'Test _customStringValidation error message - json-form.component',
        fn: this._customStringValidation
      },
      {
        name: 'customNumberValidation',
        errorMessage: 'Test _customNumberValidation error message - json-form.component',
        fn: this._customNumberValidation
      }
    ];
    this.schemaForm.setCustomValidation(this.customValidators);

    // Schema Options - set schema, initial values and ANY Custom validation
    this.schemaFormOptions = {
      schema: this.schema,
      initialFormValue: this.initialFormValue,
      customValidators: this.customValidators
    };

    // TEMP
    // setTimeout(() =>  this.schemaFormComponent.setFocus(), 0);
  }

  // Validate data - Sample Custom Validators:
  // -------------------------------------------------------------------------
  // Add custom validation on the component
  // fn: (value?: ValueType, schema?: Schema) => boolean or fn(...arg)
  _customDateValidation(control: FormControl) {
    // console.log('_customDateValidation', value);
    const value = control.value;
    // VALIDATE DATE in this block!!!!! and return isValid as true or false
    return value === '2017-11-11';
  }

  // ... arg is an array [controlvalue, schema]
  _customStringValidation(control: FormControl) {
    // console.log('_customStringValidation: ', control);
    // This validator returns valid (true) for strings === test
    // return (!!args[0] && args[0] === 'test') ? true : false;
    return !!control.value && control.value === 'test' ? true : false;
  }

  // ... arg is an array [controlvalue, schema]
  _customNumberValidation(control: FormControl) {
    // console.log('_customNumberValidation: ', args);
    // This validator returns valid (true) for strings === test
    const value = control.value;
    return !!value && value < 125 ? true : false;
  }
  // -------------------------------------------------------------------------

  // Bind events - Exposed methods
  // -------------------------------------------------------------------------
  onEvent(event: any) {
    //  console.log('onEvent at component level', event);
    // this.postMessage('This Form triggered an event: ' + event.eventName, 'warning');
  }

  // Add form instance specific code if needed (public method exposed)
  onSubmit(event: any) {
    console.log('***** onSubmit at component level', this.schemaFormComponent.form.value);
    //  this.postMessage('This Form triggered a submit event: ' + event.eventName, 'warning');
  }

  postMessage = function(msg: string, type: string) {
    switch (type) {
      case 'success':
        this.toastr.success(msg);
        break;
      case 'warning':
        this.toastr.warning(msg);
        break;
      case 'error':
        this.toastr.error(msg);
        break;
      default:
        break;
    }
  };
}
