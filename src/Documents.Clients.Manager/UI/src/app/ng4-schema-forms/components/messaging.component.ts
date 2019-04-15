import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'control-message',
  template: `
    <div *ngIf="!!message && messageType === 'errorMessage'" class="invalid-feedback d-block" [innerHTML]="message"></div>

    <div
      *ngIf="!!message && messageType === 'customErrorMessage'"
      class="invalid-feedback"
      [ngClass]="{ 'd-block': isValid === false }"
      [innerHTML]="message"
    ></div>
    <small *ngIf="!!message && messageType === 'description'" class="form-text text-muted" [innerHTML]="message"> </small>
  `,
  styleUrls: ['./schema-forms.component.scss']
})
export class MessagingComponent implements OnInit {
  @Input() messageType: string | undefined;
  @Input() message: string | undefined;
  @Input() isValid?: boolean;
  constructor() {}

  ngOnInit() {}
}
