import { Directive, Inject, Input, ElementRef, OnChanges } from '@angular/core';

@Directive({
  selector: '[appFocus]'
})
export class FocusDirective {
  @Input('appFocus') focus: boolean;
  constructor(@Inject(ElementRef) private element: ElementRef) {}

  protected ngOnChanges() {
    if (this.focus) {
      this.element.nativeElement.focus();
    }
    // console.log(this.element.nativeElement);
    // console.log(this.focus);
  }
}
