import {
  Component,
  OnInit,
  Inject,
  ElementRef,
  ViewChild,
  ViewContainerRef,
  ComponentFactoryResolver,
  ComponentFactory,
  OnDestroy,
  ComponentRef,
  AfterContentChecked,
  AfterViewInit
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import {
  LoadingService,
  BatchOperationService,
  FileService,
  ExplorerService,
  PathService,
  BatchResponse,
  BatchOperationsComponent
} from '../index';
import {
  JQ_TOKEN,
  IPathIdentifier,
  IFileIdentifier,
  IManager,
  IFile,
  ItemQueryType,
  IRequestBatchData,
  IAllowedOperation,
  IBatchResponse,
  FileSetTypes,
  IFileViewer,
  IMessaging
} from '../index';
import { PdfComponent } from './pdf/pdf.component';
import { VideoBasicComponent } from '../../media/video-basic/video-basic.component';
import { ImageComponent } from './image/image.component';
import { OfflineComponent } from './offline/offline.component';
import { AudioComponent } from './audio/audio.component';
import { DefaultComponent } from './default/default.component';
import { UnknownComponent } from './unknown/unknown.component';

@Component({
  selector: 'manager-details',
  templateUrl: './manager-details.component.html',
  styleUrls: ['./manager-details.component.scss']
})
export class ManagerDetailsComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild(BatchOperationsComponent)
  BatchOperationsComponent: BatchOperationsComponent;
  @ViewChild('detailView', { read: ViewContainerRef })
  detailViewParentContainer: ViewContainerRef;
  @ViewChild('fullscreenContainer', { read: ViewContainerRef })
  fullscreenContainer: ViewContainerRef;
  private detailViewComponentRef;
  public file: IFile;
  public rootFileIdentifier: IFileIdentifier; // Parent file uploaded
  public fileIdentifier: IFileIdentifier;
  public pathIdentifier: IPathIdentifier;
  public fileName: string;
  public fileSet: FileSetTypes;
  public viewerType: string;
  public allowedOperations: IAllowedOperation[];
  public fileViews: IFileViewer[];
  public manager: IManager;
  public previewVisible = false;
  // This is a helper method that will give me back keys for an object.  Attributes come back as a keyed object.
  public objectKeys = Object.keys;
  public message: IMessaging;
  constructor(
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute,
    private fileService: FileService,
    private pathService: PathService,
    private componentFactoryResolver: ComponentFactoryResolver,
    private viewContainerRef: ViewContainerRef,
    public explorerService: ExplorerService,
    public batchOperationService: BatchOperationService,
    public loadingService: LoadingService,
    @Inject(JQ_TOKEN) private $: any
  ) {}

  // TO DO: Need to get FolderKey and Parent PathKey on the file object... onload doesnt provide the pathKey properly
  ngOnInit() {
    this.route.data.forEach(data => {
      const identifiers = this.route.snapshot.data.identifiers;
      this.fileIdentifier = identifiers.fileIdentifier;
      this.pathIdentifier = identifiers.pathIdentifier;
      this.viewerType = identifiers.viewerType;
      this.setFileObject();
    });
  }
  ngAfterViewInit() {
    window.scroll(0, 0);
  }

  // Description Set file Object and View data
  setFileObject() {
    if (!!this.fileIdentifier) {
      // Call the proper set and pass it to the detailviewType
      this.fileService.getFileMediaSet(this.fileIdentifier, this.viewerType).subscribe(
        (response: IBatchResponse) => {
          this.fileSet = response.response as FileSetTypes;
          this.allowedOperations = this.fileSet.allowedOperations;
          this.rootFileIdentifier = this.fileSet.rootFileIdentifier;
          this.fileViews = this.fileSet.views;
          this.message = this.fileSet.message;

          // Call the Explorer on every page load - breadcrumbs won't have the information otherwise (proper view is not there in some cases - coming from search)
          this.getExplorer();
          if (this.rootFileIdentifier == null)
            this.viewerType = "offline";

          if (!!this.viewerType) {
            this.loadComponent(this.viewerType);
          }
        },
        () => {
          this.loadingService.setLoading(false);
        }
      );
    }
  }

  IsGPSCoordinate(value) {
    if (value != null && (value as string) === 'GPS Coordinates') {
      return true;
    }

    return false;
  }

  setFileName() {
    this.file = this.fileService.getFileFromViews(this.manager.views, this.rootFileIdentifier);
    this.fileName = !!this.fileSet.name ? this.fileSet.name : !!this.file ? this.file.name : '';
  }

  // Description: updates explorer and main page
  getExplorer(): void {
    const self = this;
    if (!!this.pathIdentifier) {
      this.pathService.getPathPage(this.pathIdentifier).subscribe(
        response => {
          self.manager = response.response as IManager;
          if (self.manager !== undefined) {
            self.explorerService.setCurrentExplorer(self.manager, self.pathIdentifier);
          }
          self.setFileName();
        },
        error => {
          throw new Error('Manager is undefined - redirect to error');
        }
      );
    } else {
      console.error("Manager's required Keys are undefined - redirect to error");
    }
  }

  toggleFullscreen() {
    const doc = document as any;
    if (doc.fullscreenElement || doc.webkitFullscreenElement || doc.mozFullScreenElement || doc.msFullscreenElement) {
      if (doc.exitFullscreen) {
        doc.exitFullscreen();
      } else if (doc.mozCancelFullScreen) {
        doc.mozCancelFullScreen();
      } else if (doc.webkitExitFullscreen) {
        doc.webkitExitFullscreen();
      } else if (doc.msExitFullscreen) {
        doc.msExitFullscreen();
      }
    } else {
      const element = this.fullscreenContainer.element.nativeElement as any;
      if (element.requestFullscreen) {
        element.requestFullscreen();
      } else if (element.mozRequestFullScreen) {
        element.mozRequestFullScreen();
      } else if (element.webkitRequestFullscreen) {
        element.webkitRequestFullscreen((window as any).Element.ALLOW_KEYBOARD_INPUT);
      } else if (element.msRequestFullscreen) {
        element.msRequestFullscreen();
      }
    }
  }
  // Description: show preview panel on the right
  togglePreviewPanel() {
    this.previewVisible = !this.previewVisible;
  }
  // Description: Load the correct control dynamically
  loadComponent(viewerType: string) {
    // console.log('loadComponent: ' + detailViewType);
    // Clear template for new rendering if full - temporary for development
    if (this.detailViewParentContainer && this.detailViewParentContainer.length > 0) {
      this.detailViewParentContainer.clear();
    }
    // Render the correct Component dynamically ***** Working on this - more types and defaults
    // Here is where I need to call the /api/views/documentset ..
    let factory;
    switch (viewerType) {
      case 'pdf':
      case 'document':
        factory = this.componentFactoryResolver.resolveComponentFactory(PdfComponent);
        break;
      case 'video':
        factory = this.componentFactoryResolver.resolveComponentFactory(VideoBasicComponent);
        break;
      case 'audio':
        factory = this.componentFactoryResolver.resolveComponentFactory(AudioComponent);
        break;
      case 'image':
        factory = this.componentFactoryResolver.resolveComponentFactory(ImageComponent);
        break;
      case 'offline':
        factory = this.componentFactoryResolver.resolveComponentFactory(OfflineComponent);
        break;
      case 'text':
        factory = this.componentFactoryResolver.resolveComponentFactory(DefaultComponent);
        break;
      case 'unknown':
        factory = this.componentFactoryResolver.resolveComponentFactory(UnknownComponent);
        break;
      default:
        factory = this.componentFactoryResolver.resolveComponentFactory(DefaultComponent);
        break;
    }

    // This will create an instance of the component inside the parent container.  We're saving
    // a reference to this child component so we can change properties on it, destroy it when needed, and call change detection.
    this.detailViewComponentRef = this.detailViewParentContainer.createComponent(factory);

    // Pass the child component the file
    if (!!this.detailViewComponentRef) {
      viewerType === 'video'
        ? (this.detailViewComponentRef.instance.mediaSet = this.fileSet)
        : (this.detailViewComponentRef.instance.fileSet = this.fileSet);
      this.detailViewComponentRef.instance.viewerType = this.viewerType;
      // You have to run a change detection cycle after creating the component, and then changing values on it.  Specifically the file url property. Otherwise you will get a Error: ExpressionChangedAfterItHasBeenCheckedError:
      this.detailViewComponentRef.changeDetectorRef.detectChanges();
    } else {
      this.toastr.error('There was and error with your request');
    }
  }

  ngOnDestroy() {
    if (this.detailViewComponentRef) {
      this.detailViewComponentRef.destroy();
    }
  }

  // ----------------------------------------------------------------
  // Batch Operations component:
  // ----------------------------------------------------------------
  public processBatchUiAction($event: any) {
    return this.BatchOperationsComponent.processBatchUiAction($event);
  }

  // updateView Output call:
  public updateView(arg?: any) {
    this.loadingService.setLoading(false);
    const successMessage = !!arg && !!arg.hasOwnProperty('successMessage') ? arg.successMessage : null;
    if (!!successMessage) {
      this.BatchOperationsComponent.postMessage(successMessage, 'success');
    }
    if (this.batchOperationService.batchRequest.requestType === 'DeleteRequest') {
      this.router.navigate(['/manager', this.pathIdentifier.organizationKey, this.pathIdentifier.folderKey, this.pathIdentifier.pathKey]);
    }
  }
}
