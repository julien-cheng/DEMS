import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
// Manager routing module
import { MediaRoutingModule } from './media-routing.module';

// Shared Module
import { SharedModule } from '../shared/shared.module';

// MediaTools Components
import {
  MediaToolsComponent,
  MediaSearchComponent,
  VideoComponent,
  MediaSubtitlesComponent,
  TranscriptionReviewComponent,
  MediaResolverService,
  MediaToolsService,
  MediaClipsComponent,
  TimePickerComponent,
  PagerComponent,
  MediaTimelineComponent,
  VideoBasicComponent
} from './index';

@NgModule({
  imports: [CommonModule, SharedModule, MediaRoutingModule, FormsModule, ReactiveFormsModule],
  declarations: [
    VideoComponent,
    VideoBasicComponent,
    MediaSearchComponent,
    MediaToolsComponent,
    TranscriptionReviewComponent,
    PagerComponent,
    MediaSubtitlesComponent,
    TimePickerComponent,
    MediaClipsComponent,
    MediaTimelineComponent
  ],
  providers: [MediaResolverService, MediaToolsService]
  // entryComponents: []
})
export class MediaModule {}
