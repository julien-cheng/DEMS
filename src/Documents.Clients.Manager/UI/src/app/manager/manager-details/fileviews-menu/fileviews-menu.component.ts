import { Component, OnInit, Input } from '@angular/core';
import { IFileViewer, IPathIdentifier, IFileIdentifier } from '../../index';
@Component({
  selector: '[app-fileviews-menu]',
  templateUrl: './fileviews-menu.component.html',
  styleUrls: ['./fileviews-menu.component.scss']
})
export class FileviewsMenuComponent implements OnInit {
  @Input() pathIdentifier: IPathIdentifier;
  @Input() fileIdentifier: IFileIdentifier;
  @Input() fileViews: IFileViewer[];
  @Input() viewerType: string;
  //public fileViews: IFileViewer[];
  public fileKey: string;

  constructor() { }

  ngOnInit() {
    this.fileKey = this.fileIdentifier.fileKey;
    // this.fileViews = this.file.views;
    //console.log(this.viewerType,  this.fileViews);
  }
}
