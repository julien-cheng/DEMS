import { Component, OnInit, Input } from '@angular/core';
import { IMediaSegment } from '../index';

@Component({
  selector: 'app-media-subtitles',
  templateUrl: './media-subtitles.component.html',
  styleUrls: ['./media-subtitles.component.scss']
})
export class MediaSubtitlesComponent implements OnInit {
  @Input() activeSegment: IMediaSegment;
  constructor() { }

  ngOnInit() {
   
  }
}
