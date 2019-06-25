import { Routes } from '@angular/router';
import { ErrorsComponent } from './errors/errors.component';


// Shared Components and services
import {
    AuthGuardService // Route guards
} from './shared/index';

export const appRoutes: Routes = [
    // Add routes here
    { path: '', redirectTo: '/login', pathMatch: 'full' },
    { path: '', loadChildren: 'app/auth/auth.module#AuthModule', canActivate: [AuthGuardService] },
    { path: '', loadChildren: 'app/manager/manager.module#ManagerModule', canActivate: [AuthGuardService]},
    { path: '', loadChildren: 'app/cases/cases.module#CasesModule', canActivate: [AuthGuardService]},
    { path: '', loadChildren: 'app/media/media.module#MediaModule', canActivate: [AuthGuardService]},
    { path: '', loadChildren: 'app/search/search.module#SearchModule', canActivate: [AuthGuardService]},
    { path: 'error', component: ErrorsComponent },
    {path: '**', component: ErrorsComponent}
];
