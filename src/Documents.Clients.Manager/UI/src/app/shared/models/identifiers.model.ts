
export type TIdentifierType = 'IFileIdentifier' |'IPathIdentifier' | 'IFolderIdentifier' | 'IOrganizationIdentifier' | 'IUserIdentifier';

// FolderIdentifier is base
export interface IOrganizationIdentifier {
	organizationKey: string;
}

export interface IFolderIdentifier extends IOrganizationIdentifier{
    folderKey: string;
}

export interface IPathIdentifier extends IFolderIdentifier{
    pathKey: string;
    isRoot?: boolean;
}

export interface IFileIdentifier extends IFolderIdentifier{
    fileKey: string;
}

// This might be removed later for external autodownload on load


//TO BE POPULATED:
export interface IUserIdentifier extends IOrganizationIdentifier{
}
