import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { LoadingService, AppConfigService, FileService, BatchOperationsComponent } from '../index';
import {
  IFolderIdentifier,
  IPathIdentifier,
  IFileIdentifier,
  IFile,
  IMediaSet,
  IMediaSource,
  IBatchResponse,
  IMediaSegment,
  IMediaSubtitles,
  IAllowedOperation
} from '../index';
import { MediaToolsService } from '../services/media-tools.service';
import { VideoComponent } from '../video/video.component';

@Component({
  selector: 'app-media-tools',
  templateUrl: './media-tools.component.html',
  styleUrls: ['./media-tools.component.scss']
})
export class MediaToolsComponent implements OnInit {
  @ViewChild(BatchOperationsComponent)
  BatchOperationsComponent: BatchOperationsComponent;
  @ViewChild(VideoComponent) VideoComponent: VideoComponent;
  public folderIdentifier: IFolderIdentifier;
  public pathIdentifier: IPathIdentifier;
  public fileIdentifier: IFileIdentifier;
  public allowedOperations: IAllowedOperation[];
  public mediaSet: IMediaSet;
  public segments: IMediaSegment[];
  // public subtitles: IMediaSubtitles[];
  public activeSegment: IMediaSegment; // Active segment playing or selected manually
  public videoContainerClass = 'col-sm-8'; // 'col-sm-4 order-last'; // TEMP => set to 'col-sm-8'
  public isEditMode = false; // TEMP => set to false
  // public isVideoPlaying = false;
  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private fileService: FileService,
    public mediaToolsService: MediaToolsService,
    public appConfigService: AppConfigService,
    public loadingService: LoadingService
  ) {}

  ngOnInit() {
    this.loadingService.setLoading(false);
    this.route.data.forEach(data => {
      this.folderIdentifier = this.route.snapshot.data.identifiers.folderIdentifier;
      this.pathIdentifier = this.route.snapshot.data.identifiers.pathIdentifier;
      this.fileIdentifier = this.route.snapshot.data.identifiers.fileIdentifier;

      this.route.queryParams.subscribe(params => {
        const startTime: number = params.startTime as number;
        this.setFileObject(startTime);
      });
    });
  }

  // Description Set file Object and View data
  setFileObject(startTime?: number) {
    if (!!this.fileIdentifier) {
      // Call the proper set and pass it to the detailviewType
      this.fileService.getFileMediaSet(this.fileIdentifier, 'transcript').subscribe(
        (response: IBatchResponse) => {
          this.setMediaProperties(response.response as IMediaSet);
          if (!!startTime) {
            const index = this.mediaToolsService.getSegmentIndexByStartTime(this.segments, startTime as number, true);
            this.activeSegment = this.segments[index];
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
    // this.segments = this.mediaSet.segments;
    this.segments = !!this.mediaSet.segments ? this.mediaSet.segments : [];
  }

  // Description: sets the video col class for column rearrangement
  public switchVideoColClass(tab: string) {
    this.isEditMode = tab === 'transcription';
    this.videoContainerClass = tab === 'transcription' ? 'col-sm-4 order-last' : 'col-sm-8';
  }

  //  Description: Change the active segment from within the video component (by playing or seeking in the video)
  public onActiveSegmentChange(activeSegment: IMediaSegment) {
    this.activeSegment = activeSegment;
    this.mediaToolsService.setActiveSegment(this.segments, this.activeSegment);
  }

  // Description: Change the active segment playing on the video from external component: search and transcription
  public changeVideoActiveSegment(activeSegment: IMediaSegment) {
    this.onActiveSegmentChange(activeSegment);
    this.VideoComponent.setActiveSegment(activeSegment);
  }

  // Transcription Component
  public segmentSaveHandler(mediaSet: IMediaSet) {
    // this.processDataResponse(mediaSet);
    this.mediaSet = mediaSet;
    this.allowedOperations = this.mediaSet.allowedOperations;
    this.mediaToolsService.setActiveSegment(this.segments, this.activeSegment);
  }

  // Batch Operations component:
  // ----------------------------------------------------------------
  public processBatchUiAction($event: any) {
    return this.BatchOperationsComponent.processBatchUiAction($event);
  }

  public updateView() {
    // console.log('updateView return call back from Batch Operations Component');
  }
}
