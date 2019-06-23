import 'axios';
import Axios, { AxiosResponse, AxiosRequestConfig } from 'axios';
import { IPathIdentifier, IFileIdentifier } from '@/models/identifiers.model';
import { ItemQueryType, ViewMode } from '@/models/view.model';
import { IManager } from '@/models/manager.model';
import { IBreadcrumb } from '@/models/breadcrumb.model';
import { IPath, IPathTreeNode } from '@/models/path.model';

export interface IExplorer {
  //folderKey: string;
  pathIdentifier?: IPathIdentifier;
  fileIdentifier?: IFileIdentifier;
  activePath?: ItemQueryType;
  currentExplorer?: IManager;
  activeNodeKey?: string; // current active pathKey
  currentTreeExplorer?: any;
  isCollapsed?: boolean; // Hold the collapsed state of the file explorer
  listviewMode?: ViewMode; // Hold the view mode of the listview
  nameSuffix?: string; // Add a suffix to the end of the ids
}

export class ExplorerService {
  public rootLabel = 'Case Files';
  public fileExplorer: IExplorer = {
    isCollapsed: false,
  }; // Holds current Explorer object with all associated values - default values

  constructor() {}

  // Sets the status of the current explorer
  setCurrentExplorer(folder: IManager, pathIdentifier: IPathIdentifier): IExplorer {
    //  console.log( this.fileExplorer.pathIdentifier.folderKey  + ' - '+activeNodeKey );
    this.fileExplorer.pathIdentifier = pathIdentifier;
    this.fileExplorer.currentExplorer = folder;
    this.fileExplorer.activeNodeKey = !!pathIdentifier.pathKey ? pathIdentifier.pathKey : 'root';
    this.fileExplorer.currentTreeExplorer = this.processTreeData(this.fileExplorer.currentExplorer);
    return this.fileExplorer;
  }

  // Returns flag to see if the currentExplorer is instantiated
  isExplorerReady() {
    // Returns true if currentExplorer is set
    return !!this.fileExplorer.currentExplorer;
  }

  buildBreadCrumbs(pathIdentifier: IPathIdentifier): IBreadcrumb[] {
    let bradcrumbArr: IBreadcrumb[] = [];
    let pathtree = null;
    if (this.fileExplorer.currentExplorer) {
      pathtree = this.fileExplorer.currentExplorer.pathTree;
    }
    let activeNode = this.fileExplorer.activeNodeKey;
    bradcrumbArr = pathtree
      ? (this.getTree(pathtree, activeNode, [pathtree]) as IBreadcrumb[])
      : [];
    return bradcrumbArr;
  }

  getTree(branch: IPath, activeNode: string | undefined, arr: any[] | IPath[]) {
    if (!!branch.paths) {
      let subBranch = branch.paths.filter((path: any) => {
        const childrenPaths = this.getObjects(path, 'pathKey', activeNode);
        return childrenPaths.length > 0;
      });
      if (!!subBranch && subBranch.length > 0) {
        arr.push(subBranch[0]);
        this.getTree(subBranch[0], activeNode, arr);
      }
    }
    return arr;
  }

  // ---------------------------------------------------------------------------------------------
  // ITreeNode Converting Methods
  // Convert currentExplorer into a TreeNode objec - Eventually replaced with in-house tree method
  // Returns a TreeNode Object ( { TreeModule, TreeNode } from 'angular-tree-component')
  treeID: number = 0;
  processTreeData(currentExplorer: IManager, sufix: string = ''): any[] {
    this.fileExplorer.nameSuffix = sufix;
    this.treeID = 1;
    const _self = this;
    const treeNodesDynamic: any = [];
    let pathtree = currentExplorer.pathTree;
    // console.log(currentExplorer);
    if (!currentExplorer) {
      console.log('Error: currentExplorer is undefined');
      return [];
    }

    // Build the first node (root) - IFolder
    treeNodesDynamic.push(_self.fnBuildFolderNodeObject(currentExplorer, sufix));

    // Build ItemQueryType Nodes:
    // Children levels to root.paths - only add if there are children
    if (pathtree.paths) {
      const childrenPaths = _self.fnParsePathChildren(pathtree.paths, [], sufix);
      if (childrenPaths.length) {
        // childrenPaths = _self.fnExpandTreeBranch(childrenPaths);
        treeNodesDynamic[0].children = childrenPaths;
      }
    }
    return currentExplorer !== undefined ? treeNodesDynamic : null;
  }

  // Description: Recursively iterates through the ItemQueryType Array and converts it to a TreeNode Object
  fnParsePathChildren(aPath: IPath[], arrayHolder: any, sufix: string = ''): any {
    const _self = this,
      arr = aPath.map(oPath => {
        const convertedObj = _self.fnBuildPathNodeObject(oPath, sufix);
        arrayHolder.children !== undefined && arrayHolder.children.length
          ? arrayHolder.children.push(convertedObj)
          : arrayHolder.push(convertedObj);

        oPath.paths !== undefined &&
          oPath.paths !== null &&
          _self.fnParsePathChildren(oPath.paths, convertedObj.children);

        return convertedObj;
      });
    return arr;
  }

  // Description: Parse and convert top level IManager to a top level TreeNode
  // and Set my fileExplorer.ActivePath  if it is Root (null)
  fnBuildFolderNodeObject(data: IManager, sufix: string = ''): IPathTreeNode {
    // console.log(data);
    let obj = {
      id: 'root' + sufix,
      name:
        data.pathTree.name !== undefined && data.pathTree.name.length > 0
          ? data.pathTree.name
          : this.rootLabel,
      isExpanded: true,
      children: [],
      icon: 'folder',
      // fullPath: null,
      // allowedOperations: data.allowedOperations
    };

    const oNode: IPathTreeNode = Object.assign({}, data.pathTree, obj);

    // Set my ActivePath Node as ROOT: key for root === null
    this.fileExplorer.activeNodeKey === oNode.identifier.pathKey &&
      (this.fileExplorer.activePath = data.pathTree);
    // console.log('Root: '+ this.fileExplorer.activeNodeKey, oNode.identifier);
    return oNode;
  }

  // Description: Parse and convert lower level ItemQueryType object to Lower level TreeNode Children
  // and Set my fileExplorer.ActivePath
  // Returns a single TreeNode Child object
  fnBuildPathNodeObject(oPath: IPath, sufix: string = ''): IPathTreeNode {
    const _self = this,
      hasActiveChildren = _self.getObjects(oPath, 'fullPath', this.fileExplorer.activeNodeKey);

    // Determine onload if this node needs to be expanded by looking at children obj
    let isExpanded = oPath.isExpanded
      ? oPath.isExpanded
      : this.fileExplorer.activeNodeKey === oPath.identifier.pathKey ||
        hasActiveChildren.length > 0; // console.log(oPath.name, isExpanded);
    let obj = {
      id:
        (oPath.identifier.pathKey !== undefined && oPath.identifier.pathKey !== null
          ? oPath.identifier.pathKey
          : this.treeID) + sufix,
      icon: oPath.icons !== undefined && oPath.icons !== null ? oPath.icons.join(' ') : 'folder',
      isExpanded: isExpanded, // isExpanded
      children: [], // data.paths
      // name: oPath.name,
      // fullPath: oPath.fullPath,
      //allowedOperations: oPath.allowedOperations
    };
    const oItemQueryTypeNode: IPathTreeNode = Object.assign({}, oPath, obj);
    this.treeID++;
    return oItemQueryTypeNode;
  }

  // Description: get a child object with the key value pair
  getObjects(obj: IPath, key: string, val: string | undefined) {
    let objects: (any | never)[] = [];
    const _self = this;
    for (const i in obj) {
      if (!obj.hasOwnProperty(i)) {
        continue;
      }
      if (typeof (obj as any)[i] === 'object') {
        objects = objects.concat(_self.getObjects((obj as any)[i], key, val));
      } else if (i === key && (obj as any)[key] === val) {
        objects.push(obj);
      }
    }
    return objects;
  }

  collapseFileExplorer(collapsed: boolean) {
    this.fileExplorer.isCollapsed = collapsed;
  }
}
