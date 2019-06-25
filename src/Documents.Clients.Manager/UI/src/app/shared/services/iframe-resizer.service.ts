import { Injectable } from '@angular/core';
// import { Router, NavigationEnd, ResolveEnd, NavigationStart } from '@angular/router';
import { Subject } from 'rxjs/Subject';
import * as parentIFrame from 'iframe-resizer';

export interface IParentIframe {
  clientHeight?: number;
  clientWidth?: number;
  iframeHeight?: number;
  iframeWidth?: number;
  offsetLeft?: number;
  offsetTop?: number;
  scrollLeft?: number;
  scrollTop?: number;
}

@Injectable()
export class IframeResizerService {

  isIframe: boolean;
  isIframeIE11: boolean = false;
  constructor() {
    this.isIframe = (window.self !== window.top); // this.isContentLoadedIframe();
  }

  adjustParentIframeHeight(height: number) {
    if ('parentIFrame' in window) {
      const parentIframe = (<any>window).parentIFrame;
      parentIframe.size(height);
    }
  }

  // Send message to Iframe
  sendParentMessage(msg: string) {
    if ('parentIFrame' in window) {
      const parentIframe = (<any>window).parentIFrame;
      parentIframe.sendMessage(msg);
    }
  }


}
