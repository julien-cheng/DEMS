// Description: Convert object to querystring
export class QuerystringPipe {
  static transform(value: any, args?: any): any {
    let parts = [];
    for (let i in value) {
      if (value.hasOwnProperty(i)) {
        parts.push(encodeURIComponent(i) + '=' + encodeURIComponent(value[i]));
      }
    }
    return parts.join('&');
  }
}
