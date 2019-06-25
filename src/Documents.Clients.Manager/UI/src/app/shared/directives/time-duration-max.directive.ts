import { Directive, Input } from '@angular/core';
import { AbstractControl, FormControl, Validators, Validator, ValidatorFn, ValidationErrors, NG_VALIDATORS } from '@angular/forms';

@Directive({
  selector: '[durationMax][ngModel]',
  providers: [{ provide: NG_VALIDATORS, useExisting: TimeDurationMaxDirective, multi: true }]
})
export class TimeDurationMaxDirective {
  @Input('durationMax') max: number;
  validator: ValidatorFn;
  constructor() {
    this.validator = this.timeDurationMaxValidator();
  }
  validate(c: FormControl) {
    return this.validator(c);
  }

  timeDurationMaxValidator(): ValidatorFn {
 
    // Need proper test for validation in the MM:SS format
    return (c: FormControl) => {
      let max = this.max;
      let isValid = (!!max && !isNaN(max) && !isNaN(c.value)) && (c.value <= this.max);
     // console.log(isValid,max, c.value);
      if (isValid) {
        return null;
      } else {
        return {
          timeDurationMaxValidator: {
            valid: false
          }
        };
      }
    }
  }
}
