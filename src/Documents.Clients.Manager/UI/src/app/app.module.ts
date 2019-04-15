// Angulars
import { NgModule, ErrorHandler } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RouterModule, PreloadAllModules } from '@angular/router';
import { APP_BASE_HREF } from '@angular/common';
import { ToastrModule, ToastrConfig } from 'ngx-toastr';
import './rxjs-extensions';

// Utilities
import { appRoutes } from './routes';

// App Components
import { ErrorsComponent } from './errors/errors.component';
import { AppComponent } from './app.component';
import { NavigationComponent } from './navigation/navigation.component';

import {
  JQUERY_PROVIDER,
  FileService,
  ExplorerService,
  IframeResizerService,
  GlobalErrorHandler,
  AppConfigService,
  LoadingService
} from './shared/index';
import { SharedModule } from './shared/shared.module';

@NgModule({
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    FormsModule,
    HttpClientModule,
    // HttpModule,
    ReactiveFormsModule,
    RouterModule.forRoot(appRoutes, {
      preloadingStrategy: PreloadAllModules,
      onSameUrlNavigation: 'reload'
    }), // onSameUrlNavigation: 'reload' required for same url refresh (search results)
    SharedModule,
    ToastrModule.forRoot({
      closeButton: true,
      positionClass: 'toast-bottom-left'
    })
  ],
  declarations: [
    AppComponent,
    NavigationComponent,
    ErrorsComponent
    // Add directives and Pipes here
  ],
  exports: [],
  providers: [
    ExplorerService,
    FileService,
    IframeResizerService,
    LoadingService,
    AppConfigService, // NEEDS TO BE PROVIDED HERE ONLY - otherwise instance wont be shared
    JQUERY_PROVIDER,
    { provide: APP_BASE_HREF, useValue: '/' },
    { provide: ErrorHandler, useClass: GlobalErrorHandler }
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
