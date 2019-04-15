import { Directive, ElementRef, Input, HostListener } from '@angular/core';
import { FileUpload } from '../index';

@Directive({
  selector: '[appFileSelect]'
})
export class FileSelectDirective {
  @Input() public fileUpload: FileUpload;
  protected element: ElementRef;

  public constructor(element: ElementRef) {
    // console.log("FileSelectDirective");
    this.element = element;
    // console.log(this.element);
  }

  @HostListener('change')
  public onChange(): any {
    const files = this.element.nativeElement.files;
    const options = this.fileUpload.options;
    this.fileUpload.onBeforeItemsAddtoQueue(files, null);
    this.fileUpload.addToUploadQueue(files, options);
    this.fileUpload._initAutoUpload();

    // console.log('Clear value after upload to queue');
    if (this.isEmptyAfterSelection()) {
      this.element.nativeElement.value = '';
    }
  }

  public isEmptyAfterSelection(): boolean {
    return !!this.element.nativeElement.attributes.multiple || !!this.element.nativeElement.attributes.directory;
  }
}
