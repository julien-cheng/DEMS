import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { ItemQueryType, IRequestBatchData, LinkType } from '../../index';

@Component({
  selector: 'app-link',
  templateUrl: './links.component.html',
  styleUrls: ['./links.component.scss']
})
export class LinkComponent implements OnInit {
  @Output() triggerCallback = new EventEmitter();
  @Input() row: ItemQueryType;
  @Input() pathName: string;
  @Input() imageLink = false;
  @Input() hideIcon = false;
  @Input() linkType: string;
  constructor() {}

  ngOnInit() {
    this.linkType === undefined && !!this.row && (this.linkType = LinkType[this.row.type]);
  }
}
