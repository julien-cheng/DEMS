import { Component, OnInit, Input, Inject, AfterViewInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { JQ_TOKEN } from '../../services/jQuery.service';
import { IMessaging } from '../../models/messaging.model';

@Component({
  selector: 'app-messaging',
  templateUrl: './messaging.component.html',
  styleUrls: ['./messaging.component.scss']
})
export class MessagingComponent implements OnInit, AfterViewInit {
  @Input() message: IMessaging;
  type: 'alert' | 'toastr' | 'dialog' = 'alert';
  constructor(private toastr: ToastrService, @Inject(JQ_TOKEN) private $: any) {}

  ngOnInit() {
    this.message.type && (this.type = this.message.type);
  }

  // Description: initialize modal and toastr messaging if needed
  ngAfterViewInit() {
    if (this.type === 'dialog') {
      console.log('pop');
      this.$('#messageDialog').modal();
    }

    if (this.type === 'toastr') {
      this.toastr.warning(this.message.body, this.message.title);
    }
  }
}
