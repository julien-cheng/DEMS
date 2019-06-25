import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SearchRoutingModule } from './search-routing.module';
// Shared Module
import { SharedModule } from '../shared/shared.module';
import { 
  SearchComponent,
  SearchResultComponent,
  SearchFiltersComponent,
  SearchBadgesComponent,
  SearchPaginationComponent
}  from './index';

@NgModule({
  imports: [
    CommonModule,
        SharedModule,
        SearchRoutingModule,
        FormsModule,
        ReactiveFormsModule
  ],
  declarations: [
    SearchComponent, 
    SearchResultComponent, SearchFiltersComponent, SearchBadgesComponent, SearchPaginationComponent
  ]
})
export class SearchModule { }
