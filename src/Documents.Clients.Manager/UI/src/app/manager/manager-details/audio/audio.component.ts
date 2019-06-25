import { Component, OnInit, Input } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { IFile, IFileIdentifier, FileService, IMediaSet, IMediaSource, IBatchResponse } from '../../index';

@Component({
  selector: 'app-audio',
  templateUrl: './audio.component.html',
  styleUrls: ['./audio.component.scss']
})
export class AudioComponent implements OnInit {
  @Input() fileSet: IMediaSet;
  @Input() viewerType: string;
  public fileIdentifier: IFileIdentifier;
  public fileUrl: string;
  public saferesourceURL: SafeResourceUrl;
  public containerInnerHeight: number;
  public sources: Array<any> = [];
  
  
  constructor(
    private fileService: FileService,
    private sanitizer: DomSanitizer
  ) { }

  ngOnInit() {
    if(!!this.fileSet.sources && this.fileSet.sources.length>0){
      this.buildVideoSources(this.fileSet.sources);
    }else{
      console.error("there was a problem with the file response sources");
    }
  }

  // Description: Build the video sources array
  buildVideoSources(sources: IMediaSource[]) {
    sources.forEach((source) => {
      let fileUrl = `/api/file/contents?fileidentifier.organizationKey=${source.fileIdentifier.organizationKey}&fileidentifier.folderKey=${source.fileIdentifier.folderKey}&fileidentifier.fileKey=${source.fileIdentifier.fileKey}&open=true`,
        obj = Object.assign(source, {
          fileUrl: this.saferesourceURL = this.sanitizer.bypassSecurityTrustResourceUrl(fileUrl)
        });
      this.sources.push(obj);
    });
  }


  // initPlayer() {
  //   let player = (<any>$('#player')).mediaelementplayer({
  //     pluginPath: 'mediaelement/build/',// Do not forget to put a final slash (/)
  //     shimScriptAccess: 'always',
  //     // stretching: 'responsive',
  //     success: function (mediaElement, originalNode, instance) { }
  //   }); 
  // }
  
}
