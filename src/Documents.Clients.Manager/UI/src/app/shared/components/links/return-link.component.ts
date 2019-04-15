import { Component, OnInit, Input } from '@angular/core';
import { IPathIdentifier } from '../../index';

@Component({
  selector: 'app-return-link',
  template: `
    <a class="small" *ngIf="!!pathIdentifier" [routerLink]="routingArr" title="Return to {{text}}"
      ><i [class]="iconClass"></i> <span>{{ text }}</span></a
    >
  `,
  styleUrls: ['./links.component.scss']
})
export class ReturnLinkComponent implements OnInit {
  @Input() pathIdentifier: IPathIdentifier;
  @Input() hideIcon = false;
  @Input() iconClass = 'fa fa-reply';
  @Input() text = 'Return';
  public routingArr = [];
  constructor() {}

  ngOnInit() {
    this.routingArr = ['/manager/', this.pathIdentifier.organizationKey, this.pathIdentifier.folderKey];
    !!this.pathIdentifier.pathKey && this.routingArr.push(this.pathIdentifier.pathKey);
  }
}
