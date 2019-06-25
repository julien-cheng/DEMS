import { FormControl, ValidatorFn } from '@angular/forms';

export function excludePatternValidator(pattern: RegExp | string): ValidatorFn {
    //  console.log(pattern);
    return (control: FormControl): {[key: string]: any} => {
        let success:boolean= true;
        if(!(pattern instanceof RegExp)){
            pattern = new RegExp(pattern, "i");
            // console.warn('Exclusive pattern is not a properly formatted RegExp', pattern);
        }
        success = pattern.test(control.value);
      return success ? {'excludePattern': {value: control.value}} : null;
    };
  }
