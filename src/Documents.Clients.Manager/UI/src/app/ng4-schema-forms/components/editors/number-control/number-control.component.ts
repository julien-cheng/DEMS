import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormGroup }  from '@angular/forms';
import { Schema, INumberSchema, ValueType, Validation } from '../../../index';
import { BaseControl } from '../../../models/base-control.model';

@Component({
  selector: 'number-control',
  templateUrl: './number-control.component.html',
  styleUrls: ['./number-control.component.scss']
})
export class NumberControlComponent implements OnInit {
  @Output() updateValue = new EventEmitter(); // new EventEmitter<common.ValidityValue<string | undefined>>();
  @Input() form: FormGroup;
  @Input() formControlObj: BaseControl;
  @Input() readonly?: boolean;
  @Input() formControlValidator: Validation;
  public numberControlType: string;
  public schema: INumberSchema;
  value?: ValueType;
  // errorMessage: string;
  get isInvalid() {
    // Return: the state of the ng form control and basecontrol validator
    let isInvalid = !this.formControlValidator.isValid || this.ngFormControl.invalid,
        ngformstatus =(this.ngFormControl.dirty || this.ngFormControl.touched || this.ngFormControl.value);
    return isInvalid && ngformstatus;
  }
  get ngFormControl(){
    return this.form.controls[this.formControlObj.key];
  }
  constructor() { }

  ngOnInit() {
    this.schema = <INumberSchema>this.formControlObj.schema;
    this.value = this.formControlObj.value; // this.value = this.getDefaultValue(this.schema);
    this._setNumberControlType();
  }

  // Description: gets the number control type: input, select, others...
  // FormControlType:  select or input
  private _setNumberControlType() {
    return this.numberControlType = (this.schema.format === 'select' || (this.schema.enum !== undefined && !this.isReadOnly)) ? 'select' : 'input';
  }

  // Description: is control readonly and disabled
  get isReadOnly() {
    return this.readonly || this.schema.readonly;
  }

  // Description: Trigger change events for validation and other...
  onChange(e: { target: { value: number } }) {
    this.value = !isNaN(e.target.value) ? Number(e.target.value) : e.target.value;
    // this.validate();
    // console.log( '4. ##### number onChange', this.value);
    this.updateValue.emit({ value: this.value });
  }

  // Description: Validate control
  private validate() {
    // TBD
    //  this.errorMessage = getErrorMessageOfString(this.value, this.schema);
  }
}
