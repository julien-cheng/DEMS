import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

// Shared Components and services
import {
  ManagerDetailsComponent,
  FileResolver,
  ManagerViewComponent,
  ManagerResolver,
  // SearchResolver,
  UploadComponent,
  SearchComponent,
  JsonFormsComponent
} from './index';

// Route guards
// import { AuthGuard } from '../shared/services/auth-guard.service';

const routes: Routes = [
  {
    path: 'manager/:organizationKey/:folderKey',
    component: ManagerViewComponent,
    resolve: { identifiers: ManagerResolver }
  },
  {
    path: 'manager/:organizationKey/:folderKey/:pathKey',
    component: ManagerViewComponent,
    resolve: { identifiers: ManagerResolver }
  },
  {
    path: 'file/:organizationKey/:fileKey',
    redirectTo: 'file/:organizationKey/:folderKey/',
    pathMatch: 'full'
  },
  {
    path: 'file/:organizationKey/:folderKey/:fileKey/:viewerType',
    component: ManagerDetailsComponent,
    resolve: { identifiers: FileResolver }
  },
  {
    path: 'file/:organizationKey/:folderKey/:pathKey/:fileKey/:viewerType',
    component: ManagerDetailsComponent,
    resolve: { identifiers: FileResolver }
  },
  {
    path: 'upload/:organizationKey/:folderKey',
    redirectTo: 'upload/:folderKey/',
    pathMatch: 'full'
  },
  {
    path: 'upload/:organizationKey/:folderKey/:pathKey',
    component: UploadComponent,
    resolve: { pathIdentifier: ManagerResolver }
  },
  {
    path: 'schema-forms',
    component: JsonFormsComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
  providers: []
})
export class ManagerRoutingModule {}
