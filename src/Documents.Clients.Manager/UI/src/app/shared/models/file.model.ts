import { IAllowedOperation, IItemQueryTypeBase, IPathIdentifier, IFileIdentifier } from '../index';

export interface IFile extends IItemQueryTypeBase {
  identifier: IFileIdentifier;
  pathIdentifier: IPathIdentifier;
  viewerType: string;
  length: number;
  lengthForHumans: string;
  created?: Date;
  modified?: Date;
  hashes?: Object;
  previewImageIdentifier?: IFileIdentifier;
  views: IFileViewer[];
}

export interface IFileViewer {
  identifier: IFileIdentifier;
  viewerType: ViewerTypeEnum;
  icons: string[];
  label: string;
  type: string;
}

export enum ViewerTypeEnum {
  Document,
  Image,
  Audio,
  Video,
  Unknown
}
