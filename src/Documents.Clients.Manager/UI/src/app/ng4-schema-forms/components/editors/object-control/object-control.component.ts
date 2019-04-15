import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { SchemaForm } from '../../../models/schema-form.model';
import { Schema, IObjectSchema, ValidityValue, ValueType, Validation } from '../../../index';
import { BaseControl } from '../../../models/base-control.model';
import { AbstractControl } from '@angular/forms/src/model';
import * as _ from 'lodash';
const { isObject } = _;

@Component({
  selector: 'object-control',
  templateUrl: './object-control.component.html',
  styleUrls: ['./object-control.component.scss']
})
export class ObjectControlComponent implements OnInit {
  @Output() updateValue = new EventEmitter();
  @Input() form: FormGroup;
  @Input() formControlObj: BaseControl;
  @Input() readonly?: boolean;
  @Input() formControlValidator: Validation;
  public schema: IObjectSchema;
  public nestedForm: FormGroup;
  public mainFormControl: BaseControl[];
  public title = '';
  value?: ValueType;
  // errorMessage: string;
  get isInvalid() {
    // Return: the state of the ng form control and basecontrol validator
    const isInvalid = !this.formControlValidator.isValid || this.ngFormControl.invalid,
      ngformstatus = this.ngFormControl.dirty || this.ngFormControl.touched || this.ngFormControl.value;
    return isInvalid && ngformstatus;
  }
  get ngFormControl() {
    return this.form.controls[this.formControlObj.key];
  }
  collapsed = false;
  private control: AbstractControl;
  // private initialValue?: ValueType;
  // private properties: { key: string; value: Schema }[] = [];

  constructor(public schemaForm: SchemaForm) {}

  ngOnInit() {
    this.nestedForm = new FormGroup({});
    this.schema = this.formControlObj.schema as IObjectSchema;
    this.control = this.form.get(this.formControlObj.key);
    this.value = !!this.control.value ? this.control.value : this.getDefaultValue(this.schema);
    this.title = this.schema.title || '';
    this.collapsed = typeof this.schema.collapsed === 'boolean' ? this.schema.collapsed : false;
    this.mainFormControl = this.schemaForm.getFormBaseControlProperties(this._buildProperties(), this.value);
    this.nestedForm = this.schemaForm.toFormGroup(this.mainFormControl);
    this.formControlObj.setControlsValue(this.value);
  }

  private _buildProperties() {
    const newSchema = Object.assign({}, this.schema),
      props = Object.assign({}, this.schema.properties);
    // Add default to schema props
    if (isObject(this.value) && !_.isEmpty(this.value)) {
      for (const key in this.value as any) {
        if (this.value.hasOwnProperty(key)) {
          const newVal = this.value[key],
            property = Object.assign({}, props[key]);
          if (property.default === undefined) {
            property.default = newVal;
            props[key] = Object.assign({}, property);
          }
        }
      }
      newSchema.properties = props;
    }
    return newSchema;
  }

  // Description: Gets initial values for editor ***** Working
  // needs to account initialvalue, schema.default, and without any value
  private getDefaultValue(schema: Schema, initialValue?: ValueType, required?: boolean) {
    if (initialValue !== undefined && isObject(initialValue)) {
      return initialValue;
    } else if (schema.default) {
      // Defaults can be passed as arrays OR Obj - i.e. {prop1: value1, prop2: value2... } OR [{prop1: value1, prop2: value2... }]
      return Array.isArray(schema.default) && isObject(schema.default[0])
        ? Object.assign({}, schema.default[0])
        : isObject(schema.default)
        ? Object.assign({}, schema.default)
        : {};
    } else {
      return !!this.nestedForm.value ? this.nestedForm.value : {};
    }
  }

  // Description: Triggered when any control value in the form is changed and validated - Working and TBD *****
  updateFormValue(index: number, baseControl: BaseControl) {
    // console.log('3 ***** updateFormValue in Object Control: ', this.value);
    const key = baseControl.key;
    this.value[key] = baseControl.value;
    // console.log('baseControl', baseControl, baseControl.key, baseControl.value);
    // Propagate up to form
    this.updateValue.emit({ value: this.value });
  }
}
