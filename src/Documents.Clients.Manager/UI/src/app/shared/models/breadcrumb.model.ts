import { IManager, ItemQueryType } from '../index';

export interface IBreadcrumb {
  folderKey: string;
  pathKey: string;
  isActivePath: boolean;
  name: string;
}
