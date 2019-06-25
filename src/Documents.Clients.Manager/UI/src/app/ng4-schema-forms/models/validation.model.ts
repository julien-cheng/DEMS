
import { Injectable } from '@angular/core';
import { AbstractControl, Validators, ValidatorFn, ValidationErrors } from '@angular/forms';
import { DefaultSettings, ICustomValidator, IValidatorValueKeys, IValidator } from '../index';
import { Schema, ValueType, ControlType, FormControlType, IBaseControlOptions, IBaseSchema, IArraySchema, IBooleanSchema, INullSchema, IObjectSchema, IStringSchema, INumberSchema } from '../index';
import { INTERNAL_BROWSER_PLATFORM_PROVIDERS } from '@angular/platform-browser/src/browser';
import * as _ from 'lodash';
const { isInteger } = _;

@Injectable()
export class Validation {
    public isRequired: boolean = false; // for ui required label and instructions
    public isValidated: boolean = false; // if false => status= untouched or notvalidated / if true => status= valid or error
    public errorMessages?: Array<string>;
    public formControlValidators?: IValidator[] = [];
    public validatorArr: ValidatorFn[] = [];
    private customValidators?: Array<ICustomValidator> = []; // the validators that belong to this base control
    // Description: is control valid
    private _isValid: boolean = true;
    get isValid(): boolean {
        return this._isValid;
    }
    set setValid(valid: boolean) {
        this._isValid = valid;
    }

    constructor(
    ) {
    }

    public setValidationOptions(schemaValidators?: IValidator[], formCustomValidators?: Array<ICustomValidator>) {
        // let customValidatorKeys: string | Array<string>; // get from schema now;
        // then _initValidatorFn:
        return !!schemaValidators && this._initValidatorFn(schemaValidators, formCustomValidators);
    }

    // Description: Set the initial validator functions:
    private _initValidatorFn(schemaValidators?: IValidator[], formCustomValidators?: Array<ICustomValidator>) {
        // Iterate through schemaValidators assign the right ValidationFn
        if (!!schemaValidators && schemaValidators.length) {
            schemaValidators.forEach((validator: IValidator, i: number) => {
                // If this is avaialable in angular validators:
                if (!!Validators[validator.type]) {
                    (validator.type === 'required') && (this.isRequired=true);
                    let newValFn: ValidatorFn = (validator.type === 'required' || validator.type === 'requiredTrue' || validator.type === 'email') ?
                        Validators[validator.type] : Validators[validator.type](validator.value);
                    this.validatorArr.push(newValFn);
                } else if (validator.type === 'custom') {  // else this is a custom validator:
                    let addValidator = Object.assign({}, _.find(formCustomValidators, (val) => { return val.name === validator.value; }));
                    this.customValidators.push(addValidator); // May not need this
                    let nameKey = 'customValidator_' + i,
                        valFn: ValidatorFn = (control) => {
                            let result = addValidator.fn.call(this, control);
                            let validatorResult: any = null; // if true return null
                            if (!result) {
                                validatorResult = {};
                                validatorResult[nameKey] = {
                                    type: 'custom',
                                    name: addValidator.name,
                                    errorMessage: addValidator.errorMessage
                                };
                            }
                            return validatorResult;
                        };
                    this.validatorArr.push(valFn);
                } else {
                    // console.log('NOT AN ANGULAR VALIDATOR or custom type: ', validator.type);
                    // unique -> array types
                }
                // and errorMessage -> server side overrides defaults in DefaultSettings
                this.formControlValidators.push(validator);
            });
        }
    }

    // Control level validation
    // ------------------------------------------------------
    public validateControl(value: any | undefined, schema: Schema, ngFormControl: AbstractControl) {
        const _self = this;
        let errorMessage: string;
        this.errorMessages = [];
        // Get Control specific errors:
        for (let validatorKey in ngFormControl.errors) {
            if (ngFormControl.errors.hasOwnProperty(validatorKey)) {
                // console.log(ngFormControl.errors[validatorKey].name);
                let vvk: IValidatorValueKeys = ngFormControl.errors[validatorKey];
                // Adjust key to 'custom'
                let key = this._isCustomValidator(validatorKey) ? vvk.name : validatorKey;
                errorMessage = this._getValidatorsErrorMessages(key, vvk, schema);
                (!!errorMessage && errorMessage.length) && this.errorMessages.push(errorMessage);
            }
        }
        this.setValid = !this.errorMessages.length;
        this.isValidated = true;
    }


    private _isCustomValidator(key: string): boolean {
        const patt = /customValidator_/g;
        return patt.test(key);
    }

    // Description: Return validator form the IValidator[] array by type: key
    private _getValidator(key: string, isCustom: boolean): IValidator {
        return _.find(this.formControlValidators, (val) => {
            return isCustom ? (val.value === key) : (val.type === key);
        });
    }

    // Description: Get the correct message from the api DefaultSettings object and return a string containing the message
    private _getValidatorsErrorMessages(key: string, value: IValidatorValueKeys, schema: Schema): string {
        let msg: string = '';
        const label: string = schema.title,
            type: string = schema.type,
            isCustom: boolean = (value.type === 'custom'),
            valObj: IValidator = this._getValidator(key, isCustom);
        // Make Key = custom for errorMessage
        isCustom && (key = 'custom');
        if (!!valObj && !!valObj.errorMessage) { // Default to server side message (overrides default when populated)
            msg = valObj.errorMessage;
        } else if (!!DefaultSettings.error[key]) { // else
            // console.log('DefaultSettings errorMessage: ');
            switch (key.toLowerCase()) {
                case 'required':
                case 'requiredtrue':
                case 'email':
                    msg = (type === 'boolean' && key === 'required') ? DefaultSettings.error['requiredtrue'].replace('{0}', label) :
                        DefaultSettings.error[key].replace('{0}', label);
                    break;
                case 'min': // min
                    msg = DefaultSettings.error[key].replace('{2}', value.min);
                    break;
                case 'max':  // max
                    msg = DefaultSettings.error[key].replace('{2}', value.max);
                    break;
                case 'minlength': // requiredLength
                case 'maxlength':
                    msg = DefaultSettings.error[key].replace('{2}', value.requiredLength);
                    break;
                case 'pattern':
                    msg = DefaultSettings.error[key].replace('{2}', value.requiredPattern);
                    break;
                case 'custom':
                    console.log(!!value.errorMessage);
                    msg = !!value.errorMessage ? value.errorMessage : DefaultSettings.error[key].replace('{0}', label);
                    break;
                default:
                    msg = DefaultSettings.error[key].replace('{0}', String(label)).replace('{2}', String(value));
                    break;
            }
        }
        // console.log('msg: ', msg);
        return msg;
    }

    // Description: run on form submit or custom call
    validateForm() {
        // console.log('validateForm');
    }

    // Description: unique validation - TB implemented
    private _isSame(value1: ValueType, value2: ValueType) {
        if (typeof value1 === 'string'
            || typeof value1 === 'number'
            || typeof value1 === 'boolean'
            || value1 === null
            || value1 === undefined) {
            return value1 === value2;
        }
        if (typeof value2 === 'string'
            || typeof value2 === 'number'
            || typeof value2 === 'boolean'
            || value2 === null
            || value2 === undefined) {
            return false;
        }
        if (Array.isArray(value1)) {
            if (Array.isArray(value2) && (value1 as ValueType[]).length === (value2 as ValueType[]).length) {
                for (let i = 0; i < (value1 as ValueType[]).length; i++) {
                    if (!this._isSame((value1 as ValueType[])[i], (value2 as ValueType[])[i])) {
                        return false;
                    }
                }
                return true;
            } else {
                return false;
            }
        }
        if (Array.isArray(value2)
            || Object.keys((value1 as { [name: string]: ValueType })).length !== Object.keys((value1 as { [name: string]: ValueType })).length) {
            return false;
        }
        for (const key in value1) {
            if (value1.hasOwnProperty(key) && !this._isSame((value1 as { [name: string]: ValueType })[key], (value2 as { [name: string]: ValueType })[key])) {
                return false;
            }
        }
        return true;
    }
}

    // public setValidatorStatus(status?: any, resetAllStatus: boolean = true): void {
    //     const reset = {
    //         isValid: true,
    //         isCustomValidatorValid: null,
    //         isDefaultValidatorValid: null
    //     };
    //     this.status = resetAllStatus ? Object.assign(reset, status) : Object.assign(this.status, status); // { ...stateObj, ...state };
    // };
    // // Description: run custom validators and return isValid
    // customValidateControl(value: any | undefined, schema: Schema): boolean {
    //     let isinvalidArr = [];
    //     if (this.customValidators.length) {
    //         // console.log('------------- ValidateControl: ', this.customValidators);
    //         this.customValidators.forEach((validator: CustomValidator) => {
    //             let result = validator.fn.call(this, value, schema);
    //             //  console.log('CUSTOM VALIDATOR RESULT: ', result);
    //             validator.isValid = result;
    //             (!result) && isinvalidArr.push(validator);
    //             // console.log('------------- CUSTOM VALIDATOR RESULT: ', result);
    //             return result;
    //         });
    //     }
    //     //  console.log('------------- ValidateControl: ', isinvalidArr.length);
    //     return isinvalidArr.length === 0;
    // }
    // Description: Validate string control and get Error message(s)
    // private _getErrorMessageOfString(value: string | undefined, schema: IStringSchema) {
    //     if (value !== undefined) {
    //         if (schema.minLength !== undefined
    //             && value.length < schema.minLength) {
    //             return DefaultSettings.error.minLength.replace('{0}', String(schema.minLength));
    //         }
    //         if (schema.maxLength !== undefined
    //             && value.length > schema.maxLength) {
    //             return DefaultSettings.error.maxLength.replace('{0}', String(schema.maxLength));
    //         }
    //         if (schema.pattern !== undefined
    //             && !new RegExp(schema.pattern).test(value)) {
    //             return DefaultSettings.error.pattern.replace('{0}', String(schema.pattern));
    //         }
    //     }
    //     return '';
    // }

    // Description: Validate Number controls and get Error message(s) - Needs rework:
    // private _getErrorMessageOfNumber(value: number | undefined, schema: INumberSchema) {
    //     if (value !== undefined) {
    //         // if (schema.minimum !== undefined) {
    //         //     if (schema.exclusiveMinimum) {
    //         //         if (value <= schema.minimum) {
    //         //             return DefaultSettings.error.min.replace('{0}', String(schema.minimum));
    //         //         }
    //         //     } else {
    //         //         if (value < schema.minimum) {
    //         //             return DefaultSettings.error.min.replace('{0}', String(schema.minimum));
    //         //         }
    //         //     }
    //         // }
    //         // if (schema.maximum !== undefined) {
    //         //     if (schema.exclusiveMaximum) {
    //         //         if (value >= schema.maximum) {
    //         //             return DefaultSettings.error.max.replace('{0}', String(schema.maximum));
    //         //         }
    //         //     } else {
    //         //         if (value > schema.maximum) {
    //         //             return DefaultSettings.error.max.replace('{0}', String(schema.maximum));
    //         //         }
    //         //     }
    //         // }
    //         // if (schema.multipleOf && schema.multipleOf > 0) {
    //         //     if (!isInteger(value / schema.multipleOf)) {
    //         //         return DefaultSettings.error.multipleOf.replace('{0}', String(schema.multipleOf));
    //         //     }
    //         // }
    //     }
    //     return '';
    // }

    // Description: Validate array editors and controls and get Error message(s)
    // private _getErrorMessageOfArray(value: any[] | undefined, schema: IArraySchema) {
    //     if (value !== undefined) {
    //         if (schema.minItems !== undefined) {
    //             if (value.length < schema.minItems) {
    //                 return DefaultSettings.error.min.replace('{0}', String(schema.minItems));
    //             }
    //         }
    //         if (schema.uniqueItems) {
    //             for (let i = 1; i < value.length; i++) {
    //                 for (let j = 0; j < i; j++) {
    //                     if (this._isSame(value[i], value[j])) {
    //                         return DefaultSettings.error.unique.replace('{0}', String(j)).replace('{1}', String(i));
    //                     }
    //                 }
    //             }
    //         }
    //     }
    //     return '';
    // }

    // Description: Validate object editors and controls and get Error message(s)
    // private _getErrorMessageOfObject(value: { [name: string]: ValueType } | undefined, schema: IObjectSchema) {
    //     // console.log(value);
    //     // console.log(schema);
    //     if (value !== undefined) {
    //         let length = 0;
    //         for (const key in value) {
    //             if (value.hasOwnProperty(key) && value[key] !== undefined) {
    //                 length++;
    //             }
    //         }
    //         if (schema.minProperties !== undefined
    //             && length < schema.minProperties) {
    //             return DefaultSettings.error.minProperties.replace('{0}', String(schema.minProperties));
    //         }
    //         if (schema.maxProperties !== undefined
    //             && length > schema.maxProperties) {
    //             return DefaultSettings.error.maxProperties.replace('{0}', String(schema.maxProperties));
    //         }
    //     }
    //     return '';
    // }

// validateControl(value: any | undefined, schema: Schema, ngFormControl: AbstractControl) {
    //     // console.log('ValidateControl: ', ngFormControl);
    //     // 1. validate control depending on data type and schema custom validators
    //     // 2. Set Status, isValidated, and errorMessage to the right message
    //     // 3. return errorMessage array if needed
    //     this.errorMessages = [];
    //     let errorMessage: string;
    //     // Get Angular Control specific errors: ***** Working
    //     console.log(ngFormControl.errors);
    //     for (let error in ngFormControl.errors) {
    //         if (ngFormControl.errors.hasOwnProperty(error)) {
    //             console.log(error);
    //             console.log(ngFormControl.errors[error]);
    //             errorMessage = this._buildErrorMessage(error, schema.title, ngFormControl.errors[error]);
    //         }
    //     }
    //     // errorMessage = this._buildErrorMessage('required', schema.title);
    //     // console.log('Push data type specific error message to array: ', errorMessage);
    //     (!!errorMessage && errorMessage.length) && this.errorMessages.push(errorMessage);
    //     // Get data type specific errors:
    //     switch (schema.type) {
    //         case 'string':
    //             errorMessage = this._getErrorMessageOfString(value, <IStringSchema>schema);
    //             break;
    //         case 'number':
    //         case 'integer':
    //             errorMessage = this._getErrorMessageOfNumber(value, <INumberSchema>schema);
    //             break;
    //         case 'array':
    //             errorMessage = this._getErrorMessageOfArray(value, <IArraySchema>schema);
    //             break;
    //         case 'object':
    //             errorMessage = this._getErrorMessageOfObject(value, <IObjectSchema>schema);
    //             break;
    //     }
    //     //  console.log('Push data type specific error message to array: ', errorMessage);
    //     (!!errorMessage && errorMessage.length) && this.errorMessages.push(errorMessage);
    //     // Custom Validation if any
    //     let isCustomValid = this.customValidateControl(value, schema);
    //     if (!isCustomValid) {
    //        // console.log('Push Custom Validator error messages to array: ', this.customValidators);
    //         for (let cv in this.customValidators) {
    //             if (this.customValidators.hasOwnProperty(cv) && !this.customValidators[cv].isValid) {
    //                 // console.log('Push Custom Validator error messages to array: ', this.customValidators[cv].errorMessage);
    //                 errorMessage = this.customValidators[cv].errorMessage || '';
    //                 (!!errorMessage && errorMessage.length) && this.errorMessages.push(errorMessage);
    //             }
    //         }
    //         // (!!errorMessage && errorMessage.length) && this.errorMessages.push(errorMessage);
    //     }
    //     this.setValidatorStatus({
    //         isValid: isCustomValid && !this.errorMessages.length,
    //         isCustomValidatorValid: isCustomValid,
    //         isDefaultValidatorValid: !this.errorMessages.length
    //     });
    //     // console.log(this.errorMessages);
    //     // console.log(this.status);
    //     this.isValidated = true;
    // }
