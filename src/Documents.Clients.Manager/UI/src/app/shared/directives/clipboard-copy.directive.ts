import { Directive, OnInit, Inject, ElementRef, Input } from '@angular/core';
import { IframeResizerService } from '../index';

@Directive({
  selector: '[appClipboardCopy]'
})
export class ClipboardCopyDirective implements OnInit {
  private el: HTMLElement;
  private isIE11 = false;
  @Input('appClipboardCopy') containerId: string;

  constructor(ref: ElementRef, private iframeResizerService: IframeResizerService) {
    this.el = ref.nativeElement;
  }

  ngOnInit() {
    this.isIE11 = this.iframeResizerService.isIframeIE11;
    this.el.addEventListener('click', e => {
      const target = document.getElementById(this.containerId);
      if (!!target) {
        this.copyStringToClipboard(target.innerText);
      } else {
        console.error('Clipboard text target was not found');
      }
      e.preventDefault();
    });
  }

  copyStringToClipboard(string) {
    if (this.isIE11) {
      // works for IE11
      (window as any).clipboardData.setData('Text', string);
    } else {
      document.addEventListener('copy', handler, true);
      document.execCommand('copy');
    }

    function handler(event) {
      event.clipboardData.setData('text/plain', string);
      event.preventDefault();
      document.removeEventListener('copy', handler, true);
    }
  }
}
