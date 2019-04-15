import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs/Subscription';

@Component({
  selector: 'app-errors',
  templateUrl: './errors.component.html',
  styleUrls: ['./errors.component.scss']
})
export class ErrorsComponent implements OnInit, OnDestroy {
  // error: HttpError;
  private subscriptions: Subscription;

  constructor() {
    this.subscriptions = new Subscription();
  }

  ngOnInit() {
    // const errorSubscription = this.userService.error$
    //   .subscribe(
    //   (error: HttpError) => this.error = error
    //   );
    // this.subscriptions.add(errorSubscription);
  }
  ngOnDestroy(): void {
    // this.subscriptions.unsubscribe();
  }

  clearError(): void {
    //  this.userService.clearError();
  }
}
