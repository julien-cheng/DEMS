import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';


// Shared Components and services
import {
    MediaToolsComponent,
    MediaClipsComponent,
    MediaResolverService
} from './index';

const routes: Routes = [
    {
        path: 'media/:organizationKey/:folderKey',
        redirectTo: 'media/:organizationKey/:folderKey/',
        pathMatch: 'full'
    },
    {
        path: 'media/:organizationKey/:folderKey/:fileKey',
        component: MediaToolsComponent,
         resolve: { identifiers: MediaResolverService }
    },
    {
        path: 'media/:organizationKey/:folderKey/:pathKey/:fileKey',
        component: MediaToolsComponent,
        resolve: { identifiers: MediaResolverService }
    },
    {
        path: 'mediaclips/:organizationKey/:folderKey/:fileKey',
        component: MediaClipsComponent,
         resolve: { identifiers: MediaResolverService }
    },
    {
        path: 'mediaclips/:organizationKey/:folderKey/:pathKey/:fileKey',
        component: MediaClipsComponent,
        resolve: { identifiers: MediaResolverService }
    }
];


@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
    providers: []
})
export class MediaRoutingModule { }
