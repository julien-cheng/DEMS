import { Popconfirm, Button } from 'antd';
import React from 'react';
import { FolderService } from '@/services/folder.service';
import Link from 'umi/link';
import { Row, Col, Tree, Table } from 'antd';
import { PathService } from '@/services/path.service';
import ManagerPage from '@/pages/manager';
const { TreeNode, DirectoryTree } = Tree;

class CaseTree extends React.Component<
  { pathTree: any; manager?: ManagerPage },
  { pathTree: any; manager?: ManagerPage }
> {
  constructor(props: any) {
    super(props);
    // console.log("kdiusgfygfyd",props);
    this.state = {
      pathTree: props.pathTree,
      manager: props.manager,
    };
  }
  onSelect = (keys: any, event: any) => {
    // console.log('Trigger Select', keys, event);
  };

  onExpand = (e: any) => {
    // console.log('Trigger Expand', e);
  };

  mapTreeNodeToComponent(node: any, top: boolean = false): any {
    var self = this;
    // console.log("FIRE",node.name, node.fullPath);
    return (
      <TreeNode
        title={
          self.state.manager ? (
            <Link
              to={
                `/manager/${self.state.manager.props.match.params.organization}/${self.state.manager.props.match.params.case}/` +
                encodeURIComponent(node.fullPath)}
              onClick={() => {
                (self.state.manager as ManagerPage).props.match.params.path = node.fullPath;
                (self.state.manager as ManagerPage).fetchDirectory();
              }}
            >
              {top ? 'Case Files' : node.name}
            </Link>
          ) : top ? (
            'Case Files'
          ) : (
            node.name
          )
        }
        key={node.fullPath}
      >
        {node.paths &&
          node.paths.map((x: any) => {
            return self.mapTreeNodeToComponent(x);
          })}
      </TreeNode>
    );
  }

  render() {
    var self = this;
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
