import { Component, OnInit, Input, SimpleChanges, ViewChild, ElementRef } from '@angular/core';
import { FormsModule, FormGroup, NgForm } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { IPathIdentifier, IBatchRequestOperations } from '../../index';

@Component({
  selector: 'app-file-download',
  templateUrl: './file-download.component.html'
})
export class FileDownloadComponent implements OnInit {
  @Input() pathIdentifier: IPathIdentifier;
  @Input() downloadPayload: string;
  @ViewChild('downloadForm') downloadForm: NgForm;
  @ViewChild('downloadButton') downloadButton: ElementRef;
  public payloadJSON: IBatchRequestOperations;
  constructor(private element: ElementRef) { }
  

  ngOnInit() {
  }
  ngOnChanges(simpleChanges: SimpleChanges) {
    if (!!simpleChanges.downloadPayload && !!simpleChanges.downloadPayload.currentValue) {
      let downloadPayload = JSON.stringify(simpleChanges.downloadPayload.currentValue);
      this.downloadPayload = downloadPayload;
      setTimeout(() => {
        (<HTMLButtonElement>this.downloadButton.nativeElement).click();
      });
    }
  }
}
