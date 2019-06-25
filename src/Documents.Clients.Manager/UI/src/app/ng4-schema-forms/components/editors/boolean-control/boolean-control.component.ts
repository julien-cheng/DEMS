import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { Schema, IBooleanSchema, ValueType, Validation } from '../../../index';
import { BaseControl } from '../../../models/base-control.model';

@Component({
  selector: 'boolean-control',
  templateUrl: './boolean-control.component.html',
  styleUrls: ['./boolean-control.component.scss']
})
export class BooleanControlComponent implements OnInit {
  @Output() updateValue = new EventEmitter();
  @Input() form: FormGroup;
  @Input() formControlObj: BaseControl;
  @Input() readonly?: boolean;
  @Input() formControlValidator: Validation;
  public booleanControlType: string;
  public schema: IBooleanSchema;
  value?: ValueType;
  errorMessage: string;
  valueArray: Array<boolean> = [true, false];

  get isInvalid() {
    // Return: the state of the ng form control and basecontrol validator
    let isInvalid = !this.formControlValidator.isValid || this.ngFormControl.invalid,
      ngformstatus = (this.ngFormControl.dirty || this.ngFormControl.touched || this.ngFormControl.value)
    return isInvalid && ngformstatus;
  }
  get ngFormControl() {
    return this.form.controls[this.formControlObj.key];
  }

  constructor() { }

  // Control Description: The default boolean editor is a select box with options "true" and "false".
  // To use a checkbox instead, set the format to checkbox.
  ngOnInit() {
    this.schema = <IBooleanSchema>this.formControlObj.schema;
    this.value = this.formControlObj.value; // this.getDefaultValue(this.schema, this.initialValue);
    this._setBooleanControlType();
  }

  // Description: gets the boolean control type: checkbox (default / not optional), radio (format: radio), select(format: select OR optional=true)
  //  If optional is set to true, format will be ignored.
  // checkbox, radiolist,  or select
  private _setBooleanControlType() {
    return this.booleanControlType = (this.schema.format === 'radio') ? 'radio' :
      (this.schema.optional !== undefined || this.schema.format === 'select') ? 'select' : 'checkbox';
  }

  // Description: Trigger change events for validation and other...
  onChange(e: { target: { checked: boolean } }) {
    this.value = this.ngFormControl.value;
    // console.log('this.value: ', this.value);
    this.updateValue.emit({ value: this.value });
  }


  // Description: is control readonly and disabled
  get isReadOnly() {
    return this.readonly || this.schema.readonly;
  }
}
