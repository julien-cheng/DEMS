import { Component, OnInit, Input, Output, ElementRef, EventEmitter } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { IFile, IFileIdentifier, IframeResizerService, IParentIframe, FileService, IDocumentSet, IBatchResponse } from '../../index';


@Component({
  selector: 'app-pdf',
  templateUrl: './pdf.component.html',
  styleUrls: ['./pdf.component.scss']
})

export class PdfComponent implements OnInit {
  @Input() fileSet: IDocumentSet;
  @Input() viewerType: string;
  public fileIdentifier: IFileIdentifier;
  public fileUrl: string;
  public saferesourceURL: SafeResourceUrl;
  public containerInnerHeight: number;
  // public documentSet:IDocumentSet;
  

  constructor(private sanitizer: DomSanitizer,
    private el: ElementRef,
    private fileService: FileService,
    private iframeResizerService: IframeResizerService) {
  }

  // Description: Initialize after the View has been init
  ngOnInit() {
      // console.log(this.viewerType, this.file, this.fileSet);
     this.fileIdentifier = this.fileSet.fileIdentifier;
     this.fileUrl = `/api/file/contents?fileidentifier.organizationKey=${this.fileIdentifier.organizationKey}&fileidentifier.folderKey=${this.fileIdentifier.folderKey}&fileidentifier.fileKey=${this.fileIdentifier.fileKey}&open=true`;
     this.saferesourceURL = this.sanitizer.bypassSecurityTrustResourceUrl('/assets/vendors/pdfjs-viewer/web/viewer.html?file=' + encodeURIComponent(this.fileUrl));
     setTimeout(() => {  this.calculatePDFIframeHeight(this.iframeResizerService.isIframe);  });
  }

  calculatePDFIframeHeight(isIframe: boolean) {
    let pdfIframeHeight = window.innerHeight;
    const _self = this;
    const windowHeight = window.innerHeight,
      mainWrapperHeight = $('#main-wrapper').height(),
      managerDetailHeight = $('#manager-detail').height(),
      pdfIframeDefaultHeight = $('.pdf-iframe').height();
    let pdfIframeOffset = $('.pdf-iframe').offset().top;
    if (isIframe) {
      if (!this.iframeResizerService.isIframeIE11) {
        pdfIframeOffset = ($('.main-content').height() - pdfIframeDefaultHeight) * 2 + pdfIframeOffset;
      }
      pdfIframeHeight = (mainWrapperHeight === windowHeight) ? (pdfIframeDefaultHeight * 2 - pdfIframeOffset) : managerDetailHeight;
    } else {
      pdfIframeHeight = (windowHeight - pdfIframeOffset) - 30; //+ pdfIframeDefaultHeight;
    }

    this.containerInnerHeight = pdfIframeHeight;
  }
}
