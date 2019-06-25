import { Component, OnInit, Input, ViewChild, ElementRef, Inject, ViewChildren, QueryList, Renderer2 } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { JQ_TOKEN, ItemQueryType, ExplorerService, IRequestBatchData, GridViewModel, IFile, IframeResizerService } from '../../index';
import { ImageGalleryThumbnailComponent } from './image-gallery-thumbnail.component';
import { Subscription } from 'rxjs/Subscription';


@Component({
  selector: 'app-image-gallery',
  templateUrl: './image-gallery.component.html',
  styleUrls: ['./image-gallery.component.scss']
})
export class ImageGalleryComponent implements OnInit {
  @ViewChild(ImageGalleryThumbnailComponent) imageGalleryThumbnailComponent: ImageGalleryThumbnailComponent;
  @ViewChild('modalcontainer') containerEl: ElementRef;
  @ViewChild('carouselcontainer') carouselcontainer: ElementRef;
  @ViewChildren("imgSlide") imgs: QueryList<any>; // slideIndicator
  @Input() galleryItems: ItemQueryType[]; //Holds items that are either video or images
  imageLinkArr: Array<any>=[];
  isIframe: boolean;
  isIframeStyles: any;
  public loaded: boolean = false; // loads first image first
  public activeIndex: number = 0;
  
  constructor(
    private renderer: Renderer2,
    private sanitizer: DomSanitizer,
    private iframeResizerService: IframeResizerService,
    @Inject(JQ_TOKEN) private $: any
  ) { }

  ngOnInit() {
    this.isIframe = this.iframeResizerService.isIframe;
  }

  ngAfterViewInit() {
    this.bindEvents();
    setTimeout(()=>{
      if (this.isIframe) {
        const maxHeight = $('#main-wrapper').height();
        this.isIframeStyles = this.sanitizer.bypassSecurityTrustStyle(`max-height: ${maxHeight}px`);
      }
    });
    //Set in elements in easy to array
   // this.subscription = this.imgs.changes.subscribe((img) => { this.imageLinkArr = this.imgs.toArray();});
   this.imgs.forEach((img)=>{ this.imageLinkArr.push(img);});
  }

  private bindEvents() {
    const _self = this;
    this.$(`#galleryModal`).on('show.bs.modal', function (event) {
      _self.loaded = true;
      setTimeout(() => {
        _self.lazyLoadImages({ 'bottom': 0, 'top': (!!_self.imageGalleryThumbnailComponent ? _self.imageGalleryThumbnailComponent.visibleRangeIndexTop : (_self.galleryItems.length - 1)) });
      });
    });

    // Bind the slide event for lazy loading
    let carousel = this.carouselcontainer;
    !!carousel && this.$(this.carouselcontainer.nativeElement).on('slid.bs.carousel', function (e) {
      // Set active Index on slid  
      _self.activeIndex = $(e.relatedTarget).index();
      !!_self.imageGalleryThumbnailComponent && _self.imageGalleryThumbnailComponent.adjustThumbSliderOnSlid(_self.activeIndex);
    });
  }

  public lazyLoadImages(range: { bottom: number, top: number }) {
    if (!!this.imageLinkArr) {
      let image, src;
      const btm = range.bottom,
        top = ((range.top+1) <  this.galleryItems.length)? (range.top+1) : range.top; // Add one more to nice transition at end
      for (let i = btm; i <= top; i++) {
        image = this.imageLinkArr[i];
        src = !!image && image.nativeElement.dataset.src;
        if (!!src) {
          this.renderer.setStyle(image.nativeElement, 'background-image', `url(${src})`);
          this.renderer.removeAttribute(image.nativeElement, 'data-src');
        }
      }
    } else {
      console.error('Error: ImageLinkArray is undefined');
    }
  }

  // Description: trigger loading of next range images
  updateThumbSliderRange(range: { bottom: number, top: number }) {
    this.lazyLoadImages(range);
  }

  closeModal(event) {
    (event.target.tagName === 'A') &&
      this.$(this.containerEl.nativeElement).modal('hide');
  }

  // ngOnDestroy(): void {
  //   this.subscription.unsubscribe();
  // }
}
