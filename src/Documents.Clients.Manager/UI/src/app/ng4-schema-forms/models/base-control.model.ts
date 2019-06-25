/* tslint:disable */
// Place UI params in this option object (changeable on init)
//import { scheduleMicroTask } from '@angular/core/src/util';
import { FormGroup, FormControl } from '@angular/forms';
import { AbstractControl } from '@angular/forms/src/model';
import { Validation, ICustomValidator, ValueType, IValidator } from '../index';
import { Schema, ControlType, FormControlType, IBaseControlOptions, IBaseSchema, IArraySchema, IBooleanSchema, INullSchema, IObjectSchema, IStringSchema, INumberSchema } from '../index';
import * as _ from 'lodash';
const { find, isObject } = _;

export class BaseControl implements IBaseControlOptions {
    private type: ControlType;
    initialValue?: ValueType;
    defaultValue?: ValueType;
    value?: ValueType;
    key: string;
    label: string;
    description: string;
    required: boolean;
    order: number;
    controlType: ControlType; // based on primary data type
    schema: Schema;
    formControlType?: FormControlType;
    validator: Validation = new Validation();
    //schemaValidators:  IValidator[];
    // customValidatorKeys?: string | Array<string>;
    private _isFocused: boolean;
    get isFocused(): boolean {
        return this._isFocused;
    }
    set isFocused(focused: boolean) {
        this._isFocused = focused;
    }

    constructor(
        key: string,
        controlSchema: Schema,
        formCustomValidators?: Array<ICustomValidator>,
        initialValues?: ValueType,
    ) {
        this.schema = controlSchema;
        this.type = this._convertToControlType(this.schema.type);
        this.key = key;
        this.initialValue = initialValues;
        this.defaultValue = this.schema.default;
        this._getControlType(this.schema);
        this._setFormControlInformation();
        this.validator.setValidationOptions(this.schema.validators, formCustomValidators);
        this.required = this.validator.isRequired;
    }

    // Modify db control types to ControlType
    private _convertToControlType(managerString: string): ControlType {
        // console.log(managerString);
        let type: ControlType = 'null';
        //ControlType = 'string' | 'array' | 'object' | 'boolean' | 'null' | 'number' | 'integer
        switch (managerString) {
            case 'ManagerFieldBoolean':
            case 'boolean':
                type = 'boolean'
                break
            case 'ManagerFieldInt':
            case 'ManagerFieldDecimal':
            case 'number':
            case 'integer':
                type = 'number'
                break
            case 'ManagerFieldArray':
            case 'array':
                type = 'array'
                break
            case 'ManagerFieldObject':
                type = 'object'
                break
            case 'ManagerFieldNull':
            case 'null':
                type = 'null'
                break
            default:
                type = 'string'
                break;
        }
        return type;
    }

    // Description: Get control type and cast schema
    private _getControlType(schema: Schema) {
        switch (this.type) {
            case 'object':
                this.schema = <IObjectSchema>schema;
                this.controlType = 'object';
                break;
            case 'array':
                this.schema = <IArraySchema>schema;
                this.controlType = 'array';
                break;
            case 'null':
                this.schema = <INullSchema>schema;
                this.controlType = 'null';
                break;
            case 'number':
            case 'integer':
                this.schema = <INumberSchema>schema;
                this.controlType = 'number';
                break;
            case 'boolean':
                this.schema = <IBooleanSchema>schema;
                this.controlType = 'boolean';
                break;
            default: // Default to string
                this.schema = <IStringSchema>schema;
                this.controlType = 'string';
                break;
        }
    }

    // Description: Set controls label from title or key ***** working
    private _setFormControlInformation() {
        this.label = this.schema.title !== undefined ? this.schema.title : this.key; //this.key;
        this.description = this.schema.description && this.schema.description;
        this.formControlType = this.schema.format || this._getformControlType(this.schema.type);
        this.isFocused = false;
        //Set Default values in object Form controls from children when not passed in schema
        (this.type === 'object' && this.defaultValue === undefined) && (this.defaultValue = this.buildObjectValueType(this.schema));

        //Set value dependent on Control/data type
        // this.value = this.getDefaultValueByType(this.type);
        const inVal = (this.initialValue !== undefined) ? this.initialValue :
            (this.defaultValue !== undefined) ? this.defaultValue : undefined;
        this.value = this.getDefaultValueByType(this.type, inVal);
        // console.log('SET VALUE: ', this.value);
        // console.log('--------');
    }

    // Description: Adjust complex editors: Object and arrays  (for proper validation)
    // For Object - needs to get values from properties defaults if it is not declared in parent schema
    // For Arrays - needs to get set value to an empty array
    private getDefaultValueByType(type: ControlType, inValue: ValueType) {
        let val: ValueType;
        switch (type) {
            case 'object':
                // console.log(this.key);
                val = (Array.isArray(inValue) && isObject(inValue[0])) ? inValue[0] :
                    isObject(inValue) ? inValue : {};
                // this.defaultValue = (this.type === 'object' && this.defaultValue === undefined)?this.buildObjectValueType(this.schema) : {};
                break;
            case 'array':
                val = Array.isArray(inValue) ? inValue : [];
                break;
            case 'number':
            case 'integer':
                val = (typeof inValue === "number" || !isNaN(Number(inValue))) ? Number(inValue) : null;
                break;
            case 'boolean':
                val = (typeof inValue === "boolean") ? inValue :
                    (!!this.schema.optional && this.schema.optional) ? null : false;
                break;
            case 'string':
                val = (typeof inValue === "string") ? inValue : '';
                break;
            case 'null':
            default:
                val = (inValue === null) ? inValue : null;
                break;
        }
        // console.log(val);
        return val;
    }

    // Gets Object type default value from properties defaults iterate through props get key: defaults pairs
    // Format {key: value} example: {propertyExample2: "101", propertyExample1: "testing objects"}
    private buildObjectValueType(schema) {
        if (schema.default !== undefined) {
            return (Array.isArray(schema.default) && isObject(schema.default[0])) ? schema.default[0] :
                isObject(schema.default) ? schema.default : {};
        } else {
            let value: ValueType = {};
            for (let key in schema.properties) {
                const childSchema = schema.properties[key];
                value[key] = childSchema.default !== undefined ? childSchema.default : this.createNewEmptyValuebyDataType(childSchema.type);
            }
            this.defaultValue = value;
            return value;
        }
    }

    // Description: set default form control type if format is not included in schema;
    private _getformControlType(type: string): FormControlType {
        if (type === 'boolean') {
            return 'checkbox';
        } if (type === 'string' || type === 'number' || type === 'integer') {
            return 'input';
        } else {
            return null;
        }
    };

    // Description: Initialize control specific custom validators for the controls:
    private _initCustomControlValidation(formCustomValidators?: Array<ICustomValidator>): void {
        // Initialize control validation and Add customValidators to validation object
        // this.validator = new Validation();
        // let customValidatorKeys = !!this.schema.validators && this.schema.validators.customValidatorKeys; 
        // TEMPORARY UNTIL FULLY DEFINED IN SERVER-SIDE API -> TO BE DONE!!!!!
        //this.schema.validators.customValidatorKeys; 
        // if (customValidatorKeys && formCustomValidators) {
        //     let keysArray: Array<string> = !Array.isArray(customValidatorKeys) ? [customValidatorKeys] : customValidatorKeys;
        //     keysArray.forEach((obj) => {
        //         let addValidator = _.find(formCustomValidators, function (validator) {
        //             return validator.name === obj;
        //         });
        //         !!addValidator && this.validator.customValidators.push(Object.assign({}, addValidator));
        //     });
        // }
    }

    // Public methods:
    // ----------------------------------------------------------------------
    public setControlsValue(newValue: ValueType) {
        this.value = newValue;
    }

    public getObjectSchemaProperties() {
        return this.schema.properties;
    }
    public setObjectSchemaProperties(properties: any) {
        this.schema.properties = properties;
    }
    // Description: Validate control
    public validateControl(ngFormControl: AbstractControl) {
        this.validator.validateControl(this.value, this.schema, ngFormControl);
    }

    // Description: Create a new empty value by type: String: '', array'', object etc
    public createNewEmptyValuebyDataType(type: ControlType) {
        switch (type) {
            case 'number':
            case 'integer':
                return null;
            case 'object':
                return {};
            case 'array':
                return [];
            case 'boolean':
                return false;
            default:
                return '';
        }
    }
}

