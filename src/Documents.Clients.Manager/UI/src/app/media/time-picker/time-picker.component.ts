import { Component, OnInit, Input, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { IMediaSegment } from '..';
import * as moment from 'moment';
import { FormControl, Validators } from '@angular/forms';

@Component({
  selector: 'app-media-time-picker',
  templateUrl: './time-picker.component.html',
  styleUrls: ['./time-picker.component.scss']
})
export class TimePickerComponent {
  public hours: number;
  public minutes: number;
  public seconds: number;
  public milliseconds: number;

  public maxHours: number;
  public maxMinutes: number;
  public maxSeconds: number;
  public maxMillisecondsValidator: number;

  public minHours: number;
  public minMinutes: number;
  public minSeconds: number;
  public minMillisecondsValidator: number;

  private _maxMilliseconds: number;
  private _minMilliseconds: number;

  public hoursControl = new FormControl('', [Validators.min(0), Validators.max(23)]);
  public minutesControl = new FormControl('', [Validators.min(0), Validators.max(59)]);
  public secondsControl = new FormControl('', [Validators.min(0), Validators.max(59)]);
  public millisecondsControl = new FormControl('', [Validators.min(0), Validators.max(999)]);

  @Output() onTimeChanged = new EventEmitter<number>();

  @Input()
  set maxMilliseconds(value: number) {
    const durationStart = moment.duration(value, 'milliseconds');

    this.maxHours = durationStart.hours();
    this.maxMinutes = durationStart.minutes();
    this.maxSeconds = durationStart.seconds();
    this.maxMillisecondsValidator = durationStart.milliseconds();

    this._maxMilliseconds = value;
  }
  get maxMilliseconds(): number {
    return this._maxMilliseconds;
  }

  @Input()
  set minMilliseconds(value: number) {
    const durationStart = moment.duration(value, 'milliseconds');

    this.minHours = durationStart.hours();
    this.minMinutes = durationStart.minutes();
    this.minSeconds = durationStart.seconds();
    this.minMillisecondsValidator = durationStart.milliseconds();

    this._minMilliseconds = value;
  }
  get minMilliseconds(): number {
    return this._minMilliseconds;
  }

  @Input()
  set inputTimeNumber(value: number) {
    const durationStart = moment.duration(value, 'milliseconds');

    this.hours = durationStart.hours();
    this.minutes = durationStart.minutes();
    this.seconds = durationStart.seconds();
    this.milliseconds = durationStart.milliseconds();
  }
  get inputTimeNumber(): number {
    const durationStart = moment.duration(+this.seconds, 'seconds');
    durationStart.add(+this.minutes, 'minutes');
    durationStart.add(+this.hours, 'hours');
    durationStart.add(+this.milliseconds, 'milliseconds');
    return durationStart.asMilliseconds();
  }

  constructor() {}

  emitTimeChanged() {
    if (this.isTimeControlValid()) {
      this.onTimeChanged.emit(this.inputTimeNumber);
    }
  }

  isTimeControlValid() {
    return (
      this.hoursControl.valid &&
      this.secondsControl.valid &&
      this.minutesControl.valid &&
      this.millisecondsControl.valid &&
      this.isTimeWithinBounds()
    );
  }

  isTimeWithinBounds() {
    return this.inputTimeNumber >= this.minMilliseconds && this.inputTimeNumber <= this.maxMilliseconds;
  }
}
