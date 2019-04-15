import { Component, OnInit, Input } from '@angular/core';
import { BaseControl } from '../../../models/base-control.model';
import { ValueType } from 'app/ng4-schema-forms';

@Component({
  selector: 'null-control',
  templateUrl: './null-control.component.html',
  styleUrls: ['./null-control.component.scss']
})
export class NullControlComponent implements OnInit {
  @Input() formControlObj: BaseControl;
  value: ValueType | null;
  constructor() {}

  ngOnInit() {
    this.value = this.formControlObj.value;
  }
}
