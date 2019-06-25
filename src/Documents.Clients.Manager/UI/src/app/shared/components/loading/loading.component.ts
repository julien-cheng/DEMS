import { Component, OnInit, Input } from '@angular/core';
import { IframeResizerService } from '../../services/iframe-resizer.service';

@Component({
  selector: 'app-loading',
  // templateUrl: './loading.component.html',
  template: `
    <div class="global-progress progress" *ngIf="loading">
      <div class="progress-bar progress-bar-striped progress-bar-animated bg-info" role="progressbar" aria-valuenow="75" aria-valuemin="0"
        aria-valuemax="100" style="width: 100%"></div>
    </div>
  `,
  styleUrls: ['./loading.component.scss']
})
export class LoadingComponent implements OnInit {
  @Input() loading: boolean;
  constructor(private iframeResizerService: IframeResizerService) { }

  ngOnInit() {
  }

  ngOnChanges() {
     let msg = this.loading ? 'isLoading' : 'loadingCompleted';
     this.iframeResizerService.sendParentMessage(msg);
  }
}
