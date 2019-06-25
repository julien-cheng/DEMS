import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'iterativeObject'
})
export class IterativeObjectPipe implements PipeTransform {

  transform(value): any {
    let keys = [];

      // tslint:disable-next-line:forin
      for (let key in value) {
        keys.push({key: key, value: value[key]});
      }


    return keys;
  }

}
