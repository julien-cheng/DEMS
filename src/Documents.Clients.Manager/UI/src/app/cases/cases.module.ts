import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';


// Manager routing module
import { CasesRoutingModule } from './cases-routing.module';
// Shared Module
import { SharedModule } from '../shared/shared.module';


import {
  CaseListComponent
} from './index';

@NgModule({
  imports: [
    CommonModule,
    ReactiveFormsModule,
    SharedModule,
    CasesRoutingModule
  ],
  declarations: [
    CaseListComponent
  ]
})
export class CasesModule { }
