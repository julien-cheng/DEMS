import { NouiFormatter } from 'ng2-nouislider';
import * as moment from 'moment';
export enum MediaClipActionType {
  // global = 'global',
  new = 'new',
  edit = 'edit',
  mute = 'mute'
}

export class TimeFormatter implements NouiFormatter {
  to(value: number): string {
    return !!value && value > 0 ? moment.utc(value).format('HH:mm:ss') : '00:00';
  }
  from(value: string): number {
    const v = value.split(':').map(parseInt);
    let time = 0;
    time += v[0] * 3600;
    time += v[1] * 60;
    time += v[2];
    console.log(time);
    return time;
  }
}

export class TooltipTimeFormatter implements NouiFormatter {
  to(value: number): string {
    const timeFormatted = value >= 3600000 ? moment.utc(value).format('HH:mm:ss') : moment.utc(value).format('mm:ss.SSS');
    return timeFormatted;
  }
  from(value: string): number {
    const v = value.split(':').map(parseInt);
    let time = 0;
    time += v[0] * 3600;
    time += v[1] * 60;
    time += v[2];
    console.log(time);
    return time;
  }
}
