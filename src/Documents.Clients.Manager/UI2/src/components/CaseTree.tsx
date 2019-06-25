import React from 'react';
import Link from 'umi/link';
import { Tree } from 'antd';
import ManagerPage from '@/pages/manager';
import FilePage from '@/pages/file';
const { TreeNode, DirectoryTree } = Tree;

class CaseTree extends React.Component<
  { pathTree: any; manager?: ManagerPage | FilePage },
  { pathTree: any; manager?: ManagerPage | FilePage }
> {
  constructor(props: any) {
    super(props);
    this.state = {
      pathTree: props.pathTree,
      manager: props.manager,
    };
  }
  onSelect() {}

  onExpand() {}

  titleOnClick() {
    (this.state.manager as ManagerPage).props.match.params.path = node.fullPath;
    (this.state.manager as ManagerPage).fetchDirectory();
  }
  title(node: any, top: boolean = false) {
    if (this.state.manager) {
      let path =
        `/manager/${this.state.manager.props.match.params.organization}/${this.state.manager.props.match.params.case}/` +
        encodeURIComponent(node.fullPath);
      return (
        <Link to={path} onClick={this.titleOnClick}>
          {top ? 'Case Files' : node.name}
        </Link>
      );
    } else {
      return top ? 'Case Files' : node.name;
    }
  }

  mapTreeNodeToComponent(node: any, top: boolean = false): any {
    var self = this;
    var items =
      node.paths &&
      node.paths.map((x: any) => {
        return self.mapTreeNodeToComponent(x);
      });
    return (
      <TreeNode title={this.title(node, top)} key={node.fullPath}>
        {items}
      </TreeNode>
    );
  }

  render() {
    return (
      <div>
        <h1>{JSON.stringify(this.state.pathTree)}</h1>
        <DirectoryTree
          multiple={true}
          defaultExpandAll={true}
          onSelect={this.onSelect}
          onExpand={this.onExpand}
          selectable={false}
          autoExpandParent={true}
        >
          {this.state.pathTree && this.mapTreeNodeToComponent(this.state.pathTree(), true)}
        </DirectoryTree>
      </div>
    );
  }
}

export default CaseTree;
