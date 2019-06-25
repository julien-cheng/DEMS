import { Directive, Inject, Input, ElementRef } from '@angular/core';

@Directive({
  selector: '[appFocus]'
})
export class FocusDirective {
  @Input('appFocus') focus: boolean;
  constructor( @Inject(ElementRef) private element: ElementRef) {
  }

  protected ngOnChanges() {
     (this.focus) &&  this.element.nativeElement.focus();
      // console.log(this.element.nativeElement);
      // console.log(this.focus);
  }
}
