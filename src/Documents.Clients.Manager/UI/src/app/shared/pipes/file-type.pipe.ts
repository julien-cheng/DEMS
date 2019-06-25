import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'fileType'
})
export class FileTypePipe implements PipeTransform {
  config: any = {
    ManagerFileModel: 'file',
    ManagerPathModel: 'folder', // ADD MORE...
  }
  
  transform(value: any, args?: any): any {
    return (this.config[value]) || 'item';
  }

}
