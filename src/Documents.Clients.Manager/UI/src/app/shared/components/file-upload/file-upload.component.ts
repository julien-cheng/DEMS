import { Component, OnChanges, ViewChild, ViewContainerRef, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastsManager } from 'ng2-toastr';
import { AuthService } from '../../services/auth.service';
import { IPathIdentifier } from '../../models/identifiers.model';
import { FileUpload, IFileUploadOptions } from '../../../file-upload/index';
import { IAppConfiguration } from '../../services/app-config.service';

@Component({
  selector: 'app-file-upload',
  templateUrl: './file-upload.component.html',
  styleUrls: ['./file-upload.component.scss']
})
export class FileUploadComponent {
  @Input() appConfiguration: IAppConfiguration;
  @Input() pathIdentifier: IPathIdentifier;
  @Output() updateView = new EventEmitter();
  public url: string;
  public options: IFileUploadOptions;
  public autoUpload: boolean = true;
  constructor(
    public auth: AuthService,
    public fileUpload: FileUpload,
    private toastr: ToastsManager) {
  }

  ngOnChanges(simpleChanges: SimpleChanges) {
    this.url = '/api/upload?pathIdentifier.organizationKey=' + this.pathIdentifier.organizationKey + '&pathIdentifier.folderKey=' + this.pathIdentifier.folderKey + (this.pathIdentifier.pathKey ? ('&pathIdentifier.pathKey=' + encodeURIComponent(this.pathIdentifier.pathKey)) : '');
    this.options = { url: this.url };
    if (!!this.appConfiguration) {
      this.autoUpload = this.appConfiguration.autoUpload;
      Object.keys(this.appConfiguration).map((key) => {
        (this.fileUpload.options.hasOwnProperty(key)) && (this.fileUpload.options[key] = this.appConfiguration[key]);
      });
    }
  }

  onCompleteCallback(event: any) {
    this.updateView.emit();
    let msg = !!event.fileObject.file.name ? (event.fileObject.file.name +' was uploaded successfully!'): 'This file was uploaded successfully!';
    this.postMessage(msg, 'success');
  }


  postDropMessage(event) {
    this.postMessage(event.msg, event.type);
  }

  postMessage = function (msg: string, type: string) {
    switch (type) {
      case 'success':
        this.toastr.success(msg);
        break;
      case 'error':
        this.toastr.error(msg);
        break;
      default:
        break;
    }
  };
}
