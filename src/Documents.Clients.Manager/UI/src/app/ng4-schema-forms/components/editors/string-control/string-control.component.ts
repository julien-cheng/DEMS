import { Component, OnInit, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { FormGroup }  from '@angular/forms';
import { Schema, IStringSchema, ValueType, Validation } from '../../../index';
import { BaseControl } from '../../../models/base-control.model';
// import { FocusDirective } from '../../../directives/focus.directive';

@Component({
  selector: 'string-control',
  templateUrl: './string-control.component.html',
  styleUrls: ['./string-control.component.scss']
})
export class StringControlComponent implements OnInit {
  @Output() updateValue = new EventEmitter();
  @Input() form: FormGroup;
  @Input() formControlObj: BaseControl;
  @Input() readonly?: boolean;
  @Input() formControlValidator: Validation;
  public stringControlType: string;
  public schema: IStringSchema;
  value?: ValueType;
  
  get isInvalid() {
    // Return: the state of the ng form control and basecontrol validator
    let isInvalid = !this.formControlValidator.isValid || this.ngFormControl.invalid,
        ngformstatus =(this.ngFormControl.dirty || this.ngFormControl.touched || this.ngFormControl.value)
    return isInvalid && ngformstatus;
  }
  get ngFormControl(){
    return this.form.controls[this.formControlObj.key];
  }

  constructor() {
  }

  ngOnInit() {
    this.schema = <IStringSchema>this.formControlObj.schema;
    this.value = this.formControlObj.value; // this.value = this.getDefaultValue(this.schema);
    this._setStringControlType();
  }

  isDisabled(){
    return this.isReadOnly;
  }

  // Description: gets the string control type: input, textarea, select, others...
  //  If the enum property is specified, format will be ignored.
  // FormControlType: textarea, select or input
  // HTML5 Types (html5Format):  color, date,  datetime, datetime-local, email, month, number, range, tel, text, textarea, time, url, week
  private _setStringControlType(): string {
    return this.stringControlType = (this.schema.format === 'textarea') ? 'textarea' :
      (this.schema.enum !== undefined && !this.isReadOnly) ? 'select' : 'input';
  }

  // Description: is control readonly and disabled
  get isReadOnly() {
    return this.readonly || this.schema.readonly;
  }

  // Description: Trigger change events for validation and other...
  onChange(e: { target: { value: string } }) {
    this.value = e.target.value;
   //  console.log( '4. ##### String onChange',  this.value );
   // console.log(this.isValid);
    this.updateValue.emit({ value: this.value });
  }

}
