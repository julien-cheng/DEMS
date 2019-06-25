import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

// Components
import { CaseListComponent } from './index';

const routes: Routes = [
    {
        path: 'case-list',
        redirectTo: 'case-list/',
        pathMatch: 'full'
    },
    {
        path: 'case-list/:organizationKey',
        component: CaseListComponent
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
    providers: []
})

export class CasesRoutingModule { }