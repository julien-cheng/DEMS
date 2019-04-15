import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

// Shared Components and services
import { SearchComponent, SearchResolver } from './index';

const routes: Routes = [
  // {
  //     path: 'search/:organizationKey',
  //     redirectTo: 'search/:organizationKey/',
  //     pathMatch: 'full'
  // },
  {
    path: 'search/:organizationKey',
    component: SearchComponent,
    resolve: { searchData: SearchResolver }
  },
  {
    path: 'search/:organizationKey/:folderKey',
    component: SearchComponent,
    resolve: { searchData: SearchResolver }
  },
  {
    path: 'search/:organizationKey/:folderKey/:pathKey',
    component: SearchComponent,
    resolve: { searchData: SearchResolver }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
  providers: []
})
export class SearchRoutingModule {}
