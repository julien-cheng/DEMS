import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { LoginComponent, EdiscoveryLandingComponent, LeoLandingComponent } from './index';

const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'ediscoverylanding/:token',
    component: EdiscoveryLandingComponent
  },
  {
    path: 'leouploadlanding/:token',
    component: LeoLandingComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
  providers: []
})
export class AuthRoutingModule {}
