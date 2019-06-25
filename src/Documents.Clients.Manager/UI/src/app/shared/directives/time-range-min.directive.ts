import { Directive, Input } from '@angular/core';
import { AbstractControl, FormControl, Validators, Validator, ValidatorFn, NG_VALIDATORS, NgModelGroup } from '@angular/forms';
@Directive({
  selector: '[TimeRangeMinimum]',
  providers: [{ provide: NG_VALIDATORS, useExisting: TimeRangeMinDirective, multi: true }]
})
export class TimeRangeMinDirective {
  @Input('TimeRangeMinimum') rangeMin: number = 1000;
  validator: ValidatorFn;
  constructor() {
    this.validator = this.timeRangeMinvalidator();
  }
  validate(c: AbstractControl) {
    return this.validator(c);
  }
  // Description: Validate for minimum range value of 1000 ms ***** can be converted to pass dynamically in rangeMin [TimeRangeMinimumRange]="number"
  timeRangeMinvalidator(): ValidatorFn {
    let rangeMin = this.rangeMin;
    return (group) => {
     // console.log(group.value);
     let isValid = (!isNaN(group.value.startTime) && !isNaN(group.value.endTime)) && ((group.value.endTime - group.value.startTime) >= rangeMin);
     let isStartSmaller=  (!isNaN(group.value.startTime) && !isNaN(group.value.endTime)) && (group.value.endTime >= group.value.startTime);
      if (isValid) {
        return null;
      } else {
        let obj= {
          timeRangeMinvalidator: {
            valid: false
          },
          isStartSmaller: {
            valid: isStartSmaller
          }
        };
        
        return obj;
      }
    }
  }
}
