import { Component, OnInit, Input, ViewChild, ElementRef, Inject, AfterContentInit } from '@angular/core';
import { JQ_TOKEN } from '../../services/jQuery.service';

@Component({
  selector: 'app-bootstrap-modal',
  templateUrl: './bootstrap-modal.component.html',
  styleUrls: ['./bootstrap-modal.component.scss']
})
export class BootstrapModalComponent implements OnInit, AfterContentInit {
  @Input() title: string;
  @Input() modalSizeClass: string; // modal-lg for large modals, modal-sm for small modals, and empty for default medium size
  @Input() modalClass: string;
  @Input() elementID: string;
  @Input() closeOnBodyCLick = '';
  @ViewChild('modalcontainer') containerEl: ElementRef;
  @ViewChild('modalFooter') modalFooter;
  showFooter = false;

  constructor(@Inject(JQ_TOKEN) private $: any) {}

  ngOnInit() {}

  ngAfterContentInit() {
    this.showFooter = this.modalFooter.nativeElement.children.length > 0;
  }

  // Description: Allow for redirect on Link clicks
  closeModal(event) {
    event.target.tagName === 'A' &&
      this.closeOnBodyCLick.toLocaleLowerCase() === 'true' &&
      this.$(this.containerEl.nativeElement).modal('hide');
  }
}
