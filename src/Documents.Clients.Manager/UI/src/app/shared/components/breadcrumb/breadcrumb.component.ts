import { Component, SimpleChanges, Input, OnChanges } from '@angular/core';
import { IManager, IPath, IBreadcrumb, IPathIdentifier } from '../../index';
import { ExplorerService } from '../../services/explorer.service';

@Component({
  selector: 'app-breadcrumb',
  templateUrl: './breadcrumb.component.html',
  styleUrls: ['./breadcrumb.component.scss']
})
export class BreadcrumbComponent implements OnChanges {
  @Input() manager: IManager;
  @Input() pathIdentifier: IPathIdentifier;
  @Input() fileName: string;
  public breadcrumbs: IBreadcrumb[];
  public activeNodeKey: string;
  constructor(public explorerService: ExplorerService) {}

  ngOnChanges(simpleChanges: SimpleChanges) {
    !!this.fileName ? (this.activeNodeKey = this.fileName) : (this.activeNodeKey = this.explorerService.fileExplorer.activeNodeKey);
    this.breadcrumbs = this.explorerService.buildBreadCrumbs(this.pathIdentifier);
  }
}
