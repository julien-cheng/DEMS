import { ActivatedRoute } from '@angular/router';
import { Component, OnInit, Input, Output, EventEmitter, Inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, FormGroup } from '@angular/forms';
import 'rxjs/add/operator/debounceTime';
import 'rxjs/add/operator/distinctUntilChanged';
import { IMediaSegment, IFileIdentifier, IBatchResponse, IMediaSet } from '..';
import { LoadingService, AppConfigService, FileService, JQ_TOKEN } from '../index';
import { MediaToolsService } from 'app/media/services/media-tools.service';

@Component({
  selector: 'app-media-search',
  templateUrl: './media-search.component.html',
  styleUrls: ['./media-search.component.scss']
})
export class MediaSearchComponent implements OnInit {
  constructor(
    private route: ActivatedRoute,
    public fileService: FileService,
    public mediaToolsService: MediaToolsService,
    @Inject(JQ_TOKEN) private $: any
  ) {
    this.initializeTypeAheadSearch();
    this.currentPage = 1;
    this.pageSize = 5;
  }
  private _segments: IMediaSegment[];
  @Output() onActiveSegmentChange = new EventEmitter();
  @Input() enableShareLink = false;
  @Input() canonicalURLFormat: string = null;
  @Input() segmentLabel = 'Transcript Segments';
  @Input() segments: IMediaSegment[];
  @Input() public activeSegment: IMediaSegment;
  @Input() public pageSize = 5;
  public searchResults: IMediaSegment[];
  public fileIdentifier: IFileIdentifier;
  public mediaSet: IMediaSet;
  public searchText = '';
  public searchTranscriptFormControl = new FormControl();
  public currentPage: number;
  public debounceTiming = 300; // A value in milliseconds to wait before executing a user search from a user typing.
  public searchFormGroup = new FormGroup({
    searchControl: this.searchTranscriptFormControl
  });

  // Share link button when enabled
  // --------------------------------------------------------------------------
  public shareLinkSegmentURL = '';
  public shareLinkSegment: IMediaSegment;

  ngOnInit() {
    this.searchResults = this.segments;
  }

  public initializeTypeAheadSearch() {
    this.searchTranscriptFormControl.valueChanges
      .debounceTime(this.debounceTiming) // debounce the changes.
      .distinctUntilChanged() // wait until it's actually settled.
      .subscribe(transcriptSearchString => {
        this.searchTranscripts(transcriptSearchString); // peform search.
      });
  }

  public clearSearch() {
    this.searchText = '';
  }

  public searchTranscripts(searchValue: string) {
    // If the user hasn't entered anything, or if the user
    // has input an empty string, then we 'reset' the search back to the entire list of segments.
    if (!searchValue || searchValue.length == 0) {
      this.searchResults = this.segments;
    } else {
      this.searchResults = [];
      // This is a really primitive search, but seems to work with everything I've tried so far.
      this.searchResults = this.segments.filter(segment => {
        if (segment.text.toLowerCase().includes(searchValue.toLowerCase())) {
          return segment;
        } else {
          return null;
        }
      });
    }
    // After we're done searching return to the first page.
    this.currentPage = 1;
  }

  // We need to take the event from the pager, and set the local current page, so our splice command in the markup
  // will work whenever the pager changes pages.
  public onPageChanged(currentPage: number) {
    this.currentPage = currentPage;
  }

  setActiveSegment(segment: IMediaSegment) {
    this.onActiveSegmentChange.emit(segment);
  }
  public openShareModal(event: Event, segment: IMediaSegment) {
    const destination = `/${this.route.snapshot.url.join('/')}?startTime=${segment.startTime}&endTime=${segment.endTime}`;

    this.shareLinkSegment = segment;

    this.shareLinkSegmentURL = !!this.canonicalURLFormat
      ? this.canonicalURLFormat.replace('__ENCODED_PATH__', encodeURIComponent(destination)).replace('__PATH__', destination)
      : `${window.location.origin}/${destination}`;

    this.$('#shareSegment')
      .modal()
      .on('hidden.bs.modal', e => {
        this.shareLinkSegment = null;
        this.shareLinkSegmentURL = '';
      });

    event.stopImmediatePropagation();
    return false;
  }
}
