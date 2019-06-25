import { Component, OnInit, Input } from '@angular/core';
import { ItemQueryType, IFile, ISearch, IterativeObjectPipe, DateService } from '../index';

@Component({
  selector: 'app-search-result',
  templateUrl: './search-result.component.html',
  styleUrls: ['./search-result.component.scss']
})
export class SearchResultComponent {
  @Input() searchResult: ISearch;
  constructor(public dateService: DateService) {}
  
  ngOnChanges() {
    let iterativeObjectPipe: IterativeObjectPipe = new IterativeObjectPipe();
    this.searchResult.rows.every(row=>{
      !!row.attributes && (row.attributes= iterativeObjectPipe.transform(row.attributes) || null);
      return true;
    });
  }

  // Description: Builds ItemQueryType row object from IFIle to build link
  buildPathRow(row: IFile): ItemQueryType{
      row = Object.assign({}, row, {
          name: row.fullPath || 'Case Files',
          icons: ['folder'],
          identifier: row.pathIdentifier
      });
      return row;
  }
}
