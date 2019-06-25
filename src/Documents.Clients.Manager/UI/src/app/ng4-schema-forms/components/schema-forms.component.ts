import { Component, OnInit, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { FormGroup, NgForm, AbstractControl } from '@angular/forms';
import { SchemaForm } from '../models/schema-form.model';
import { ISchemaFormOptions, SCHEMAFORMS_EVENTS } from '../definitions/api';
import { BaseControl } from '../models/base-control.model';
import { FormControl } from '@angular/forms/src/model';

import * as _ from 'lodash';
const { includes, pick } = _;

@Component({
  selector: 'ng4-schema-forms',
  templateUrl: './schema-forms.component.html',
  styleUrls: ['./schema-forms.component.scss']
})
export class SchemaFormsComponent implements OnInit {
  @Input() options: ISchemaFormOptions | null;
  @Input() columns: number = 1; //Defaults to 1
  @Input() breakpoint: string = 'md'; // Will be stored in the server
  @Input() displaySubmitButton: boolean = true;
  @Input() setFocusOnload: boolean;
  @Input() submitButtonText: string = 'Save';
  @Input() submitButtonIcon: string;
  @Input() resetButtonText: string = 'Cancel';
  @Input() resetButtonIcon: string;
  @ViewChild('form') form: NgForm;
  public formGroup: FormGroup;
  public objectKeys = Object.keys;
  public schemaFormValues: any;
  public mainFormControl: BaseControl[];
 

  // Possible event handlers:
  @Output() initialized = new EventEmitter();
  @Output() updateData = new EventEmitter();
  @Output() event = new EventEmitter();
  @Output() stateChange = new EventEmitter();
  @Output() validate = new EventEmitter();
  @Output() validated = new EventEmitter();
  @Output() complete = new EventEmitter();
  @Output() success = new EventEmitter();
  @Output() error = new EventEmitter();
  @Output() cancel = new EventEmitter();
  @Output() schemaFormSubmit = new EventEmitter();

  constructor(
    public schemaFormModel: SchemaForm
  ) {

  }

  // Some of this code may change to ngOnChanges(simpleChanges: SimpleChanges) {...}
  ngOnInit() {
    this.formGroup = new FormGroup({});

    // Set the schema form options and get properties for array
    this.schemaFormModel.setSchemaFormData({
      options: this.options,
      events: pick(this, this.schemaFormModel.eventNames)
    });

    this.mainFormControl = this.schemaFormModel.getFormBaseControlProperties(this.schemaFormModel.schema);
    this.formGroup = this.schemaFormModel.toFormGroup(this.mainFormControl);
  }

  ngAfterContentInit() {
    this.setFocusOnload && this.setFocus();
  }

  // Description: returns true/false for valid form
  get isValid() {
    return !!this.formGroup && this.formGroup.valid;
  }

  // Description: Triggered when any control value in the form is changed and validated - Working and TBD *****
  updateFormValue(baseControl: BaseControl) {
    //  console.log(baseControl);
    // console.log('1. !!!!! updateFormValue in Schema Form Component - Final bubble', baseControl.value);
    //   console.log(this.form.value);
    // 1. Update data schema  ***** for data submit ready ***** WORKING *****
    // 2. Update Validation status  ***** for data submit ready  ***** WORKING *****
  }


  // Description: Add any code for internal form submission in this block
  onSchemaFormSubmit($event: Event) {
    // console.log(this.form.value);
    if (this.isValid) {
      this.schemaFormValues = this.form.value; // JSON.stringify(this.form.value);
      this.schemaFormModel.fireEvent({ eventName: SCHEMAFORMS_EVENTS.schemaFormSubmit, schemaFormValues: this.form.value });
      return true;
    } else {
      return false;
    }
  }

  // Description: Resets forms to original values
  onSchemaFormReset($event: Event){
    const initialValues =this.schemaFormModel.initialFormValue;
    Object.keys(initialValues).forEach((control) => {
       (!!this.formGroup.controls[control]) && this.formGroup.controls[control].patchValue(initialValues[control]);
    });
  }


  // Description: Exposes the form submit to outside buttons
  // works with displaySubmitButton set to true
  public triggerFormSubmit($event: Event) {
    //  console.log($event);
    return this.form.valid && this.form.ngSubmit.emit($event || new Event('submit'));
  }

  public ngformControl: AbstractControl;

  // Description: Sets focus in a pre-specified control
  public setFocus(key?: string) {
    // console.log('setFocus: ', key);
    (!key) && (key = this.mainFormControl[0].key);// find first key if key is undefined
    if (!!key) {
      let control = this.mainFormControl.filter((basecontrol) => {
        return basecontrol.key === key;
      });
      control.length && (control[0].isFocused = true);
    }
  }

  private _validateForm() {
    for (let control in this.formGroup.controls) {
      if (this.formGroup.controls.hasOwnProperty(control)) {
        this.formGroup.controls[control].markAsTouched();
      }
    }
  }
}
