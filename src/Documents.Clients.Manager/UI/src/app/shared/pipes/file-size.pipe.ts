import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'fileSize'
})
// Description: converst bytes into KB, MB or GB
// optional pass binary = true (ex: {{ item?.size | fileSize:true }}) for conversion in binary or false/not defined for decimal conversions
export class FileSizePipe implements PipeTransform {
  config: any = {
    units: [
      { size: 1000000000, suffix: ' GB', prefix: null },
      { size: 1000000, suffix: ' MB', prefix: null },
      { size: 1000, suffix: ' KB', prefix: null }
    ],
    unitsBinary: [
      // Binary conversions
      { size: 1073741824, suffix: ' GB', prefix: null },
      { size: 1048576, suffix: ' MB', prefix: null },
      { size: 1024, suffix: ' KB', prefix: null }
    ]
  };
  transform(bytes: number, binary: boolean = false): string {
    if (!Number(bytes)) {
      return '';
    }
    let unit: any = {},
      i = 0,
      prefix,
      suffix;
    const unitsConfig = binary ? this.config.unitsBinary: this.config.units;
    while (unit) {
      unit = unitsConfig[i];
      prefix = unit.prefix || '';
      suffix = unit.suffix || '';
      if (i === unitsConfig.length - 1 || bytes >= unit.size) {
        return prefix + (bytes / unit.size).toFixed(2) + suffix;
      }
      i += 1;
    }
    return null;
  }

}
