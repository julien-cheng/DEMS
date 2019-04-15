import {
  Component,
  OnInit,
  Input,
  Output,
  EventEmitter,
  ViewChild,
  ElementRef,
  SimpleChanges,
  ViewContainerRef,
  HostListener,
  AfterViewInit
} from '@angular/core';
import { IMediaSet, IMediaSource, IMediaSubtitles, IMediaSegment, IVideoProperties, EventType } from '../index';
import { MediaToolsService } from '../services/media-tools.service';
import * as _ from 'lodash';
const { isEqual } = _;

@Component({
  selector: 'app-video',
  templateUrl: './video.component.html',
  styleUrls: ['./video.component.scss']
})
export class VideoComponent implements OnInit, AfterViewInit {
  @Output() onActiveSegmentChange = new EventEmitter<IMediaSegment>();
  @ViewChild('player') player: ElementRef;
  @ViewChild('playerContainer', { read: ViewContainerRef })
  playerContainer: ViewContainerRef;
  @Input() mediaSet: IMediaSet;
  @Input() activeSegment: IMediaSegment;
  @Input() isEditMode: boolean;
  public loopVideoSegment = false;
  public segments: IMediaSegment[];

  private _activeSegment: IMediaSegment;
  private _segmentMap: any = {};
  private _segmentKeys: Array<number> = [];
  private video: HTMLVideoElement;
  private _nextSegmentKey: number = null; // Store key of next active segment if playing =>Set to null when there are no active segments, -1 when it is the last segment
  private _activeSegmentEndTime: number = null; // stores the current active segment endtime

  // Overall Video props
  public sources: IMediaSource[];
  public subtitles: IMediaSubtitles[];
  public poster: string;
  public preload: 'auto' | 'none' | 'metadata';
  public videoProperties: IVideoProperties = {
    isPlaying: false,
    isMuted: false,
    isEnded: false,
    isClosedCaptionsOn: false,
    duration: 0
  };

  constructor(private mediaToolsService: MediaToolsService) {}

  ngOnInit() {
    if (!!this.mediaSet.poster) {
      this.poster = this.mediaToolsService.getFileContentURL(this.mediaSet.poster);
    }
    this.preload = this.mediaSet.preload ? 'auto' : 'none';
    this.segments = this.mediaSet.segments;
    this.processSegmentObject(this.segments);
    this.sources = this.mediaToolsService.buildMediaSources(this.mediaSet.sources);
    if (this.mediaSet.subtitles) {
      this.subtitles = this.mediaToolsService.buildMediaSubtitles(this.mediaSet.subtitles);
    }
    this._activeSegment = this.activeSegment; // || this.mediaSet.segments[0];
  }

  ngAfterViewInit() {
    const self = this;
    this.video = this.player.nativeElement;
    // Bind Video Events
    this.video.addEventListener('loadedmetadata', () => {
      // console.log( 'loadedmetadata');
      this.setVideoProperties();
      if (!!self._activeSegment && self._activeSegment.startTime > 0) {
        self.video.currentTime = self._activeSegment.startTime / 1000;
      }
    });

    this.video.addEventListener('timeupdate', e => {
      const currentTime = this.video.currentTime * 1000;
      if (!!this._activeSegmentEndTime && currentTime > this._activeSegmentEndTime) {
        this._endOfSegmentCallback(currentTime);
      }
      if (this._nextSegmentKey >= 0 && (this._nextSegmentKey === null || currentTime >= this._nextSegmentKey)) {
        this.findActiveSegment(this.video.currentTime * 1000);
      }
    });

    this.video.addEventListener('seeked', e => {
      // this._activeSegment, this.activeSegment,
      this._nextSegmentKey = null;
      this._activeSegmentEndTime = null;
    });

    this.video.addEventListener('play', e => {
      this.videoProperties.isPlaying = true;
      this.videoProperties.isEnded = false;
    });

    this.video.addEventListener('pause', e => {
      this.videoProperties.isPlaying = false;
    });

    this.video.addEventListener('ended', e => {
      this.videoProperties.isPlaying = false;
      this.videoProperties.isEnded = true;
    });
  }

  setVideoProperties() {
    this.videoProperties.textTrackMode = this.video.textTracks[0].mode;
    this.videoProperties.isClosedCaptionsOn = this.videoProperties.textTrackMode === 'showing';
    this.videoProperties.isMuted = this.video.muted;
    this.videoProperties.duration = this.video.duration;
    // console.log(this.video.duration, this.videoProperties.duration);
  }

  // Description: Set Base Segments Object and Array
  processSegmentObject(segments: IMediaSegment[]) {
    this.segments.forEach(segment => {
      if (!!segment.startTime) {
        this._segmentMap[segment.startTime.toString()] = segment;
      }
    });
    this._segmentKeys = Object.keys(this._segmentMap).map(Number);
  }

  // Finds the active Segment
  findActiveSegment(currentTime: number) {
    // console.log(' | ----------------- findActiveSegment ------------------------ | ', this._nextSegmentKey);
    let _activeSegment = null,
      foundSegmentBigger,
      foundSegmentSmaller;
    const foundIndex = this._segmentKeys.findIndex(el => el > currentTime);

    if (foundIndex < 0) {
      // This is the last segment
      foundSegmentSmaller = this._segmentKeys[this._segmentKeys.length - 1];
      this._nextSegmentKey = -1;
    } else {
      foundSegmentBigger = this._segmentKeys[foundIndex];
      foundSegmentSmaller = this._segmentKeys[foundIndex - 1]; //
      this._nextSegmentKey = foundSegmentBigger; // Set next segment, end of active segment and global active segment eventemitters
    }

    // Set Segments actives when starttime is less than current time
    if (
      !!foundSegmentSmaller &&
      this._segmentMap[foundSegmentSmaller].startTime <= currentTime &&
      this._segmentMap[foundSegmentSmaller].endTime >= currentTime
    ) {
      _activeSegment = this._segmentMap[foundSegmentSmaller];
    }

    this._setActiveSegment(_activeSegment);
  }

  // Description: Set segments active and inactive from within the video component (play, pause, seek)
  private _setActiveSegment(segment: IMediaSegment) {
    this._activeSegment = segment;
    this.onActiveSegmentChange.emit(segment);
    this._activeSegmentEndTime = !!segment ? segment.endTime : null;
  }

  // Description: Run end of segment checks and loop if needed
  private _endOfSegmentCallback(currentTime: number) {
    this.isEditMode && this.loopVideoSegment && !!this._activeSegment
      ? (this.video.currentTime = this._activeSegment.startTime / 1000)
      : this._setActiveSegment(null);
  }

  // Description: returns true for video playing false for ended/pause
  public isVideoPlaying() {
    return !!this.video && !!(this.video.currentTime > 0 && !this.video.paused && !this.video.ended && this.video.readyState > 2);
  }

  // Description: Navigate to new active segment
  public setActiveSegment(segment) {
    this.video.currentTime = segment.startTime / 1000;
  }
}
