import { ErrorHandler } from '@angular/core';


export class GlobalErrorHandler extends ErrorHandler {
    constructor() {
        // Notes: The true paramter tells Angular to rethrow exceptions, so operations like 'bootstrap' will result in an error
        // when an error happens. If we do not rethrow, bootstrap will always succeed.
        // console.log('Error Constructor: GlobalErrorHandler');
        super();
    }
    handleError(error) {
        // Address Error-> send to server and user messaging
        console.log(error);
        // delegate to the default handler - optional, remove if ignoring default handler
        super.handleError(error);
    }
}
