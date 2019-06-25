import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
// import { SharedModule } from '../shared/shared.module';

import {
  FileSelectDirective,
  FileDropDirective,
  FileUpload,
  FileUploadComponent,
  IFileUploadOptions
} from './index';


@NgModule({
  imports: [
    CommonModule,
    HttpClientModule
  //  SharedModule
  ],
  declarations: [
    FileSelectDirective,
    FileDropDirective,
    FileUploadComponent
  ],
  exports: [
    FileDropDirective,
    FileSelectDirective,
    FileUploadComponent

  ],
  providers: [FileUpload]
})
export class FileUploadModule { }
