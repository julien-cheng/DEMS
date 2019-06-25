import { IPath, IAllowedOperation, ViewType, ItemQueryType } from '../index';

// Itemquery response object
export interface IManager { 
    pathName: string;
    allowedOperations?: IAllowedOperation[];
    pathTree: IPath;
    views: ViewType[];
    fileNameValidationPatterns?:ValidationPatterns[];
    pathNameValidationPatterns?:ValidationPatterns[];
}

// Form validation patterns for itemquery type add and rename
export type ValidationPatterns = {
    type: string;
    isAllowed: boolean;
    pattern: string;
}

export interface IAutodownloadKeys {
    autodownloadString: string;
}