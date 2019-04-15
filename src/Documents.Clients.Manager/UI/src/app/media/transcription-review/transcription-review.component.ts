import { Component, OnInit, Input, ViewChild, Inject, Output, EventEmitter, SimpleChanges, OnChanges } from '@angular/core';
// import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { IMediaSegment, IFileIdentifier, FileService, IBatchResponse, IMediaSet, LoadingService, JQ_TOKEN } from '../index';
import { TimePickerComponent } from '../time-picker/time-picker.component';
import { MediaToolsService } from '../services/media-tools.service';

@Component({
  selector: 'app-transcription-review',
  templateUrl: './transcription-review.component.html',
  styleUrls: ['./transcription-review.component.scss']
})
export class TranscriptionReviewComponent implements OnChanges {
  @Input() activeSegment: IMediaSegment;
  @Input() segments: IMediaSegment[];
  @Input() fileIdentifier: IFileIdentifier;
  @Output() onActiveSegmentChange = new EventEmitter<IMediaSegment>();
  @Output() onSegmentSave = new EventEmitter<IMediaSet>();

  public isEditing = false;
  public isNew = false;
  public currentEditSegment: IMediaSegment;
  public currentEditIndex: number;
  public tempSegmentForCancel: IMediaSegment;
  public scrolledLocation: number;

  constructor(
    public loadingService: LoadingService,
    public mediaToolsService: MediaToolsService,
    public toastr: ToastrService,
    @Inject(JQ_TOKEN) private $: any
  ) {}
  @ViewChild('startTime') timePickerStart: TimePickerComponent;
  @ViewChild('endTime') timePickerEnd: TimePickerComponent;

  ngOnChanges(simpleChanges: SimpleChanges) {
    if (!!simpleChanges.activeSegment && !simpleChanges.activeSegment.firstChange && !!this.activeSegment) {
      this.scrollTopForActiveSegment();
    }
  }

  // Description: Trigger change to the active segment on click
  public setActiveSegment(segment: IMediaSegment) {
    this.onActiveSegmentChange.emit(segment);
  }

  // Description: scroll to the top of the current active segment
  private scrollTopForActiveSegment() {
    // console.log(this.isEditing, this.scrolledLocation, this.activeSegment.startTime, document.getElementById(`${this.activeSegment.startTime}`));
    if (
      !this.isEditing &&
      this.scrolledLocation !== this.activeSegment.startTime &&
      document.getElementById(`${this.activeSegment.startTime}`)
    ) {
      const offsetTop = document.getElementById(`${this.activeSegment.startTime}`).offsetTop;
      const elementHeight = document.getElementById(`${this.activeSegment.startTime}`).clientHeight;
      const scrollTo = offsetTop - elementHeight - 40;
      $(`#segmentReview`).animate({ scrollTop: scrollTo }, `fast`);
      this.scrolledLocation = this.activeSegment.startTime;
    }
  }

  // Description: Transcription Form Section
  // ----------------------------------------
  public getMaxTime() {
    if (this.currentEditSegment) {
      if (this.currentEditIndex === this.segments.length - 1) {
        return 24 * 60 * 60 * 999; // 24 hours in milliseconds.
      }
      // we're rounding up to the next whole second. 1234 /1000 = 1.234 --> floor ==> 1.0 * 1000 ==> 1000 + 1000 = 2000
      return this.segments[this.currentEditIndex + 1].startTime;
    }
  }

  public getMinTime() {
    if (this.currentEditSegment) {
      if (this.currentEditIndex === 0 && this.currentEditSegment.startTime > 0) {
        return 0;
      } else {
        // we're rounding up to the next whole second. 1234 /1000 = 1.234 --> floor ==> 1.0 * 1000 ==> 1000 + 1000 = 2000
        return this.segments[this.currentEditIndex - 1].endTime;
      }
    }
  }

  public addBeforeAllowed(segment: IMediaSegment): boolean {
    if (this.isEditing) {
      return false;
    }

    const index = this.getSegmentIndex(segment);
    // if we're on the very first segment, we need to make sure its start time isn't 0, or 1 second.
    if (index === 0) {
      if (segment.startTime >= 3000) {
        return true;
      } else {
        return false;
      }
    } else {
      // here we're going to make sure there's actually a gap that we can add a segment into.
      const segmentBefore = this.segments[index - 1];
      if (segment.startTime - segmentBefore.endTime > 3000) {
        return true;
      } else {
        return false;
      }
    }
  }

  public addAfterAllowed(segment: IMediaSegment) {
    if (this.isEditing) {
      return false;
    }

    const index = this.getSegmentIndex(segment);
    // here we're going to make sure there's actually a gap that we can add a segment into.
    // check to see if this is the last segment.  if so we're always going to allow them to add a segment after.
    if (index == this.segments.length - 1) {
      return true;
    } else {
      const segmentAfter = this.segments[index + 1];
      if (segmentAfter.startTime - segment.endTime > 3000) {
        return true;
      } else {
        return false;
      }
    }
    // Notice there's no validation for creating a segment past the end of the video.
  }

  public addSegmentBefore(segment: IMediaSegment) {
    const index = this.getSegmentIndex(segment);

    this.currentEditSegment = {
      startTime: index === 0 ? 0 : this.segments[index - 1].endTime + 1000,
      endTime: segment.startTime - 1000,
      text: ''
    };
    this.segments.splice(index, 0, this.currentEditSegment);
    this.currentEditIndex = index;
    this.isEditing = true;
    this.isNew = true;
  }

  public addSegmentAfter(segment: IMediaSegment) {
    const index = this.getSegmentIndex(segment);

    this.currentEditSegment = {
      startTime: segment.endTime + 1000,
      endTime: index === this.segments.length - 1 ? segment.endTime + 2000 : this.segments[index + 1].startTime - 1000,
      text: ''
    };

    this.segments.splice(index + 1, 0, this.currentEditSegment);
    this.currentEditIndex = index + 1;
    this.isEditing = true;
    this.isNew = true;
  }

  public saveSegment() {
    if (this.timePickerStart.isTimeControlValid() && this.timePickerEnd.isTimeControlValid()) {
      this.putMediaSegments(true);
    }
  }

  public putMediaSegments(turnEditModeOff: boolean) {
    this.loadingService.setLoading(true);
    this.mediaToolsService
      .saveMediaSegments({
        rootFileIdentifier: this.fileIdentifier,
        segments: this.segments
      })
      .subscribe(
        (response: IBatchResponse) => {
          const mediaSet = response.response as IMediaSet;
          this.onSegmentSave.emit(mediaSet);
          this.toastr.success('The segment was saved successfully');
        },
        error => {
          this.toastr.error(`There was an error saving the segment`);
          console.error(error);
          throw error;
        },
        () => {
          this.loadingService.setLoading(false);
          if (turnEditModeOff) {
            this.isEditing = false;
            this.currentEditSegment = null;
            this.isNew = false;
          }
        }
      );

    // The save will process async.  So we won't wait for save to complete first.
    // if (turnEditModeOff) {
    //   this.isEditing = false;
    //   this.currentEditSegment = null;
    //   this.isNew = false;
    // }
  }

  public delete(segmentToDelete: IMediaSegment) {
    this.segments.splice(this.getSegmentIndex(segmentToDelete), 1);
    this.putMediaSegments(false);
  }

  public edit(segmentToEdit: IMediaSegment) {
    this.currentEditIndex = this.getSegmentIndex(segmentToEdit);

    // We save out a copy of that segment so we can handle cancel, and overwrite the changes with the original values.
    this.tempSegmentForCancel = {
      startTime: segmentToEdit.startTime,
      endTime: segmentToEdit.endTime,
      text: segmentToEdit.text
    };

    this.currentEditSegment = segmentToEdit;
    this.isEditing = true;
    this.isNew = false;
    // this.onActiveSegmentChange.emit(this.currentEditSegment); // Don't change the active segment - UI decision
  }

  public getSegmentIndex(segment: IMediaSegment): number {
    return this.segments.findIndex(searchSegment => {
      return segment.startTime == searchSegment.startTime;
    });
  }

  setStartTime(time: number) {
    this.currentEditSegment.startTime = time;
  }

  setEndTime(time: number) {
    this.currentEditSegment.endTime = time;
  }

  public cancel() {
    if (this.isNew) {
      this.segments.splice(this.currentEditIndex, 1);
    } else {
      // We're going to overwrite in case of cancel any changes that the user made.
      this.segments[this.currentEditIndex] = this.tempSegmentForCancel;
      // now we can blow this guy away.
      this.tempSegmentForCancel = null;
    }

    this.isEditing = false;
    this.isNew = false;
    this.currentEditSegment = null;
  }
}
