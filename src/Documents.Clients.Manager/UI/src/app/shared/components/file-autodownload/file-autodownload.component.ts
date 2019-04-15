import { Component, OnInit, Input } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { IAutodownloadKeys, IPathIdentifier, IFileIdentifier, IFile } from '../../index';
@Component({
  selector: 'app-file-autodownload',
  templateUrl: './file-autodownload.component.html',
  styleUrls: ['./file-autodownload.component.scss']
})
export class FileAutodownloadComponent implements OnInit {
  @Input() pathIdentifier: IPathIdentifier;
  @Input() autodownloadKeys: IAutodownloadKeys;
  folderKey: string;
  saferesourceURLs: SafeResourceUrl[] = [];
  constructor(private sanitizer: DomSanitizer) {}

  ngOnInit() {
    const autodownloadKeys: string[] = this.autodownloadKeys.autodownloadString.split('||');
    const fileIdentifiers: IFileIdentifier[] = autodownloadKeys.map(entry => {
      const obj = JSON.parse(entry);
      // When we parse this guy, we get uppercase keys.  We need lowercase keys.
      obj.fileKey = obj.FileKey;
      obj.folderKey = obj.FolderKey;
      obj.organizationKey = obj.OrganizationKey;
      return obj as IFileIdentifier;
    });

    this.saferesourceURLs = fileIdentifiers.map(fileIdentifier => {
      const url = `/api/file/contents?fileidentifier.organizationKey=${fileIdentifier.organizationKey}&fileidentifier.folderKey=${
        fileIdentifier.folderKey
      }&fileidentifier.fileKey=${fileIdentifier.fileKey}&open=false`;
      return this.sanitizer.bypassSecurityTrustResourceUrl(url);
    });
  }
}
