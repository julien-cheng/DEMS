import { Component, OnInit, Inject, Input } from '@angular/core';
import { AppConfigService,IFolderIdentifier, FileService, IFile, IconPipe, ExplorerService } from '../shared/index';
import { JQ_TOKEN } from '../shared/services/jQuery.service';
@Component({
  selector: 'app-navigation',
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.scss']
})
export class NavigationComponent implements OnInit {
  @Input() folderIdentifier: IFolderIdentifier;
  public foundFiles: IFile[];
  public searchTerm: string;
  public folderKey: string;
  
  constructor(
    private explorerService: ExplorerService,
    private fileService: FileService,
    public appConfigService: AppConfigService,
    @Inject(JQ_TOKEN) private $: any) {
  }

  ngOnInit() {
   
  }
  
  // Description: search functionality for files and folder
  searchFolder(searchTerm) {
    // console.log(searchTerm);
    this.$('#searchResultsModal').modal();
    // Make call to the server to get results... Working TBD
    //  this.fileService.searchFiles(searchTerm).subscribe((files) => {
    //   this.foundFiles = files;
    //   this.$('#searchResultsModal').modal();
    // });
    return false;
  }
}
