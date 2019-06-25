import { Component, OnInit, SimpleChanges, Input, Output, EventEmitter, ViewChild, HostListener, ElementRef } from '@angular/core';
import { FormGroup, FormControl, Validators, FormBuilder, NgForm, NgModel } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { IMediaSegment, IMediaSet, MediaClipActionType, TimeFormatter, TooltipTimeFormatter, LoadingService, IVideoProperties, IFileIdentifier, IAllowedOperation, FormatTimePipe } from '../index';
import { MediaToolsService } from '../services/media-tools.service';
import { ToastsManager } from 'ng2-toastr';
import { PerfectScrollbarModule, PerfectScrollbarConfigInterface, PerfectScrollbarComponent, PerfectScrollbarDirective } from 'ngx-perfect-scrollbar';
import { NouisliderComponent } from 'ng2-nouislider';
import * as moment from 'moment';
import * as _ from 'lodash';
import { isContext } from 'vm';
const { isEqual } = _;

@Component({
  selector: 'app-media-timeline',
  templateUrl: './media-timeline.component.html',
  styleUrls: ['./media-timeline.component.scss']
})
export class MediaTimelineComponent implements OnInit {
  @Output() onActiveSegmentChange = new EventEmitter();
  @Output() onSegmentSave = new EventEmitter<IMediaSet>();
  @Output() processBatchUiAction = new EventEmitter();
  @Input() fileIdentifier: IFileIdentifier;
  @Input() activeSegment: IMediaSegment;
  @Input() segments: IMediaSegment[];
  @Input() videoDuration: number; // get information about the video
  @Input() allowedOperations: IAllowedOperation[];
  @ViewChild('segmentForm') segmentForm: NgForm;
  @ViewChild('muteslider') muteslider: NouisliderComponent;
  @ViewChild('mainSlider') mainSlider: NouisliderComponent;

  public isSaving: boolean = false;
  public clipForm: FormGroup;
  public muteForm: FormGroup;
  public eMediaClipActionType = MediaClipActionType;
  public activeAction: MediaClipActionType = MediaClipActionType.new; // TEMP -> change to new ***** WATCH *****
  public muteIsEditMode: boolean = false;
  public activeSegmentRange: IMediaSegment;
  public activeMuteSegmentRange: IMediaSegment;
  public videoIsLoaded = false;
  private _defaultNewModeTimes: IMediaSegment = { startTime: 0, endTime: 0 };
  public tempSegmentForCancel: { segment: IMediaSegment, index: number };
  public tempMuteSegmentForCancel: { segment: IMediaSegment, index: number };
  // Sliders config settings:
  private _sliderMinRangeValue = 1000; // in ms - This holds the min segment value for both sliders
  private _sliderMax: number = this.videoDuration; // This should come from video duration ***** TBD ***** ; // in ms - This holds the min segment value for both sliders

  // Main Slider:
  public pipValues = [0, 0];// ADD INITIAL VALUES and number of pips
  public timelineConfig: any = {};
  public mainSliderDisabled: boolean = false; // set the main slider inactive on mute segments 

  // Muting segments Slider:
  public mutePipValues = [0, 0];
  public muteConfig: any = {};
  public segmentDuration: number;

  // Video Slider:
  public videopipValue = 0;
  public videoConfig: any = {};

  // Dynamic attributes to slider and timeline depending on video duration
  public timelineWidth;
  private pipValuesArr: number[] | number;
  private pipDensity: number = 1;
  private pipMode: 'count' | 'values';

  // Perfect Scrollbar
  public config: PerfectScrollbarConfigInterface = {
    suppressScrollY: true,
    useBothWheelAxes: true
    // scrollXMarginOffset:380
  };
  @ViewChild(PerfectScrollbarDirective) directiveRef?: PerfectScrollbarDirective;
  public isScrollable: boolean = false;

  constructor(
    private sanitizer: DomSanitizer,
    private formBuilder: FormBuilder,
    private mediaToolsService: MediaToolsService,
    private formatTime: FormatTimePipe,
    public loadingService: LoadingService,
    public toastr: ToastsManager
  ) { }

  ngOnInit() {
    this._calculatetimelineWidth();
    this._setPipObject();
    this._setupConfigObjects();
  }

  ngOnChanges(simpleChanges: SimpleChanges) {
    if (!!simpleChanges.videoDuration) {// Set up _defaultNewModeTimes based on video duration
      this._defaultNewModeTimes = {
        startTime: 0,
        endTime: this.videoDuration,
        startTimeFormatted: this.formatTime.transform(0),
        endTimeFormatted: this.formatTime.transform(this.videoDuration),
        mutes: []
      }
    }

    if (!!simpleChanges.activeSegment) {
      this.setActiveSegment(this.activeSegment);
      this._adjustAllowedOperations(this.activeSegment);
    }
  }

  ngAfterViewInit() {
    // Set up scrollable
    setTimeout(() => { this.setupScrollable(); });
  }

  // Dsecription: Adjust timeline width for longer videos - This number needs to take into account the view port width
  private _calculatetimelineWidth() {
    const mainContainer = window.innerWidth - 380, //left nav (300) + gutters (30) + buttons col (50)
      videoDurationSec = this.videoDuration / 1000,
      pixelsPerSec = 5.3,
      minWidthNeeded = videoDurationSec * pixelsPerSec; // 300 for left nav + gutters
    if (minWidthNeeded > mainContainer) {
      this.timelineWidth = this.sanitizer.bypassSecurityTrustStyle(`width: ${minWidthNeeded}px`);
    }
  }

  // Dsecription: Set Pips depending on video duration
  private _setPipObject() {
    this.pipValuesArr = [];
    if (!isNaN(this.videoDuration)) {
      this.pipMode = 'values'
      for (var i = 0; i <= this.videoDuration; i++) {
        (i % 20000 === 0) && this.pipValuesArr.push(i);
      }

      // (this.videoDuration - this.pipValuesArr[this.pipValuesArr.length-1] < 10000) && this.pipValuesArr.splice(-1,1); //remove last one if diff less than 10000 (10sec)
      this.pipValuesArr.push(this.videoDuration);
      this.pipDensity = Math.round((200000 / this.videoDuration) * 100) / 100

    } else { // default to these:
      // mode: 'count',        // values: 10,
      this.pipMode = 'count'
      this.pipValuesArr = 10;
      this.pipDensity = 1;
    }


  }

  // Dsecription: set up the  slider configuration objects
  private _setupConfigObjects() {
    this._sliderMax = this.videoDuration;
    this.timelineConfig = {
      connect: true,
      behaviour: 'tap',
      start: 0,
      keyboard: true,
      margin: this._sliderMinRangeValue,
      range: {
        'min': [0],
        'max': [this._sliderMax] // ms to get correct resolution
      },
      tooltips: [new TooltipTimeFormatter(), new TooltipTimeFormatter()],
      pips: {
        mode: this.pipMode,
        values: this.pipValuesArr,
        density: this.pipDensity,
        format: new TimeFormatter()
      }
    };

    this.videoConfig = {
      connect: [true, false],
      range: {
        'min': [0],
        'max': [this._sliderMax] // ms to get correct resolution
      },
      tooltips: new TooltipTimeFormatter()
    }


    this.muteConfig = {
      behaviour: 'tap-drag',
      start: 0,
      connect: true,
      keyboard: true,
      margin: this._sliderMinRangeValue,
      range: {
        'min': [0],
        'max': [this._sliderMax]
      },
      tooltips: [new TooltipTimeFormatter(), new TooltipTimeFormatter()],
      padding: [0, 1000]
    }
    this.videoIsLoaded = true;
  }

  // Description: Disable ExportClipRequest
  private _adjustAllowedOperations(segment: IMediaSegment) {
    this.allowedOperations.forEach((ao: IAllowedOperation) => { (ao.batchOperation.type === 'ExportClipRequest') && (ao.isDisabled = !(!!segment)); });
  }

  // Description: Sets the active pips range on active segment change
  private _setPipValues(activeSegment: IMediaSegment) {
    this.pipValues = (!!activeSegment) ? [(activeSegment.startTime || 0), (activeSegment.endTime || 0)] : [this._defaultNewModeTimes.startTime, this._defaultNewModeTimes.endTime];
  }

  // Description: Set activeSegmentRange
  private updateActiveSegmentRange(activeSegment: IMediaSegment) {
    // console.log('updateActiveSegmentRange ', activeSegment, this.activeSegmentRange);
    if (!!activeSegment) {
      this.activeSegmentRange = activeSegment;
      this.activeSegmentRange.startTimeFormatted = this.formatTime.transform(activeSegment.startTime); // || '00:00'
      this.activeSegmentRange.endTimeFormatted = this.formatTime.transform(activeSegment.endTime); // || '00:00'
    } else {
      this.activeSegmentRange = Object.assign({}, this._defaultNewModeTimes);
    }
  }

  // Description: update active segment range for the slider
  private updateActiveSegmentRangeTime(startTime: number, endTime: number) {
    //  console.log('updateActiveSegmentRangeTime: ',startTime);
    this.activeSegmentRange.startTime = startTime;
    this.activeSegmentRange.endTime = endTime;
    this.activeSegmentRange.startTimeFormatted = this.formatTime.transform(startTime);
    this.activeSegmentRange.endTimeFormatted = this.formatTime.transform(endTime);
  }

  // Description: Set active segment from external components: search, video through parent (clip) - set mediaClipAction to edit if clip
  public setActiveSegment(activeSegment: IMediaSegment) {
    // console.log('setActiveSegment: ', activeSegment);
    this._resetCurrentMuteSegment();
    if (activeSegment === undefined || !_.isEqual(this.activeSegmentRange, activeSegment)) {
      this.updateActiveSegmentRange(activeSegment);
      this._setPipValues(activeSegment);
      this._resetCurrentSegment();

      // Prepare the temp segment
      this.tempSegmentForCancel = {
        segment: Object.assign({}, this.activeSegment, { isActive: false }),
        index: (!!activeSegment ? this.mediaToolsService.getSegmentIndex(this.segments, this.activeSegment) : -1)
      };

      // need this to update paddings on muted segments:
      setTimeout(() => { this._setupMuteComponent(); });
    }


    // Reset to edit to work on active segments
    // (!!this.activeSegment && (this.activeAction === this.eMediaClipActionType.new)) && (this.activeAction = this.eMediaClipActionType.edit);
    (!!this.activeSegment && (this.activeAction === this.eMediaClipActionType.new)) && this.selectActiveAction(this.eMediaClipActionType.edit);
  }

  // Description: change the startTime and endTime model
  public updateTime(event: Event, control: FormControl, name: string, isMute: boolean = false) {
    const value = control.value,
      ms = this.formatTime.transformReversed(value); // console.log('updateTime: ', name,value, ms);
    if (control.valid && !isNaN(ms)) {
      if (isMute) {
        this.activeMuteSegmentRange[name] = ms;
        (!!this.activeMuteSegmentRange) && (this.mutePipValues = [this.activeMuteSegmentRange.startTime, this.activeMuteSegmentRange.endTime]);
      } else {
        this.activeSegmentRange[name] = ms;
        this._setPipValues(this.activeSegmentRange);
      }
    }
  }

  // Description: Set startTime or EndTime to Current Video time 
  public setToCurrentTime(event: Event, model: NgModel, name: string, isMute: boolean = false) {
    let control = model.control,
      setTimeTo = this.videopipValue,
      limit = this._sliderMinRangeValue;

    if (name === 'startTime') {
      limit = isMute ? (this.activeMuteSegmentRange.endTime - limit) : (this.activeSegmentRange.endTime - limit);
      (setTimeTo > limit) && (setTimeTo = limit)
    } else if (name === 'endTime') {
      limit = isMute ? (this.activeMuteSegmentRange.startTime + limit) : (this.activeSegmentRange.startTime + limit);
      (setTimeTo < limit) && (setTimeTo = limit)
    }

    control.patchValue(this.formatTime.transform(setTimeTo));
    this.updateTime(event, control, name, isMute);
  }


  // Description: Is set time button disabled 
  public isAppendBtnDisabled(name: 'startTime' | 'endTime', isMute: boolean = false): boolean {
    let isDisabled = false;
    if (isMute) {
      isDisabled = (this.videopipValue < this.activeSegmentRange.startTime || this.videopipValue > this.activeSegmentRange.endTime) ? true :
        (name === 'startTime') ? (this.videopipValue > this.activeMuteSegmentRange.endTime) : (name === 'endTime') ? (this.videopipValue < this.activeMuteSegmentRange.startTime) : false;
    } else {
      isDisabled = (name === 'startTime') ? (this.videopipValue > this.activeSegmentRange.endTime) :
        (name === 'endTime') ? (this.videopipValue < this.activeSegmentRange.startTime) : false;
    }
    return isDisabled
  }

  // Description: Set currently focused handle to the pip target of click
  // private _focusedHandle: HTMLElement;
  // public capturePipClick(e) {
  //   let el = <HTMLElement>e.target;    // console.log(el, el.classList.contains('noUi-handle'));
  //   if (el.classList.contains('noUi-handle')) {
  //     this._focusedHandle = el;
  //   } else if (el.classList.contains('noUi-value')) {
  //     if (!!this._focusedHandle) {
  //       let timeVal: number = Number(el.getAttribute('data-value'));
  //       if (this._focusedHandle.classList.contains('noUi-handle-lower')) { //This is startTime
  //         (timeVal >= this.activeSegmentRange.endTime - this._sliderMinRangeValue) && (timeVal = this.activeSegmentRange.endTime - this._sliderMinRangeValue);
  //         this.updateActiveSegmentRangeTime(timeVal, this.activeSegmentRange.endTime);
  //       } else if (this._focusedHandle.classList.contains('noUi-handle-upper')) { //This is endTime
  //         (timeVal <=  this.activeSegmentRange.startTime + this._sliderMinRangeValue) && (timeVal = this.activeSegmentRange.startTime + this._sliderMinRangeValue);
  //         this.updateActiveSegmentRangeTime(this.activeSegmentRange.startTime, timeVal);// this._sliderMinRangeValue;
  //       }
  //       this._setPipValues(this.activeSegmentRange);
  //       this._focusedHandle = undefined;
  //     }
  //   } else {
  //     this._focusedHandle = undefined;
  //   }
  // }

  // Clipping actions: clip, edit and mute
  //----------------------------------------------
  // Description: select active action.
  public selectActiveAction(actionType: MediaClipActionType) {
    // If action is new => Reset current active segment to undefined for new segment creation
    if (actionType === this.eMediaClipActionType.new) {
      !!this.segmentForm && this.segmentForm.reset();
      this.onActiveSegmentChange.emit();
    }

    // If action is mute => set up mute component before change
    (this.activeAction === this.eMediaClipActionType.mute) && this._resetMuteComponent();

    // Set activeAction type flag - change
    this.activeAction = (this.activeAction === this.eMediaClipActionType.mute && actionType === this.activeAction) ? this.eMediaClipActionType.edit : actionType;
    (this.activeAction === this.eMediaClipActionType.mute) && setTimeout(() => {
      this._setupMuteComponent();
    });
  }

  // Description: Form submit for new segment
  public onSubmitSaveSegment(segmentForm: NgForm) {
    let segment = segmentForm.value;
    if (this.activeAction === this.eMediaClipActionType.new) {
      segment.isActive = true;
      this.mediaToolsService.addNewSegmentByStartTime(this.segments, segment);// this.segments.push(segment);
    }


    this.tempSegmentForCancel.segment = Object.assign({}, segment); // Save current working segment as temp
    this._saveSegments(segment);
  }

  // Descriptions: save current segments
  private _saveSegments(segment?: IMediaSegment) {
    this.isSaving = true;
    this.loadingService.setLoading(true);
    this.mediaToolsService.saveMediaClipSets({
      rootFileIdentifier: this.fileIdentifier,
      segments: this.segments
    }).subscribe(
      (response) => {
        this.onSegmentSave.emit(response.response);
        // If new segment mode
        (this.activeAction === this.eMediaClipActionType.new) && this.onActiveSegmentChange.emit(segment);  // Set new segment active
        this.toastr.success('The segment was saved successfully');
      },
      (error) => {
        this.toastr.error('There was a problem with your request');
        this.selectActiveAction(this.eMediaClipActionType.new);
        console.error(error);
      },
      () => {  //Complete
        this.isSaving = false;
        this.loadingService.setLoading(false);
      }
    );
  }

  // Description: Reset Segment Form - cancel button
  public resetForm(segmentForm: NgForm) {
    (this.activeAction !== this.eMediaClipActionType.new) ? this.selectActiveAction(this.eMediaClipActionType.new) : this.setActiveSegment(null);
  }

  // Description: Reset segments to before changes - Update the segments object with the this.tempSegmentForCancel values
  private _resetCurrentSegment() {
    // (!!this.tempSegmentForCancel && this.tempSegmentForCancel.index >= 0) && console.log(' ***** CHANGED BY TEMP  ***** ');
    (!!this.tempSegmentForCancel && this.tempSegmentForCancel.index >= 0) &&
      (this.segments[this.tempSegmentForCancel.index] = this.tempSegmentForCancel.segment);
  }

  // Description: delete segment from model
  public deleteSegment() {
    this.segments.splice(this.mediaToolsService.getSegmentIndex(this.segments, this.activeSegment), 1);
    this.tempSegmentForCancel = undefined;
    this._saveSegments();
    this.selectActiveAction(this.eMediaClipActionType.new);
  }

  // MUTED SEGMENTS
  // ----------------------------------------------------------------------------
  // Description: set up mute component and slider
  private _setupMuteComponent() {
    // console.log('_setupMuteComponent', this.activeSegmentRange, this._sliderMax);
    if (!!this.activeSegmentRange && !!this.muteslider) { //!!this.activeSegmentRange.startTime
      this.mainSliderDisabled = true;
      let start = this.activeSegmentRange.startTime, // Limits the bottom value/handle based on the parent activeSegmentRange
        topPadding = Number(this._sliderMax - this.activeSegmentRange.endTime); // Limits the top value of top handle based on the parent activeSegmentRange
      this._defaultMuteComponentRange();
      this.muteslider.slider.updateOptions({
        padding: [start, topPadding]
      }, false);
    };
  }

  // Description: sets the muted slider to default
  private _defaultMuteComponentRange() {
    //  console.log('_defaultMuteComponentRange', this.activeSegmentRange,  this.activeMuteSegmentRange);
    !!this.activeSegmentRange.mutes && this.mediaToolsService.resetActiveSegment(this.activeSegmentRange.mutes);
    let start = this.activeSegmentRange.startTime, // Limits the bottom value/handle based on the parent activeSegmentRange
      end = (start + this._sliderMinRangeValue + 3000);
    this.setActiveMuteSegmentTime(start, end);
    (end > this.activeSegmentRange.endTime) && (end = this.activeSegmentRange.endTime);
    this.mutePipValues = [start, end];
    this.muteIsEditMode = false;
    this.segmentDuration = this.activeSegmentRange.endTime;
    this.tempMuteSegmentForCancel = undefined;
  }
  // Description: reset the mute component and slider
  private _resetMuteComponent() {
    this.mainSliderDisabled = false;
  }

  // Description: Activate a saved muted segment
  public setActivateMuteSegment(segment: IMediaSegment) {
    // console.log('|***** setActivateMuteSegment: ', segment);  console.log('tempMuteSegmentForCancel: ', this.tempMuteSegmentForCancel); 
    if (!!segment) {
      this._resetCurrentMuteSegment();
      this.muteIsEditMode = true;
      this.mediaToolsService.resetActiveSegment(this.activeSegmentRange.mutes);
      this.tempMuteSegmentForCancel = {
        segment: Object.assign({}, segment),
        index: this.mediaToolsService.getSegmentIndex(this.activeSegment.mutes, segment)
      };
      segment.isActive = true;
      this.activeMuteSegmentRange = segment;
      this.activeMuteSegmentRange.startTimeFormatted = this.formatTime.transform(segment.startTime) || '00:00:00';
      this.activeMuteSegmentRange.endTimeFormatted = this.formatTime.transform(segment.endTime) || '00:00:00';
      this.mutePipValues = [segment.startTime, segment.endTime];
    }
  }
  // Description: Reset segments to before changes - Update the segments object with the this.tempMuteSegmentForCancel values
  private _resetCurrentMuteSegment() {
    if (!!this.activeSegmentRange) {
      !Array.isArray(this.activeSegmentRange.mutes) && (this.activeSegmentRange.mutes = []); //for nulls 
      (!!this.tempMuteSegmentForCancel && this.tempMuteSegmentForCancel.index >= 0) &&
        (this.activeSegmentRange.mutes[this.tempMuteSegmentForCancel.index] = this.tempMuteSegmentForCancel.segment);
    }
  }

  // Description: builds the activeMuteSegmentRange  
  private setActiveMuteSegmentTime(startTime: number, endTime: number) {
    // console.log('|***** setActiveMuteSegmentTime: ', startTime, endTime);
    this.activeMuteSegmentRange = {
      startTime: startTime,
      endTime: endTime,
      startTimeFormatted: this.formatTime.transform(startTime) || '00:00:00',
      endTimeFormatted: this.formatTime.transform(endTime) || '00:00:00'
    }
  }

  // Description: validates the range of the mute segments
  public mutedSegmentInvalid(segment: IMediaSegment): boolean {
    const isStartTimeInvalid = segment.startTime < this.activeSegmentRange.startTime,
      isEndTimeInvalid = segment.endTime > this.activeSegmentRange.endTime;
    return (isStartTimeInvalid || isEndTimeInvalid);
  }

  // Description: reset active mute range and default all settings
  public newMuteSegment() {
    this._defaultMuteComponentRange();
    this.mediaToolsService.resetActiveSegment(this.activeSegmentRange.mutes);
  }

  // Description: Update the activeMuteSegmentRange for muting segments
  public onMuteSliderChange(value) {
    const startTime = Math.round(value[0]),
      endTime = Math.round(value[1]);
    this.activeMuteSegmentRange.startTime = startTime;
    this.activeMuteSegmentRange.endTime = endTime;
    this.activeMuteSegmentRange.startTimeFormatted = this.formatTime.transform(startTime);
    this.activeMuteSegmentRange.endTimeFormatted = this.formatTime.transform(endTime);
  }

  // Description: Delete a muted segment
  public deleteMutedSegment(event: Event, segment: IMediaSegment) {
    const segmentIndex = this.mediaToolsService.getSegmentIndex(this.activeSegmentRange.mutes, segment);
    (!isNaN(segmentIndex) && segmentIndex >= 0) && this.activeSegmentRange.mutes.splice(segmentIndex, 1);
    this._saveSegments();
    segment.isActive && this._defaultMuteComponentRange(); // Reset slider if deleting active segment
    event.stopImmediatePropagation();   // Needed to prevent trigger of activateMuteSegment
    return false;
  }

  // Description: Form submit for new mute segment
  public onSubmitSaveMuteSegment(muteForm: NgForm) {
    let newSegment: IMediaSegment = muteForm.value;
    if (!this.muteIsEditMode) { // New mode
      !Array.isArray(this.activeSegment.mutes) && (this.activeSegment.mutes = []); // For cases where mutes =null
      this.mediaToolsService.addNewSegmentByStartTime(this.activeSegment.mutes, newSegment);
      this.setActivateMuteSegment(newSegment);
    }

    this.tempMuteSegmentForCancel = undefined;
    this._saveSegments();
  }

  // Description: Reset Mute Form 
  public resetMuteForm(muteForm: NgForm) {
    this._resetCurrentMuteSegment();
    this.muteIsEditMode = false;
    this.selectActiveAction(this.eMediaClipActionType.edit);
  }

  // Video Slider
  // ----------------------------------------------------------------------------
  // Description: Update the current value of the video slider
  public updateVideoCurrentTime(currentTime: number) {
    this.videopipValue = currentTime;
  }

  // Horizontal Scrollbar Slider
  // ----------------------------------------------------------------------------
  // Description: capture all events and the change event
  // https://angularcomponents.blogspot.com/2017/10/ngx-perfect-scrollbar.html
  // https://github.com/zefoy/ngx-perfect-scrollbar#readme
  // Description: find the active range on the main slider and bring it into view if not there
  public onSliderChange(value) {
    const startTime = Math.round(value[0]),
      endTime = Math.round(value[1]);   //console.log(value, eventType, startTime, endTime); 
    this.updateActiveSegmentRangeTime(startTime, endTime);
  }

  // Description: Initialize Scrollable 
  //----------------------------------------------
  @ViewChild('noUIContainer') elementView: ElementRef;
  @ViewChild('timelineContainer', {read: ElementRef}) private timelineContainer: ElementRef;
  //timelineContainer
  // private mouse: MouseEvent;
  private defaultScrollableOffset: number = 0;

  // Description: set up scrollable defaults
  private setupScrollable() {
    this.isScrollable = this.showScrollButtons();
    this.defaultScrollableOffset = this.elementView.nativeElement.getBoundingClientRect().left;
  }

  scrollContainerToLeft(offset?: number) {
    let width = this.timelineContainer.nativeElement.getBoundingClientRect().width - 50,
     newOffset = !!offset ? offset : this.elementView.nativeElement.scrollLeft + width*0.8; //((window.innerWidth - 410) * 0.8);
    this.directiveRef.scrollToLeft(newOffset, 100);
  }

  scrollContainerToRight(offset?: number) {
    let width = this.timelineContainer.nativeElement.getBoundingClientRect().width - 50,
     newOffset = !!offset ? offset : this.elementView.nativeElement.scrollLeft - width*0.8; //- ((window.innerWidth - 410) * 0.8);
    this.directiveRef.scrollToLeft(newOffset, 100);
  }

  // Description: Scroll to specific offset from right or left
  scrollToView(isNext: boolean = true, offset: number = 0) {
    isNext ? this.directiveRef.scrollToRight(offset, 100) : this.directiveRef.scrollToLeft(offset, 100);
  }

  showScrollButtons(): boolean {
    const scrollGeom = this.directiveRef.geometry(),
      offsetGeom = this.directiveRef.geometry('offset');
    // console.log('showScrollButtons', offsetGeom);
    return scrollGeom.w > offsetGeom.w;
  }
}

