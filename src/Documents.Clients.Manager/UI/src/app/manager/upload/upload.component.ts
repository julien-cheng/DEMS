import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { IframeResizerService, ExplorerService, AuthService, IPathIdentifier } from '../index';
@Component({
  selector: 'app-upload',
  templateUrl: './upload.component.html',
  styleUrls: ['./upload.component.scss']
})
export class UploadComponent implements OnInit {
  public folderKey: string;
  public pathKey: string;
  public pathIdentifier: IPathIdentifier;
  constructor(
    public auth: AuthService,
    private route: ActivatedRoute,
    private explorerService: ExplorerService,
    public iframeResizerService: IframeResizerService,
    private router: Router
  ) {
    //  this.iframeResizerService.setTopNavVisible(true);
  }

  ngOnInit() {
    this.explorerService.fileExplorer.isCollapsed = true;
    this.pathIdentifier = this.explorerService.fileExplorer.pathIdentifier;
    this.folderKey = this.explorerService.fileExplorer.pathIdentifier.folderKey; // To be transfered to identifier
    this.pathKey = this.route.snapshot.params.pathKey;

    // Redirect out if read only is set to true
    if (this.auth.readOnly) {
      this.router.navigate(['/']);
    }
  }
}
