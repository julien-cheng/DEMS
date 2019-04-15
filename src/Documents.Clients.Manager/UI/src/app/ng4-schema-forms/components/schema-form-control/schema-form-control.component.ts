import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormGroup, FormControl, AbstractControl } from '@angular/forms';
import {
  IBaseControlOptions,
  IBaseSchema,
  Schema,
  IArraySchema,
  IBooleanSchema,
  INullSchema,
  IObjectSchema,
  IStringSchema,
  INumberSchema,
  ValidityValue,
  ValueType
} from '../../index';
import { SchemaForm } from '../../models/schema-form.model';
import { BaseControl } from '../../models/base-control.model';
import { Validation } from '../../models/validation.model';

@Component({
  selector: 'app-schema-form-control',
  templateUrl: './schema-form-control.component.html',
  styleUrls: ['./schema-form-control.component.scss']
})
export class SchemaFormControlComponent implements OnInit {
  @Output() updateFormValue = new EventEmitter();
  @Input() form: FormGroup;
  @Input() mainFormControl?: BaseControl;
  public validator: Validation;
  public formControl: BaseControl;
  public ngformControl: AbstractControl;

  constructor(public schemaForm: SchemaForm) {}

  ngOnInit() {
    this.formControl = this.mainFormControl;
    this.ngformControl = this.form.controls[this.formControl.key];
    this.formControl.validateControl(this.ngformControl);
    this.validator = this.formControl.validator;
  }

  updateValue(newValue: ValidityValue<ValueType>) {
    // console.log('2. ----- Value Updated in Schema FormControl Component - validate and propagate up: ', newValue);
    this.formControl.value = newValue.value;
    this.formControl.validateControl(this.ngformControl);
    // console.log(this.formControl.value);
    //  console.log( this.formControl.key);
    // let cntrl = this.form.get(this.formControl.key);
    // console.log(cntrl.value);
    //  console.log('<--------|');
    // Needs to propagate the BaseControl instance up to form
    this.updateFormValue.emit(this.formControl);
  }
}
