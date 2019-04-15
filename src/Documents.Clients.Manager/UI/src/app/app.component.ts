import { Component, OnInit, ViewContainerRef } from '@angular/core';
import { ToastrService, Toast } from 'ngx-toastr';
import { IframeResizerService, AppConfigService, IAppConfiguration, LoadingService } from './shared/index';
// import * as parentIFrame from 'iframe-resizer';
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  isIframe: boolean;
  constructor(
    private toastr: ToastrService,
    private vRef: ViewContainerRef,
    private iframeResizerService: IframeResizerService,
    public appConfigService: AppConfigService,
    public loadingService: LoadingService
  ) {
    // this.toastr.setRootViewContainerRef(vRef);
    this.appConfigService.setAPIConfiguration();
  }

  ngOnInit() {
    // Document user Agent for IE styling
    document.documentElement.setAttribute('data-browser', navigator.userAgent);
    const msie = parseInt((/trident\/.*; rv:(\d+)/.exec(navigator.userAgent.toLowerCase()) || [])[1]); // parseInt((/msie (\d+)/.exec(navigator.userAgent.toLowerCase()) || [])[1]);
    // Set IE11 flags
    if (!isNaN(msie)) {
      document.documentElement.setAttribute('data-browser', 'MSIE-' + msie);
      this.iframeResizerService.isIframeIE11 = true;
    }
    // Detect if the interface is being loaded in iframe
    this.isIframe = this.iframeResizerService.isIframe; // this.inIframe();
  }
}
