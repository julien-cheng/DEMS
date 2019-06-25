import { IFolderIdentifier } from '../index';

export interface IFolder {
    identifier: IFolderIdentifier;
    name:string;
    type: string; // ManagerFolderModel
    fields: IFolderFields;
}

export interface IFolderFields {
    firstName?: string;
    lastName?: string;
    arrestNumber?:number;
    docketNumber?: number;
    trialNumber?: number;
    icmsNumber?: number;
    indictmentNumber?: number;
}
