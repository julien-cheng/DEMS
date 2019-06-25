// Schema API
// -------------------------------------------------------------------------
<ng4-schema-forms>
  some component inputs: 
  [options]="schemaFormOptions" 
  [setFocusOnload]="true" //Set the focus on first field
  [displaySubmitButton]="false" //do not display the default submit button - bind an external button and events
  submitButtonText="buttons text here!" // default button's text
  (event)="onEvent($event)" // Add external events
  (submit)="onSubmit($event)"  // Add external onSubmit events
  more...

Available events:
  initialized: 'initialized',
  updateData: 'updateData',
  event: 'event',
  stateChange: 'stateChange',
  validate: 'validate',
  validated: 'validated',
  complete: 'complete',
  success: 'success',
  error: 'error',
  cancel: 'cancel',
  submit: 'submit'


Follows json schema specifications:
http://json-schema.org/documentation.html
// -------------------------------------------------------------------------

TYPES by data type:
type Schema = IObjectSchema | IArraySchema | INumberSchema | IStringSchema | IBooleanSchema | INullSchema;

Format: for additional control type options: 
html5Format = 'color' | 'date' | 'datetime' | 'datetime-local' | 'time' | 'month' | 'email' | 'uri' | 'url' | 'week' | 'hostname' | 'ipv4' | 'ipv6' | 'code' | 'markdown';

// Custom Validators - Validate data - Sample Custom Validators:
// -------------------------------------------------------------------------
STEPS:
1. Add customValidatorKeys in schema of the objec to validate

// Add custom validation on the component  and pass the name on the schema as such:
customValidatorKeys: 'customStringValidation'
or as an array:
customValidatorKeys: ['customDateValidation', 'customStringValidation']


2. Add the Validation function in the component where the module is being initialized
// fn: (value?: ValueType, schema?: Schema) => boolean or fn(...arg)
for example:
 _customStringValidation(...args) {
    return (!!args[0] && args[0] === 'test' ) ? true: false;
  }
or
 _customStringValidation(value?: any, schema?:  Schema) {
    return (!!value && value === 'test' ) ? true: false;
  }

3. Register the customValidator in the Schema form options - for example: 
  - Server side error message overrides this objects errorMessage
  - last resort default cath all in DefaultSettings (api.ts)
  this.schemaForm.setCustomValidation( [
        {
          name:'customDateValidation',
          errorMessage: 'Test _customDateValidation error message',
          fn: this._customDateValidation
        },
        {
          name:'customStringValidation',
          errorMessage: 'Test customStringValidation error message',
          fn: this._customStringValidation
        },
        {
          name:'_customNumberValidation',
          errorMessage: 'Test _customNumberValidation error message',
          fn: this._customNumberValidation
        }
      ];
  );

  // Schema Options - set schema, initial values and ANY Custom validation
  this.schemaFormOptions = {
    schema: this.schema,
    initialFormValue: this.initialFormValue,
    customValidators: this.customValidators
  };