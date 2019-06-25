import { Directive, HostListener, ElementRef, OnInit, Input } from "@angular/core";

@Directive({
  selector: '[appInputTrim]'
})

// Trim strings 
export class InputTrimDirective {
  @Input('appInputTrim') trimFormControl;
  @Input('isSingleString') isSingleString?: boolean= false;
  constructor() {
  }

  ngOnInit() {
  }

  @HostListener("blur", ["$event.target.value"])
  onBlur(value) {
    this.trimControlValue(value);
  }

  @HostListener("keyup", ["$event", "$event.target.value"])
  onkeyup(e,value) {
    // Do only on space bar and only if its a single string (i.e. email)
    (this.isSingleString && e.keyCode === 32) &&  this.trimControlValue(value);
  }

  private trimControlValue(value: string) {
    let trimmedVal = value.toString().trim();
    (!!trimmedVal && trimmedVal.length > 0) &&
      this.trimFormControl.setValue(trimmedVal);
  }

}
