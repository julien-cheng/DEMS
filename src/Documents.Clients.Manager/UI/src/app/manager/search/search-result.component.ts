import { Component, OnInit, Input } from '@angular/core';
import { ItemQueryType, IFile } from '../index';
@Component({
  selector: 'app-search-result',
  templateUrl: './search-result.component.html',
  styleUrls: ['./search.component.scss']
})
export class SearchResultComponent implements OnInit {
  @Input() row: IFile;
  constructor() {}
  ngOnInit() {}

  // Builds ItemQueryType row object from IFIle to build link
  buildPathRow(row: IFile): ItemQueryType {
    row = Object.assign({}, row, {
      name: row.fullPath || 'Case Files',
      icons: ['folder'],
      identifier: row.pathIdentifier
    });
    return row;
  }
}
