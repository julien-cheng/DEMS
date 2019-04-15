import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { LoadingService, AppConfigService } from '../index';
import {
  IFolderIdentifier,
  IPathIdentifier,
  IFileIdentifier,
  IBatchResponse,
  FileService,
  IMediaSet,
  IMediaSegment,
  IVideoProperties
} from '../index';
import { IAllowedOperation, BatchOperationService, BatchOperationsComponent, IRequestBatchData, BatchResponse } from '../index';
import { MediaToolsService } from '../services/media-tools.service';
import { MediaTimelineComponent } from '../media-timeline/media-timeline.component';
import { VideoBasicComponent } from '../video-basic/video-basic.component';

@Component({
  selector: 'app-media-clips',
  templateUrl: './media-clips.component.html',
  styleUrls: ['./media-clips.component.scss']
})
export class MediaClipsComponent implements OnInit {
  @ViewChild(BatchOperationsComponent)
  BatchOperationsComponent: BatchOperationsComponent;
  @ViewChild(MediaTimelineComponent)
  mediaTimelineComponent: MediaTimelineComponent;
  @ViewChild(VideoBasicComponent) videoBasicComponent: VideoBasicComponent;
  public folderIdentifier: IFolderIdentifier;
  public pathIdentifier: IPathIdentifier;
  public fileIdentifier: IFileIdentifier;
  public mediaSet: IMediaSet;
  public allowedOperations: IAllowedOperation[];
  public requestTypesFlags: any;
  public videoProperties: IVideoProperties;
  public videoDuration: number;
  public activeSegment: IMediaSegment;
  public segments: IMediaSegment[] = [];
  private _activeSegmentTriggeredPause = false; // For pausing video at end of active segment
  public videoStartTime = 0; // number in sec
  constructor(
    private route: ActivatedRoute,
    private fileService: FileService,
    public batchOperationService: BatchOperationService,
    public mediaToolsService: MediaToolsService,
    public loadingService: LoadingService,
    public toastr: ToastrService
  ) {}

  ngOnInit() {
    this.loadingService.setLoading(false);
    this.route.data.forEach(data => {
      this.folderIdentifier = this.route.snapshot.data.identifiers.folderIdentifier;
      this.pathIdentifier = this.route.snapshot.data.identifiers.pathIdentifier;
      this.fileIdentifier = this.route.snapshot.data.identifiers.fileIdentifier;
      this.route.queryParams.subscribe(params => {
        const startTime = params.startTime as number,
          endTime = params.endTime as number;
        this.setFileObject(Number(startTime), Number(endTime));
      });
    });
  }

  // Description Set file Object and View data
  setFileObject(startTime?: number, endTime?: number) {
    if (!!this.fileIdentifier) {
      // Call the proper set and pass it to the detailviewType
      this.fileService.getFileMediaSet(this.fileIdentifier, 'clip').subscribe(
        (response: IBatchResponse) => {
          this.setMediaProperties(response.response as IMediaSet);
          // Preselect a segment and set video to startTime
          if (!!startTime && !!endTime) {
            const index = this.mediaToolsService.getSegmentIndexByTime(this.segments, Number(startTime), Number(endTime));
            if (index >= 0) {
              this.activeSegment = this.segments[index];
              this.videoStartTime = startTime / 1000;
            } else {
              this.toastr.warning('The segment you are looking for does not exist.');
            }
          }
          this.segments = this.mediaToolsService.setActiveSegment(this.segments, this.activeSegment);
        },
        error => {
          console.error(error);
        },
        () => {
          this.loadingService.setLoading(false);
        }
      );
    }
  }

  private setMediaProperties(mediaSet: IMediaSet) {
    this.mediaSet = mediaSet;
    this.allowedOperations = this.mediaSet.allowedOperations;
    this.requestTypesFlags = Object.assign({}, this.batchOperationService.extractrequestTypesFlags(this.allowedOperations));
    this.segments = !!this.mediaSet.segments ? this.mediaToolsService.defaultNullableMuteSegment(this.mediaSet.segments) : [];
  }

  // Description: change active segment trigger from search component
  public changeClipActiveSegmentSearch(activeSegment: IMediaSegment) {
    // console.log('changeClipActiveSegmentSearch: ', activeSegment);
    this.videoBasicComponent.setVideoCurrentTime(activeSegment.startTime / 1000);
    this.changeClipActiveSegment(activeSegment);
  }

  // Description: Change the active segment playing on the video from external component: search and transcription
  public changeClipActiveSegment(activeSegment: IMediaSegment) {
    // console.log('changeClipActiveSegment', activeSegment);
    this.activeSegment = activeSegment; // Reset activeSegment if undefined
    !!this.activeSegment
      ? this.mediaToolsService.setActiveSegment(this.segments, this.activeSegment)
      : this.mediaToolsService.resetActiveSegment(this.segments);
    this._activeSegmentTriggeredPause = false;
  }

  // Update interface for new segments
  public onSegmentSave(mediaSet: IMediaSet) {
    //  console.log('onSegmentSave', mediaSet);
    this.mediaSet = mediaSet;
    this.allowedOperations = this.mediaSet.allowedOperations;
    this.mediaToolsService.setActiveSegment(this.segments, this.activeSegment);
  }

  // Video Basic Component:
  // -----------------------------------------

  public onVideoUpdate(currentTime: number) {
    const msCurrentTime = currentTime * 1000;
    if (!isNaN(currentTime) && currentTime >= 0) {
      this.mediaTimelineComponent.updateVideoCurrentTime(msCurrentTime);
    }

    // check active segment and pause at the end of it
    if (!!this.activeSegment && this.activeSegment.endTime <= msCurrentTime && !this._activeSegmentTriggeredPause) {
      this.videoBasicComponent.pauseVideo();
      this._activeSegmentTriggeredPause = true;
    }
  }

  public updateVideoProperties(videoProperties: IVideoProperties) {
    this.videoProperties = videoProperties;
    this.videoDuration = !!this.videoProperties.duration ? this.videoProperties.duration * 1000 : 0;
  }

  // Batch operations:
  public processBatchUiAction(requestBatchData: IRequestBatchData) {
    if (requestBatchData.requestType === 'ExportClipRequest') {
      if (!!requestBatchData.batchOperations && requestBatchData.batchOperations.length > 0) {
        requestBatchData.batchOperations[0].clip = this.activeSegment;
      }
    }

    if (requestBatchData.requestType === 'ExportFrameRequest') {
      if (!!requestBatchData.batchOperations && requestBatchData.batchOperations.length > 0) {
        requestBatchData.batchOperations[0].milliseconds = this.videoBasicComponent.player.nativeElement.currentTime * 1000;
      }
    }
    return this.BatchOperationsComponent.processBatchUiAction(requestBatchData);
  }

  public updateView(batchResponse?: BatchResponse) {
    //  console.log('updateView return call back from Batch Operations Component', batchResponse);
  }
}
