import { Component, OnInit, Input } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'app-offline',
  templateUrl: './offline.component.html',
  styleUrls: ['./offline.component.scss']
})
export class OfflineComponent implements OnInit {
  @Input() viewerType: string;
  
  constructor(private sanitizer: DomSanitizer) { }

  ngOnInit() {
  }
}
