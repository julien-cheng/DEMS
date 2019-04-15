/* tslint:disable */
import { Injectable } from '@angular/core';
import { FormControl, FormGroup, Validators, ValidatorFn } from '@angular/forms';
import { IterativeObjectPipe } from '../../shared/pipes/iterative-object.pipe';
import {
  IBaseControlOptions,
  SCHEMAFORMS_EVENTS,
  IBaseSchema,
  Schema,
  IArraySchema,
  IBooleanSchema,
  INullSchema,
  IObjectSchema,
  IStringSchema,
  INumberSchema
} from '../index';
import { ICustomValidator, ISchemaFormOptions } from '../index';
import { BaseControl } from '../models/base-control.model';

@Injectable()
export class SchemaForm {
  private events: any;
  public schema: IBaseSchema;
  public initialFormValue: any;
  public eventNames = Object.keys(SCHEMAFORMS_EVENTS);
  public customValidators?: Array<ICustomValidator> = [];

  public constructor() {}

  // Description: Sets the models schema, options, and custom validators ({ options = null, events = null }: { options: any, events: any })
  public setSchemaFormData({ options = null, events = null }: { options: ISchemaFormOptions; events: any }) {
    if (options.schema) {
      this.schema = options.schema;
    }
    // Registers the options:
    if (options.initialFormValue) {
      this.initialFormValue = options.initialFormValue;
    }

    if (options.customValidators) {
      this.customValidators = options.customValidators;
    }

    // Registers the events exposed:
    if (events) {
      this.events = events;
    }

    // Fire Exposed events when appropriate
    this.fireEvent({ eventName: SCHEMAFORMS_EVENTS.initialized });
  }

  // Description: Sets BaseControl from schema and DEFAULT VALUES
  public getFormBaseControlProperties(schema: IBaseSchema, initialFormValue?: any): BaseControl[] {
    // console.log('getFormBaseControlProperties', schema); // console.log('schemaKey', schemaKey);
    let controlsArray: BaseControl[] = [],
      properties = schema.properties;
    if (properties) {
      for (let key in properties) {
        if (properties.hasOwnProperty(key)) {
          let initialValue =
            initialFormValue !== undefined
              ? initialFormValue[key]
              : this.initialFormValue !== undefined
              ? this.initialFormValue[key]
              : undefined;
          let formControl = new BaseControl(key, properties[key], this.customValidators, initialValue);
          controlsArray.push(formControl);
        }
      }
    }
    return controlsArray;
  }

  public getSingleFormBaseControl(schema: IBaseSchema, schemaKey?: string): BaseControl[] {
    let controlsArray: BaseControl[] = [];
    // console.log('getSingleFormBaseControl',controlsArray);
    if (!!schema && !!schemaKey) {
      let formControl: BaseControl = new BaseControl(schemaKey, <Schema>schema, this.customValidators);
      controlsArray.push(formControl);
    }
    return controlsArray;
  }

  // Description: Sets FormGroups and Controls for the form and INITIAL VALUES
  public toFormGroup(properties: BaseControl[]) {
    let group: any = {};
    // console.log('toFormGroup', properties);
    // need and array of IBaseControls in the form to build the dynamic form
    properties.forEach(prop => {
      // console.log(prop.validator.customValidators);
      group[prop.key] =
        !!prop.validator.validatorArr && prop.validator.validatorArr.length
          ? new FormControl(prop.value, Validators.compose(prop.validator.validatorArr))
          : new FormControl(prop.value);
    });
    // console.log('---------------------', group);
    return new FormGroup(group);
  }

  // Exposed setting of the customValidators
  public setCustomValidation(customValidators: Array<ICustomValidator>) {
    // Might need to expand on this and look at this.customValidators.lenght
    // and do something like :(forEach((customValidator)=>{this.customValidators.push(customValidator)});
    return (this.customValidators = customValidators);
  }

  // Trigger the exposed public events with this method
  public fireEvent(event) {
    try {
      !!this.events[event.eventName] && this.events[event.eventName].emit(event); //  this.events.event.emit(event);
    } catch (m) {
      console.error(m);
    }
  }

  public setControlData() {
    console.log('setControlData', this);
  }

  public getControlData() {
    console.log('setControlData', this);
  }

  // Description: resets controls to initial values
  // public resetFormData() {

  // }

  // Description: breaks the object object into array of objec in the form properties = [{key: nameOfSchemaObj, value: object value }, ...]
  // Use for proper ng looping
  public getFormProperties() {
    // NEED TO MODIFY THIS METHOD TO ADD CUSTOM VALIDATION TO THE OBJECT change {key: nameOfSchemaObj, value: object value }
    // format to {name: nameOfSchemaObj, schema: object value, customValidation?: Array<ICustomValidator> }
    let iterativeObjectPipe: IterativeObjectPipe = new IterativeObjectPipe();
    return (!!this.schema && iterativeObjectPipe.transform(this.schema.properties)) || null;
  }
  // Methods to build:
  // Description Triggered by Submit form in schemaFormsComponent
  public validateSchemaForm() {
    console.log('validateSchemaForm');
  }

  public onChangeSchemaForm() {
    console.log('onChangeSchemaForm');
  }

  public watchSchemaForm() {
    console.log('watchSchemaForm');
  }

  public unwatchSchemaForm() {
    console.log('unwatchSchemaForm');
  }

  public destroySchemaForm() {
    console.log('destroySchemaForm');
  }

  // Trigger focus
  public setFocus(key?: string) {
    console.log('SchemaForm setFocus: ', key);
  }
}
