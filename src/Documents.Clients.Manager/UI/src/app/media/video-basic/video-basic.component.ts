import { Component, OnInit, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
// IMediaSource, IMediaSubtitles, IMediaSegment, EventType
import { IMediaSet, IMediaSource, IMediaSubtitles, IVideoProperties } from '../index';
import { MediaToolsService } from '../services/media-tools.service';

@Component({
  selector: 'app-video-basic',
  templateUrl: './video-basic.component.html',
  styleUrls: ['./video-basic.component.scss']
})
export class VideoBasicComponent implements OnInit {
  @Output() onVideoUpdate = new EventEmitter();
  @Output() updateVideoProperties = new EventEmitter<IVideoProperties>();
  @Input() mediaSet: IMediaSet;
  @Input() startTime:number=0;
  @ViewChild('player') player: ElementRef;
  // Overall Video props
  public sources: IMediaSource[];
  public subtitles: IMediaSubtitles[];
  public poster: string;
  public preload: 'auto' | 'none' | 'metadata';
  private video: HTMLVideoElement;
  // private _videoStep: number = 0;
  public videoProperties: IVideoProperties = {
    isPlaying: false,
    isMuted: false,
    isEnded: false,
    isClosedCaptionsOn: false,
    duration: 0
  };
  constructor(private mediaToolsService: MediaToolsService) { }

  ngOnInit() {
    !!this.mediaSet.poster && (this.poster = this.mediaToolsService.getFileContentURL(this.mediaSet.poster));
    this.preload = this.mediaSet.preload ? 'auto' : 'none';
    this.sources = this.mediaToolsService.buildMediaSources(this.mediaSet.sources);
    this.mediaSet.subtitles && (this.subtitles = this.mediaToolsService.buildMediaSubtitles(this.mediaSet.subtitles));
  }
  ngAfterViewInit() {
    let _self = this;
    this.video = this.player.nativeElement;
    // this.video.controls =false;
    // Bind Video Events
    this.video.addEventListener("loadedmetadata", () => { //console.log( 'loadedmetadata');
      this.setVideoProperties();
      this.updateVideoProperties.emit(this.videoProperties);
      // preload a start time:
      this.startTime>0 && this.setVideoCurrentTime(this.startTime);
    });
  
    this.video.addEventListener("timeupdate", (e) => {
      // let sec = Math.floor(this.video.currentTime);
      // if(sec !== this._videoStep){
      //   this._videoStep = sec;
      //   this.onVideoUpdate.emit(this.video.currentTime);
      // }
      this.onVideoUpdate.emit(this.video.currentTime);
    });
  }

  setVideoProperties() {
    (!!this.video.textTracks && this.video.textTracks.length >0 ) && (this.videoProperties.textTrackMode = this.video.textTracks[0].mode);
    this.videoProperties.isClosedCaptionsOn = (this.videoProperties.textTrackMode === "showing");
    this.videoProperties.isMuted = this.video.muted;
    this.videoProperties.duration = this.video.duration;
  }

  // Description: Setting the video current time from parent
  public setVideoCurrentTime(time:number){
    // console.log(time, this.videoProperties.duration);
    (!isNaN(time) && time<=this.videoProperties.duration) && (this.video.currentTime = time);
  }

  // Description: pause video from external components
  public pauseVideo(){
    this.video.pause();
  }
}
