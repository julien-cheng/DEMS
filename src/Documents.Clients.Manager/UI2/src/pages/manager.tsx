import React from 'react';

import styles from './manager.css';
import { Row, Col, Tree } from 'antd';
import { PathService } from '@/services/path.service';
const { TreeNode, DirectoryTree } = Tree;
export default class ManagerPage extends React.Component {
  constructor(props: any) {
    super(props);
    this.state = {
      pathTree: null,
    };
  }
  onSelect = (keys: any, event: any) => {
    console.log('Trigger Select', keys, event);
  };

  onExpand = () => {
    console.log('Trigger Expand');
  };
  fetchDirectory() {
    var me = this;
    new PathService()
      .getPathPage({
        organizationKey: this.props.match.params.organization,
        folderKey: this.props.match.params.case,
        pathKey: '',
      })
      .then(value => {
        console.log(value.data.response);
        this.setState({ pathTree: value.data.response.pathTree });
      });
  }
  componentDidMount() {
    this.fetchDirectory();
  }
  mapTreeNodeToComponent(node: any): any {
    var self = this;
    console.log(node.name, node.fullPath);
    return (
      <TreeNode title={node.name} key={node.fullPath}>
        {node.paths.map((x: any) => {
          return self.mapTreeNodeToComponent(x);
        })}
      </TreeNode>
    );
  }

  render() {
    return (
      <div className={styles.normal}>
        <Row>
          <Col span={4}>
            <DirectoryTree
              multiple={true}
              defaultExpandAll={true}
              onSelect={this.onSelect}
              onExpand={this.onExpand}
            >
              {this.state.pathTree && this.mapTreeNodeToComponent(this.state.pathTree)}
            </DirectoryTree>
          </Col>
          <Col span={20}>col-18 col-push-6</Col>
        </Row>
      </div>
    );
  }
}
