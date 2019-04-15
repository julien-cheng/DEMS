import {
  Component,
  OnInit,
  AfterContentInit,
  Input,
  Output,
  HostListener,
  NgZone,
  EventEmitter,
  ElementRef,
  ViewChildren,
  QueryList,
  Renderer2,
  AfterViewInit
} from '@angular/core';
import { DomSanitizer, SafeStyle, SafeResourceUrl } from '@angular/platform-browser';
import { Subscription } from 'rxjs/Subscription';
import { ItemQueryType } from '../../index';

@Component({
  selector: 'app-image-thumbnail',
  template: `
    <div class="carousel-indicators-wrapper" [ngClass]="{ 'indicators-centered': !isScrollable }">
      <ol class="carousel-indicators" [style.width]="getSafeStyle(indicatorWidth + 'px')" [style.left]="thumbnailsLeft">
        <li
          *ngFor="let item of galleryItems; let i = index"
          [ngClass]="{ active: i === 0 }"
          [attr.data-src]="item.safeThumbUrl"
          [attr.data-slide-to]="i"
          data-target="#gallery"
          [style.width]="getSafeStyle(_thumbWidth + 'px')"
          #thumbSlide
        ></li>
      </ol>
    </div>
    <ng-container *ngIf="isScrollable">
      <button class="arrow thumb-control-prev" [disabled]="!isPrevActive" (click)="moveSlides('prev')">
        <i class="fa fa-angle-left" aria-hidden="true"></i> <span class="sr-only">Previous</span>
      </button>
      <button class="arrow thumb-control-next" [disabled]="!isNextActive" (click)="moveSlides('next')">
        <i class="fa fa-angle-right" aria-hidden="true"></i> <span class="sr-only">Next</span>
      </button>
    </ng-container>
  `,
  styleUrls: ['./image-gallery-thumbnail.component.scss']
})
export class ImageGalleryThumbnailComponent implements OnInit, AfterViewInit {
  @Output() updateThumbSliderRange = new EventEmitter();
  @Input() galleryItems: ItemQueryType[];
  @ViewChildren('thumbSlide') thumbs: QueryList<any>; // @ViewChildren("thumbSlide") thumbs: QueryList<any>; // slideIndicator
  imageLiArr: Array<any> = [];
  thumbWidth = 120; // This is the default width - will adjust to windows width
  thumbMarginHorizontal = 6; // Includes the margin 3px + 120px  // thumbMarginHorizontal: number = 6;
  private _activeSlideIndex = 0; // The current active Gallery item active
  private _outerThumbWidth: number; // Includes the margin
  private _visibleCount: number; // Number of thumbnails visible in the slider
  _thumbWidth: number; // the width of each thumbnail images around 120-140 - adaptive to viewport
  isPrevActive = false; // is the prev arrow active
  isNextActive = true; // is the next arrow active
  isScrollable = true; // The number of images and total width is greater than the viewport = is scrollable
  count: number; // Total number of gallery items
  indicatorWidth: number; // Total horizontal width of the thumbnail slider
  thumbLeft: number; // Left position in px, number only, of the thumbnail slider 0 (default)
  thumbnailsLeft: string; // Left position in px string style
  public visibleRangeIndexBottom = 0; // The thumb that is in the first position
  public visibleRangeIndexTop: number; // The thumb that is in the first position

  constructor(private renderer: Renderer2, private sanitization: DomSanitizer, private zone: NgZone) {}

  ngOnInit() {
    this.count = this.galleryItems.length;
    this.setIndicatorsWidth();
    this.resetSliderDefaultPosition();
  }
  ngAfterViewInit() {
    this.thumbs.forEach(li => {
      this.imageLiArr.push(li);
    }); // Build the array(
    this.lazyLoadImages();
  }

  // Description: Reset slider
  resetSliderDefaultPosition(): void {
    this.thumbLeft = 0;
    this._setTopRange();
    this.setThumbnailsPosition();
  }

  // Sets the Width of the indicator and adjusts the thumbs so that it fills the screen width
  setIndicatorsWidth() {
    // Make it as big as the window innerwidth unless the width is smaller: // width = (window.innerWidth < width) ? window.innerWidth : width;
    const viewport = window.innerWidth;
    this._visibleCount = Math.floor(viewport / (this.thumbWidth + this.thumbMarginHorizontal));
    this._thumbWidth = Math.floor(viewport / this._visibleCount) - this.thumbMarginHorizontal;
    this._outerThumbWidth = this._thumbWidth + this.thumbMarginHorizontal;
    this.indicatorWidth = !isNaN(this.count) && Number(this.count * this._outerThumbWidth); // Number(this.count * this.thumbWidth);
    this.isScrollable = viewport < this.indicatorWidth;
  }

  private _setTopRange() {
    this.visibleRangeIndexTop = this.visibleRangeIndexBottom + (this._visibleCount - 1);
  }

  // Description: trigger the sliding of the thumbs (left or right); set arrow active state and visibleRangeIndexes
  moveSlides(action: string) {
    if (action === 'next') {
      const max = this.indicatorWidth - window.innerWidth + this.thumbMarginHorizontal / 2,
        lft = this.thumbLeft - this._outerThumbWidth;
      this.thumbLeft = Math.abs(lft) <= max ? lft : -max;
      this.visibleRangeIndexBottom++;
    } else if (action === 'prev') {
      const lft = this.thumbLeft + this._outerThumbWidth;
      this.thumbLeft = lft > 0 ? 0 : lft;
      this.visibleRangeIndexBottom--;
    } else {
      // Resets/shifts to  active slide in gallery or first slide
      this.visibleRangeIndexBottom =
        this._activeSlideIndex + this._visibleCount <= this.count ? this._activeSlideIndex : this.count - this._visibleCount;
      this.thumbLeft = this.visibleRangeIndexBottom > 0 ? -(this._outerThumbWidth * this.visibleRangeIndexBottom) : 0;
    }

    this._setTopRange();
    this.setThumbnailsPosition();

    this.updateThumbSliderRange.emit({
      bottom: this.visibleRangeIndexBottom,
      top: this.visibleRangeIndexTop
    });
  }

  // Description: Update the left position of the slider - transition
  setThumbnailsPosition(): void {
    this.zone.run(() => {
      // Needs zone for detecting changes in main carousel and syncing functionalities
      this.thumbnailsLeft = this.thumbLeft + 'px';
      this.updateArrowActiveStates();
      this.lazyLoadImages();
    });
  }

  // Description: update the active state of the next/prev arrow buttons
  updateArrowActiveStates() {
    this.isPrevActive = this.thumbLeft < 0;
    this.isNextActive = this.visibleRangeIndexTop < this.count - 1;
  }

  getSafeStyle(value: string): SafeStyle {
    return this.sanitization.bypassSecurityTrustStyle(value);
  }

  @HostListener('window:resize', ['$event'])
  sizeChange(event) {
    this.setIndicatorsWidth();
    this.moveSlides('reset');
  }

  public lazyLoadImages() {
    if (!!this.imageLiArr && this.imageLiArr.length > 0) {
      let image, src, srcUrl;
      const btm = this.visibleRangeIndexBottom,
        top =
          this.visibleRangeIndexTop + this._visibleCount < this.galleryItems.length
            ? this.visibleRangeIndexTop + this._visibleCount
            : this.galleryItems.length - 1; // Lazy load 2 ranges
      for (let i = btm; i <= top; i++) {
        image = this.imageLiArr[i];
        src = !!image && image.nativeElement.dataset.src;
        if (!!src) {
          srcUrl = src;
          this.renderer.setStyle(image.nativeElement, 'backgroundImage', 'url(' + srcUrl + ')');
          this.renderer.removeAttribute(image.nativeElement, 'data-src');
        }
      }
    } else {
      // console.error('Error: ImageLinkArray is undefined');
    }
  }

  // Description: Checks thumbslider has the image and adjusts if its not visible. Catch for sliding with main carousel control
  public adjustThumbSliderOnSlid(activeIndex: number) {
    this._activeSlideIndex = activeIndex;
    const maxIndex = this.visibleRangeIndexBottom + this._visibleCount; // See if the currently active thumb is hidden
    if (activeIndex >= maxIndex || activeIndex < this.visibleRangeIndexBottom) {
      this.moveSlides('reset');
    }
  }
}
