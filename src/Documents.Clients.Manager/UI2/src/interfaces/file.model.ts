import { IItemQueryTypeBase } from './view.model';
import { IFileIdentifier, IPathIdentifier } from './identifiers.model';

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
  name: string;
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
  Unknown,
}
