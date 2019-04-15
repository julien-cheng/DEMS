import { Directive, EventEmitter, ElementRef, HostListener, Input, Output, NgZone, OnDestroy } from '@angular/core';
import { Observable } from 'rxjs/Observable';
import { ISubscription } from 'rxjs/Subscription';
import { FileUpload, IUploadedFile, FileObject } from '../index';

@Directive({
  selector: '[appFileDrop]'
})
export class FileDropDirective implements OnDestroy {
  @Input() public fileUpload: FileUpload;
  @Output() postMessage = new EventEmitter();
  protected element: ElementRef;
  private subscription: ISubscription;

  public constructor(element: ElementRef, private zone: NgZone) {
    this.element = element;
  }

  @HostListener('document:drop', ['$event'])
  public onDrop(event: any): void {
    //  console.log('FileDropDirective drop');
    const transfer = this._getTransfer(event);
    if (!this._transferHaveFiles(transfer.types)) {
      return;
    }
    //  console.log('FileDropDirective drop PASSED');
    // Drag and drop browser compliant
    this.fileUpload.isIE11 ? this._subscribetoDataListforIE11(transfer) : this._subscribetoDataList(transfer);

    this._preventAndStop(event);
  }

  // Description: Drop call for most modern browsers (not IE11)
  protected _subscribetoDataList(transfer: any) {
    // for directory upload
    // console.log('HANDLE modern browsers - not IE11');
    const options = this.fileUpload.options;
    const items: DataTransferItemList = transfer.items;
    const newQueue: Observable<IUploadedFile[]> = this.fileUpload.prepareUploadQueueItems(items);

    this.subscription = newQueue.subscribe(
      response => {
        this.zone.run(() => {
          this.fileUpload.addToUploadQueue(response, options);
          this.fileUpload._initAutoUpload();
        });
      },
      error => {
        console.error('There was an error with the files or folders dragged into the browser');
        this.postMessage.emit({
          msg: 'There was an error with the files or folders dragged into the browser',
          type: 'error'
        });
      },
      () => {
        // console.log('Subscription Finished!');
      }
    );
  }

  // Description: Drop call IE11
  protected _subscribetoDataListforIE11(transfer: any) {
    // console.log('HANDLE IE11 drag and drop', transfer);
    const files = transfer.files;
    // console.log(files);
    if (files.length) {
      const options = this.fileUpload.options;
      this.fileUpload.onBeforeItemsAddtoQueue(files, null);
      this.fileUpload.addToUploadQueue(files, options);
      this.fileUpload._initAutoUpload();
    } else {
      console.error('Directories upload is not supported in IE11');
      this.postMessage.emit({
        msg: 'Directory upload is not supported in IE11',
        type: 'error'
      });
    }
  }

  @HostListener('document:dragover', ['$event'])
  public onDragOver(event: any): void {
    // console.log("FileDropDirective dragover");
    this._preventAndStop(event);
  }

  protected _getTransfer(event: any): any {
    return event.dataTransfer ? event.dataTransfer : event.originalEvent.dataTransfer;
  }

  protected _preventAndStop(event: any): any {
    event.preventDefault();
    event.stopPropagation();
  }

  protected _transferHaveFiles(types: any): any {
    if (!types) {
      return false;
    }

    if (types.indexOf) {
      return types.indexOf('Files') !== -1;
    } else if (types.contains) {
      return types.contains('Files');
    } else {
      return false;
    }
  }

  ngOnDestroy() {
    // console.log('destroy subscription');
    if (!!this.subscription) {
      this.subscription.unsubscribe();
    }
  }
}
