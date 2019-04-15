import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'iconClass' })
export class IconPipe implements PipeTransform {
  transform(value: string[], args: string): string {
    if (value !== undefined && value !== null) {
      return value.length > 0 ? value.join(' ') : args !== undefined && args !== null ? args : 'file-o';
    } else {
      // Default to folder/path
      return args !== undefined && args !== null ? args : 'folder';
    }
  }
}
