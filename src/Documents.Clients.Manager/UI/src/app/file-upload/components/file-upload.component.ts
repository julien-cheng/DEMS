/* tslint:disable */ // TEMP
import { Component, Input, OnInit, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { FileUpload } from '../models/file-upload.model';
import { IFileUploadOptions } from '../models/file-upload.model';
import * as _ from 'lodash';
const { includes, pick } = _;

@Component({
  selector: 'app-fileUpload',
  template: `
    <ng-content select="[fileUpload-content]"></ng-content>
  `
})
export class FileUploadComponent implements OnInit {
  //@Input() set options(options: IFileUploadOptions) { };
  @Input() options = <IFileUploadOptions>{};
  @Input() fileUpload: FileUpload;

  // Possible event handlers:
  @Output() initialized = new EventEmitter();
  @Output() updateData = new EventEmitter();
  @Output() event = new EventEmitter();
  @Output() changeFilter = new EventEmitter();
  @Output() stateChange = new EventEmitter();
  @Output() complete = new EventEmitter();
  @Output() success = new EventEmitter();
  @Output() error = new EventEmitter();
  @Output() cancel = new EventEmitter();

  constructor() {
    // console.log(this.fileUpload);
  }

  ngOnInit() {}

  ngAfterViewInit() {
    this.initialized.emit(this.fileUpload);
  }

  ngOnChanges(simpleChanges: SimpleChanges) {
    // console.log(simpleChanges);
    this.fileUpload.setFileUploadData({
      options: simpleChanges.options && simpleChanges.options.currentValue,
      events: pick(this, this.fileUpload.eventNames)
    });
  }
}
