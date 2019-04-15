// Angular modules
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TreeModule } from 'angular-tree-component';
import { NouisliderModule } from 'ng2-nouislider';
import { PerfectScrollbarModule, PerfectScrollbarConfigInterface, PERFECT_SCROLLBAR_CONFIG } from 'ngx-perfect-scrollbar';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { FileUploadModule } from '../file-upload/file-upload.module';
import { Ng4SchemaFormsModule } from '../ng4-schema-forms/ng4-schema-forms.module';

import {
  // Components
  BootstrapModalComponent,
  FileUploadComponent,
  FileAutodownloadComponent,
  FileExplorerComponent,
  PreviewPanelComponent,
  InlineEditorComponent,
  LoadingComponent,
  FileDownloadComponent,
  OperationsMenuComponent,
  BreadcrumbComponent,
  SearchFormComponent,
  SearchBarComponent,
  FileLinkComponent,
  ReturnLinkComponent,
  LinkComponent,
  PaginationComponent,
  BatchOperationsComponent,
  MessagingComponent,

  // Services (NOTE: DO NOT ADD EXPLORER SERVICE HERE)
  BatchOperationService,
  FolderService,
  FileService,
  FileResolver,
  PathService,
  SearchService,
  ManagerResolver,
  SearchResolver,
  ListService,
  AuthGuardService,
  AuthService,
  PaginationService,
  DateService,

  // Pipes
  IconPipe,
  FileSizePipe,
  FileTypePipe,
  FiletypeIconPipe,
  QuerystringPipe,
  IterativeObjectPipe,
  FormatTimePipe,

  // Directives
  FileSortableDirective,
  ModalTriggerDirective,
  GotopDirective,
  ClipboardCopyDirective,
  InputTrimDirective,
  TimeStringValidatorDirective,
  TimeRangeMinDirective,
  TimeDurationMaxDirective
} from './index';

const DEFAULT_PERFECT_SCROLLBAR_CONFIG: PerfectScrollbarConfigInterface = {
  wheelPropagation: true
};

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    TreeModule.forRoot(),
    FileUploadModule,
    Ng4SchemaFormsModule,
    NouisliderModule,
    PerfectScrollbarModule
  ],
  declarations: [
    // pipes and componenets
    IconPipe,
    FileSizePipe,
    FiletypeIconPipe,
    QuerystringPipe,
    IterativeObjectPipe,
    FileTypePipe,
    FormatTimePipe,
    FileUploadComponent,
    FileAutodownloadComponent,
    FileDownloadComponent,
    FileExplorerComponent,
    PreviewPanelComponent,
    PreviewPanelComponent,
    BootstrapModalComponent,
    FileAutodownloadComponent,
    InlineEditorComponent,
    BatchOperationsComponent,
    MessagingComponent,
    FileSortableDirective,
    ModalTriggerDirective,
    GotopDirective,
    IterativeObjectPipe,
    LoadingComponent,
    OperationsMenuComponent,
    BreadcrumbComponent,
    SearchFormComponent,
    SearchBarComponent,
    FileLinkComponent,
    ReturnLinkComponent,
    LinkComponent,
    PaginationComponent,
    ClipboardCopyDirective,
    InputTrimDirective,
    TimeStringValidatorDirective,
    TimeRangeMinDirective,
    TimeDurationMaxDirective
  ],
  exports: [
    FileUploadComponent,
    FileAutodownloadComponent,
    FileDownloadComponent,
    OperationsMenuComponent,
    FileExplorerComponent,
    PreviewPanelComponent,
    InlineEditorComponent,
    LoadingComponent,
    BreadcrumbComponent,
    SearchFormComponent,
    SearchBarComponent,
    FileLinkComponent,
    ReturnLinkComponent,
    LinkComponent,
    PaginationComponent,
    BatchOperationsComponent,
    BootstrapModalComponent,
    MessagingComponent,
    TreeModule,
    IconPipe,
    FiletypeIconPipe,
    FileSizePipe,
    FileTypePipe,
    IterativeObjectPipe,
    FormatTimePipe,
    FileSortableDirective,
    ModalTriggerDirective,
    GotopDirective,
    ClipboardCopyDirective,
    InputTrimDirective,
    TimeStringValidatorDirective,
    TimeRangeMinDirective,
    TimeDurationMaxDirective,
    FileUploadModule,
    Ng4SchemaFormsModule,
    NouisliderModule,
    PerfectScrollbarModule
  ],
  providers: [
    // Services - NOTE: DO NOT ADD EXPLORER SERVICE HERE
    BatchOperationService,
    FolderService,
    FileService,
    FileResolver,
    PathService,
    SearchService,
    PaginationService,
    ManagerResolver,
    SearchResolver,
    ListService,
    AuthGuardService,
    AuthService,
    DateService,
    IconPipe,
    FiletypeIconPipe,
    FileSizePipe,
    FileTypePipe,
    IterativeObjectPipe,
    QuerystringPipe,
    FormatTimePipe,
    {
      provide: PERFECT_SCROLLBAR_CONFIG,
      useValue: DEFAULT_PERFECT_SCROLLBAR_CONFIG
    }
  ],
  entryComponents: []
})
export class SharedModule {}
