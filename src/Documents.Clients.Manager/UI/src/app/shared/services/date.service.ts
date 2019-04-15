import { Injectable } from '@angular/core';
import * as moment from 'moment';

@Injectable()
export class DateService {
  constructor() {}

  // Description: is this a date
  public isDate(label: string): boolean {
    const formats = [moment.ISO_8601, 'MM/DD/YYYY  :)  HH*mm*ss'],
      isDate = moment(label, formats, true).isValid();

    return isDate;
  }
}
