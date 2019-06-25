import { Pipe, PipeTransform } from '@angular/core';
import * as moment from 'moment';

@Pipe({
  name: 'formatTime'
})
export class FormatTimePipe implements PipeTransform {
  // Description: transform milliseconds (number) to a string HH:mm:ss:sss or 'mm:ss.SSS' depending on value
  transform(value: number, args?: any): any {
    let timeFormatted = '00:00';
    (value > 0) && (timeFormatted = (value >= 3600000) ? moment.utc(value).format('HH:mm:ss.SSS') : moment.utc(value).format('mm:ss.SSS'));
    // console.log('timeFormatted',value, timeFormatted);
    return timeFormatted;
  }

  // Description: transform a string HH:mm:ss:sss to milliseconds (number)
  transformReversed(value: string): number {
    let ms = value.split(':').length <= 2 ? <number>(moment.duration('00:' + value).valueOf()) : <number>(moment.duration(value).valueOf());
    return ms;
  }

}
