import { Component, OnInit, Input } from '@angular/core';
import { ItemQueryType } from '../../index';

@Component({
  selector: 'app-file-link',
  templateUrl: './file-link.component.html',
  styleUrls: ['./file-link.component.scss']
})
export class FileLinkComponent implements OnInit {
  @Input() row: ItemQueryType;
  @Input() imageLink = false;
  @Input() hideIcon = false;
  constructor() {}

  ngOnInit() {}
}
