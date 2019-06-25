import { Component, OnInit, Input } from '@angular/core';
import {DomSanitizer, SafeResourceUrl} from '@angular/platform-browser';
import {IFile, IFileIdentifier, IImageSet } from '../../index';

@Component({
  selector: 'app-image',
  templateUrl: './image.component.html',
  styleUrls: ['./image.component.scss']
})
export class ImageComponent implements OnInit {
  @Input() fileSet: IImageSet;
  @Input() viewerType: string;
  public fileIdentifier: IFileIdentifier;
  public fileUrl:string;
  public saferesourceURL: SafeResourceUrl;
  
  constructor(private sanitizer: DomSanitizer) { }

  ngOnInit() {
    // console.log(this.viewerType, this.file, this.fileSet);
    this.fileIdentifier = this.fileSet.fileIdentifier;
    this.fileUrl = `/api/file/contents?fileidentifier.organizationKey=${this.fileIdentifier.organizationKey}&fileidentifier.folderKey=${this.fileIdentifier.folderKey}&fileidentifier.fileKey=${this.fileIdentifier.fileKey}&open=true`;
    this.saferesourceURL = this.sanitizer.bypassSecurityTrustResourceUrl(this.fileUrl);
  }

}
