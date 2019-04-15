import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
// Manager routing module
import { ManagerRoutingModule } from './manager-routing.module';

// Shared Module
import { SharedModule } from '../shared/shared.module';
// Media Module
import { MediaModule } from '../media/media.module';
import { VideoBasicComponent } from '../media/video-basic/video-basic.component';
// Manager Components
import {
  // ManagerComponent,
  ManagerDetailsComponent,
  PdfComponent,
  ImageComponent,
  AudioComponent,
  UnknownComponent,
  FileviewsMenuComponent,
  ManagerViewComponent,
  GridViewComponent,
  ObjectViewComponent,
  ListViewComponent,
  IconsViewComponent,
  DetailsViewComponent,
  FileResolver,
  OfflineComponent,
  ActionToolbarComponent,
  DefaultComponent,
  UploadComponent,
  JsonFormsComponent,
  CellViewComponent,
  SearchComponent,
  SearchResultComponent,
  ImageGalleryComponent,
  ImageGalleryThumbnailComponent
} from './index';

@NgModule({
  imports: [CommonModule, SharedModule, ManagerRoutingModule, FormsModule, ReactiveFormsModule, MediaModule],
  declarations: [
    ManagerDetailsComponent,
    ListViewComponent,
    IconsViewComponent,
    DetailsViewComponent,
    CellViewComponent,
    PdfComponent,
    ImageComponent,
    OfflineComponent,
    AudioComponent,
    UnknownComponent,
    ActionToolbarComponent,
    DefaultComponent,
    UploadComponent,
    JsonFormsComponent,
    ManagerViewComponent,
    GridViewComponent,
    ObjectViewComponent,
    SearchComponent,
    SearchResultComponent,
    FileviewsMenuComponent,
    ImageGalleryComponent,
    ImageGalleryThumbnailComponent
  ],
  entryComponents: [DefaultComponent, VideoBasicComponent, PdfComponent, ImageComponent, AudioComponent, OfflineComponent, UnknownComponent]
})
export class ManagerModule {}
