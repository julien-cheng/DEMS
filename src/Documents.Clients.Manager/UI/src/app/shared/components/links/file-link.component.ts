import { Component, OnInit, Input } from '@angular/core';
import { ItemQueryType } from '../../index';

@Component({
  selector: 'app-file-link',
  template: `
      <ng-container *ngIf="!imageLink">
      <a *ngIf="!!row.pathIdentifier && !!row.identifier" 
      [routerLink]="(!!row.identifier && row.pathIdentifier.pathKey.length >0) ? ['/file/', row.identifier.organizationKey, row.identifier.folderKey, row.pathIdentifier?.pathKey, row.identifier.fileKey, row.viewerType ] :
            ['/file/', row.identifier.organizationKey, row.identifier.folderKey, row.identifier.fileKey,row.viewerType ]">
        <i *ngIf="!hideIcon" class="icon {{ row.icons | iconClass: 'file' }}"></i> {{row.name}}
      </a>
      <span *ngIf="row.pathIdentifier === null || row.identifier === undefined">
        <i class="icon {{ row.icons | iconClass: 'file' }}"></i> {{row.name}}
      </span>
    </ng-container>

    <ng-container *ngIf="imageLink">
      <a *ngIf="!!row.pathIdentifier && !!row.identifier" class="imageLink"
        [routerLink]="(!!row.identifier && row.pathIdentifier.pathKey.length >0) ? ['/file/', row.identifier.organizationKey, row.identifier.folderKey, row.pathIdentifier.pathKey, row.identifier.fileKey, row.viewerType ] : ['/file/',row.identifier.organizationKey, row.identifier.folderKey, row.identifier.fileKey, row.viewerType ]">
        <i *ngIf="row.previewImageIdentifier === null || row.previewImageIdentifier?.fileKey === null" class="icon {{ row.icons | iconClass: 'file' }} fa-5x" aria-hidden="true"></i>
        <img *ngIf="!!row.previewImageIdentifier && row.previewImageIdentifier?.fileKey !== null" class="img-thumbnail" 
          [src]="'/api/file/contents?fileidentifier.organizationKey='+ row.previewImageIdentifier.organizationKey+ '&fileidentifier.folderKey=' +row.previewImageIdentifier.folderKey+'&fileidentifier.fileKey=' +row.previewImageIdentifier.fileKey"  />
      </a>
      <span *ngIf="row.pathIdentifier === null || row.identifier === undefined">
        <i class="icon {{ row.icons | iconClass: 'file' }} fa-5x" aria-hidden="true"></i>
      </span>
    </ng-container>
  `,
  styleUrls: ['./links.component.scss']
})
export class FileLinkComponent implements OnInit {
  @Input() row: ItemQueryType;
  @Input() imageLink: boolean= false;
  @Input() hideIcon: boolean=false;
  constructor() { }

  ngOnInit() {
  }

}
