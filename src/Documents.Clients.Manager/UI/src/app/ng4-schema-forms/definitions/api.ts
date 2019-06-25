import { ValidatorFn, ValidationErrors } from '@angular/forms';

/**
 * Ng4SchemaFormsModule module
 *
 * Description: Controls based on 6 primitive types: obj, array, number, string, boolean, null (display controls)
 * String: text box or HTML5 input types/controls - format param will determine this type
 * number:
 * boolean: select box with options "true" and "false. To use a checkbox instead, set the format to checkbox.
 * array: Enumerated strings, you can also use the select or checkbox format
 *        defaults to checkbox editor (multiple checkboxes) will be used if there are fewer than 8 enum options.
 *        Otherwise, the select editor (a multiselect box) will be used.
 * Object: lists and child editors - object layout is one child editor per row
 *
*/

// Global CONST:
// ----------------------------------------------------
export const SCHEMAFORMS_EVENTS = {
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
    schemaFormSubmit: 'schemaFormSubmit'
};

// TYPES:
// ----------------------------------------------------
export type ControlType = 'string' | 'array' | 'object' | 'boolean' | 'null' | 'number' | 'integer';
export type ValueType = { [name: string]: any } | any[] | number | boolean | string | null;
export type Schema = IObjectSchema | IArraySchema | INumberSchema | IStringSchema | IBooleanSchema | INullSchema;
// Use this type to define control types:
export type html5Format = 'color' | 'date' | 'datetime' | 'datetime-local' | 'time' | 'month' | 'email' | 'uri' | 'url' | 'week' | 'hostname' | 'ipv4' | 'ipv6' | 'code' | 'markdown';
export type FormControlType = 'textarea' | 'input' | 'radio' | 'checkbox' | 'select' | html5Format;
export type ValidityValue<T> = {
    value: T;
};

// INTERFACES: Working interface
// ----------------------------------------------------
export interface ISchemaFormOptions {
    schema: Schema;
    initialFormValue: any;
    customValidators?: Array<ICustomValidator>;
    events?: (...params) => any | void; // TBD
}

export interface IBaseControlOptions {
    value?: ValueType;
    key?: string;
    label?: string;
    required?: boolean;
    order?: number;
    controlType?: string;
    schema: Schema;
}

// CUSTOM VALIDATION Notes:
// Add a custom validator on the component and match the name of the validator in the schema
// add customValidator in component  like so:{name:string, errorMessage: string, fn: [validation function()]}
//      this.customValidators.push({ name: 'customStringValidation', errorMessage: 'Test _customStringValidation error message', fn: this._customStringValidation });
export interface IBaseSchema {
    type: ControlType;
    title?: string;
    description?: string;
    default?: ValueType;
    readonly?: boolean;
    required?: boolean;
    properties?: { [name: string]: Schema };
    propertyOrder?: number;
    optional?: boolean;
    format?: FormControlType;
    validators?: IValidator[];
}

// Base Schema Types:
export interface IStringSchema extends IBaseSchema {
    type: 'string';
    enum?: string[][];
    minLength?: number;
    maxLength?: number;
    pattern?: string;
    placeholder?: string; // For select controls (enum !=undefined) placeholder will be an optional top choice (select one etc...)
}

export interface IBooleanSchema extends IBaseSchema {
    type: 'boolean';
}

export interface INumberSchema extends IBaseSchema {
    type: 'number' | 'integer';
    minimum?: number;
    exclusiveMinimum?: boolean;
    maximum?: number;
    exclusiveMaximum?: boolean;
    enum?:Array<Array<any>>;
    multipleOf?: number;
};

export interface IArraySchema extends IBaseSchema {
    type: 'array';
    items: Schema;
    minItems?: number;
    uniqueItems?: boolean;
    collapsed?: boolean;
}

export interface IObjectSchema extends IBaseSchema {
    type: 'object';
    properties: { [name: string]: Schema };
    requiredProperties?: string[];
    maxProperties?: number;
    minProperties?: number;
    collapsed?: boolean;
}

export interface INullSchema extends IBaseSchema {
    type: 'null';
}

// Validation
// ----------------------------------------------------
// From schema:
export interface IValidator {
    type: string;
    value: string;
    errorMessage: string;
}

// Build custom validator for ng and schema use
export interface ICustomValidator {
    name: string;
    errorMessage?: string;
    fn: (value?: ValueType, schema?: Schema) => boolean;
};

// Validator types:
export interface IValidationTypes {
    required?: boolean;
    minLength?: number; // for strings # of characters
    maxLength?: number; // for strings # of characters
    min?: number; // Validator that requires controls to have a value greater than a number.
    max?: number; // Validator that requires controls to have a value less than a number.
    pattern?: string | RegExp;
    unique?: boolean;
    requiredTrue?: boolean;
    custom?: string; // pass key to custom validation fn
    //     minProperties?: number; -> replace with min ***** TBD for Obj/array
    //     maxProperties?: number;-> replace with max ***** TBD for Obj/array
}

// export type validatorWithParams = 'minLength' | 'maxLength' | 'min' ;
// Interface that ng uses for passing data back ***** working
// Object returned form ValidationFn different by type
export interface IValidatorValueKeys {
    type?: string;
    name?: string;
    requiredLength?: number;
    actualLength?: number;
    min?: number;
    max?: number;
    lessThan?: number; // greater or equal than a number ( leave for future handling of datetime values)
    greaterThan?: number; // greater or equal than a number ( leave for future handling of datetime values)
    actual?: number;
    requiredPattern?: string | RegExp;
    actualValue?: string;
    errorMessage?: string;
}

// Labels, errors and theme variables
// ----------------------------------------------------
// Need to workour the placeholders {0} - label, {1} - label,  {2} - value
// nullvalidator: 'No-op value',  // NG driven: nullValidator: No-op validator - not needed here - returns null
export const DefaultSettings = {
    error: {
        min: 'Value must be larger or equal than <b>{2}</b>.',  // NG driven: Validator that requires controls to have a value greater than a number.
        max: 'Value must be less or equal than <b>{2}</b>.',  // NG driven: Validator that requires controls to have a value less than a number.
        required: '<b>{0}</b> is required',// NG driven: Validator that requires controls to have a non-empty value.
        requiredTrue: '<b>{0}</b> is required to be set to true', // NG driven: Validator that requires control value to be true.
        minLength: 'Value must be at least <b>{2}</b> characters long.',  // NG driven: Validator that requires controls to have a value of a minimum length.
        maxLength: 'Value must be at most <b>{2}</b> characters long.',  // NG driven: Validator that requires controls to have a value of a maximum length.
        pattern: 'Value doesn\'t match the pattern <b>{2}</b>.', // NG driven: Validator that requires a control to match a regex to its value.
        unique: 'The item in <b>{0}</b> and <b>{1}</b> must not be same.', // array types
        greaterThan: 'Value must be larger than <b>{2}</b>.', // number and integer types / keep for future datetime types inclusion
        lessThan: 'Value must be less than <b>{2}</b>.', // number and integer types  / keep for future datetime types inclusion
        custom: 'There was a problem with {0}.'
        // email: '<b>{0}</b> must be in an email format', // NG driven: Validator that performs email validation. -> will have pattern for this one TBD
        // minimum: 'Value must be larger or equal than <b>{2}</b>.', // number and integer types
        // maximum: 'Value must be less or equal than <b>{2}</b>.', // number and integer types
        // largerThan: 'Value must be larger than <b>{2}</b>.', // number and integer types
        // smallerThan: 'Value must be less than <b>{2}</b>.', // number and integer types
        // minItems: 'The length of the array must be >= <b>{2}</b>.', // array types
        // multipleOf: 'Value must be multiple value of <b>{2}</b>.', // number and integer types
        // minProperties: 'Properties count must be >= <b>{2}</b>.', // object types
        // maxProperties: 'Properties count must be <= <b>{2}</b>.', // object types
    }
    // icons: { // May not be needed
    //     search: 'fa fa-search',
    //     collapseUp: 'fa fa-angle-up',
    //     collapseDown: 'fa fa-angle-down'
    // }
};
