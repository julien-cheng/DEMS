import { IItemQueryTypeBase, IPathIdentifier } from '../index';

export interface IPath extends IItemQueryTypeBase {
  identifier: IPathIdentifier;
  paths?: IPath[];

  // IItemQueryTypeBase:
  // key: string;
  // pathKey: string;
  // folderKey:string;
  // type: string; // ManagerPathModel
  // name: string;
  // icons?: string[];
  // allowedOperations?: IAllowedOperation[];
}

export interface IPathTreeNode extends IPath {
  id: string;
  icon?: string;
  children?: IPathTreeNode[];
}
