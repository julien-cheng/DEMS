import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { SchemaForm } from '../../../models/schema-form.model';
import { Schema, IBaseSchema, IArraySchema, ValidityValue, ValueType, Validation } from '../../../index';
import { BaseControl } from '../../../models/base-control.model';

@Component({
  selector: 'array-control',
  templateUrl: './array-control.component.html',
  styleUrls: ['./array-control.component.scss']
})
export class ArrayControlComponent implements OnInit {
  @Output() updateValue = new EventEmitter();
  @Input() form: FormGroup;
  @Input() formControlObj: BaseControl;
  @Input() readonly?: boolean;
  @Input() formControlValidator: Validation;
  public schema: IArraySchema;
  public nestedForm: FormGroup;
  public mainFormControl: BaseControl[];
  value?: ValueType;
  errorMessage: string;
  collapsed = false;
  get isInvalid() {
    // Return: the state of the ng form control and basecontrol validator
    // let isInvalid = !this.formControlValidator.status.isValid || this.ngFormControl.invalid,
    const isInvalid = !this.formControlValidator.isValid || this.ngFormControl.invalid,
      ngformstatus = this.ngFormControl.dirty || this.ngFormControl.touched || this.ngFormControl.value;
    return isInvalid && ngformstatus;
  }
  get ngFormControl() {
    return this.form.controls[this.formControlObj.key];
  }

  constructor(public schemaForm: SchemaForm) {}

  // The controlTemplate comes this.schema.items - namely the type of editor that will be repeated per default value
  // default: ['default item 1', 'default item 2'] - the initial values for the editor controls and how many are needed on init
  ngOnInit() {
    this.nestedForm = new FormGroup({});
    this.schema = this.formControlObj.schema as IArraySchema;
    this.value = this.formControlObj.value; // this.getDefaultValue(this.schema);// this.schema.default; - NEEDS TO FIX THIS TO PASS [{'key':val}] to build the rest of the editor
    this.collapsed = typeof this.schema.collapsed === 'boolean' ? this.schema.collapsed : false;
    this.mainFormControl = this.schemaForm.getFormBaseControlProperties(this._buildProperties());
    this.nestedForm = this.schemaForm.toFormGroup(this.mainFormControl);
  }

  // Description: build control array from value
  private _buildProperties() {
    const self = this;
    const props = Object.assign({}, this.schema);
    props.properties = {};
    let i = 0;
    if (Array.isArray(this.value)) {
      this.value.forEach(defaultVal => {
        props.properties[this.formControlObj.key + '_' + i] = self.setNestedFormInformation(defaultVal, i);
        i++;
      });
    }
    // console.log(props);
    return props;
  }

  // Description: build individual control schema - array from schema.items (template)
  private setNestedFormInformation(defaultVal: any, index: number): Schema {
    // console.log(defaultVal);
    const property = Object.assign({}, this.schema.items);
    property.default = defaultVal;
    if (!this.schema.items.title) {
      property.title = '';
    } // can be empty or add the main schema title
    // console.log(property);
    return property;
  }

  // Description: Add Editor Row
  onAddRow($event: any) {
    // console.log('|-----> Before: ', this.value, this.mainFormControl);
    const newIndex = this.mainFormControl.length,
      newKey = this.formControlObj.key + '_' + newIndex,
      newVal = this.formControlObj.createNewEmptyValuebyDataType(this.schema.items.type),
      prop = this.setNestedFormInformation(newVal, newIndex), // prop = { index: newIndex, key: this.schema.title, value: Object.assign({}, this.schema.items) };
      newBaseControl: BaseControl[] = this.schemaForm.getSingleFormBaseControl(prop, newKey);
    if (newBaseControl.length && Array.isArray(this.value)) {
      this.mainFormControl.push(newBaseControl[0]);
      this.value.push(newVal);
      this.nestedForm.addControl(newKey, new FormControl(newVal)); // Need to add Validator ?new FormControl(newVal, Validaotor)
    }
    // console.log('<-----| After: ', this.value, this.mainFormControl);
  }

  // Description: Delete Editor Row
  onDelete(index: number) {
    // console.log('Before: ', index, this.mainFormControl);
    Array.isArray(this.value) && this.value.length && this.value.splice(index, 1);
    this.mainFormControl.splice(index, 1);
    // console.log('After: ', this.value, this.controlsArray);
    // Recalculate index !!!!!
    // this._updateItemIndexes();
    // console.log('After: ', this.value, this.mainFormControl);
    this.updateValue.emit({ value: this.value });
  }

  // Description: Triggered when any control value in the form is changed and validated
  updateFormValue(index: number, baseControl: BaseControl) {
    this.value[index] = baseControl.value;
    // console.log('3 ***** updateFormValue in Array Control: ', this.value);// ***** WORKING ON THIS - propagate UP *//
    // Propagate up to form
    this.updateValue.emit({ value: this.value });
  }
}
