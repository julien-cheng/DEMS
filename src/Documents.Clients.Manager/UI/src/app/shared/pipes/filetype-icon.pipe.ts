import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'filetypeIcon'
})
export class FiletypeIconPipe implements PipeTransform {
// https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types
config: any = {
    fileTypes: [
      { filetype: 'video/*', faIcon: 'fa-file-video-o'},
      { filetype: 'audio/*', faIcon: 'fa-file-audio-o'},
      { filetype: 'audio/*', faIcon: 'fa-file-video-o'},
      { filetype: 'image/*', faIcon: 'fa-image'},
      { filetype: 'text/plain', faIcon: 'fa-file-text-o'},
      { filetype: 'application/pdf', faIcon: 'fa-file-pdf-o'},
      { filetype: 'application/msword', faIcon: 'fa-file-word-o'},
      { filetype: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', faIcon: 'fa-file-excel-o'},
      { filetype: 'application/vnd.openxmlformats-officedocument.presentationml.presentation', faIcon: 'fa-file-powerpoint-o'},
      { filetype: 'application/x-zip-compressed', faIcon: 'fa-file-archive-o'},
    ],
    default: 'fa-file-o'
  };
  transform(value: any, args?: any): any {
    let iconClass: string;
    this.config.fileTypes.forEach((obj) => {
      const patt = new RegExp(obj.filetype);
      if (patt.test(value)) {
        return iconClass = obj.faIcon;
      };
    });
    iconClass === undefined && (iconClass = this.config.default);
    return iconClass;
  }

}
