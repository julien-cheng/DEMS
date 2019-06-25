import {InjectionToken} from '@angular/core';

export let TOASTR_TOKEN = new InjectionToken('toastr');

// Create an interface for intellisense or export interface Toastr: any for more complex ones
// export interface Toastr {
//     success(msg: string, title?:string): void;
//     info(msg: string, title?: string): void;
//     warning(msg: string, title?: string): void;
//     error(msg: string, title?: string): void;
// }
