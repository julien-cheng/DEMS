import { Injectable } from '@angular/core';

@Injectable()

export class LoadingService {
  public loading: boolean;
  constructor() {
   this.setLoading(true);
  }

  public setLoading(isLoading: boolean) {
    //this.loading = isLoading;
    setTimeout(()=> this.loading = isLoading);
  }
}
