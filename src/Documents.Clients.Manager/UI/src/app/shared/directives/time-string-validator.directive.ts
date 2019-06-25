import { Directive } from '@angular/core';
import { AbstractControl, FormControl, Validators, Validator, ValidatorFn, ValidationErrors, NG_VALIDATORS } from '@angular/forms';

@Directive({
  selector: '[timeStringValidator][ngModel]',
  providers: [{ provide: NG_VALIDATORS, useExisting: TimeStringValidatorDirective, multi: true }]
})
export class TimeStringValidatorDirective implements Validator {
  validator: ValidatorFn;
  constructor() {
    this.validator = this.timeStringvalidator();
  }
  validate(c: FormControl) {
    return this.validator(c);
  }

  // Description: Accepts h:mm:ss.SSS | hh:mm:ss.SSS | mm:ss.SSS | '00:00'
  timeStringvalidator(): ValidatorFn {
    return (c: FormControl) => {
      let isValid = true;
      if(c.value !== '00:00'){
        isValid = /^(([0-9]{1}|0[0-9]{1}|1[0-9]{1}|2[0-3]{1}):([0-5]{1}[0-9]{1}):([0-5]{1}[0-9]{1}).([0-9]{1}[0-9]{1}[0-9]{1}))$/.test(c.value) ||
           /^(([0-5]{1}[0-9]{1}):([0-5]{1}[0-9]{1}).([0-9]{1}[0-9]{1}[0-9]{1}))$/.test(c.value);
      }
      
      if (isValid) {
        return null;
      } else {
        return {
          timeStringvalidator: {
            valid: false
          }
        };
      }
    }
  }
}
