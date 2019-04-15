import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'description',
  template: `
    <small class="form-text text-muted"> {{ message }} </small>
  `
})
export class DescriptionComponent implements OnInit {
  @Input() message: string | undefined;

  constructor() {}

  ngOnInit() {}
}
