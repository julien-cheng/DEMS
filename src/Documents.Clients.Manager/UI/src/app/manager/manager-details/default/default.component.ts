import { Component, OnInit, Input } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { IFile, IFileIdentifier, FileSetTypes, ITextSet } from '../../index';

@Component({
  selector: 'app-default',
  templateUrl: './default.component.html',
  styleUrls: ['./default.component.scss']
})
export class DefaultComponent implements OnInit {
  @Input() fileSet: FileSetTypes;
  @Input() viewerType: string;
  public fileIdentifier: IFileIdentifier;
  public fileUrl: string;
  public saferesourceURL: SafeResourceUrl;

  constructor(private sanitizer: DomSanitizer) {}

  ngOnInit() {
    this.fileIdentifier =
      !!(this.fileSet as ITextSet).fileIdentifier && this.viewerType === 'text'
        ? (this.fileSet as ITextSet).fileIdentifier
        : this.fileSet.rootFileIdentifier;
    this.fileUrl = `/api/file/contents?fileidentifier.organizationKey=${this.fileIdentifier.organizationKey}&fileidentifier.folderKey=${
      this.fileIdentifier.folderKey
    }&fileidentifier.fileKey=${this.fileIdentifier.fileKey}&open=true`;
    this.saferesourceURL = this.sanitizer.bypassSecurityTrustResourceUrl(this.fileUrl);
  }
}
