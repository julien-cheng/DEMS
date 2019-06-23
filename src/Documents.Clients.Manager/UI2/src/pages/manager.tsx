import React from 'react';

import styles from './manager.css';
import { Row, Col, Tree } from 'antd';
const { TreeNode, DirectoryTree } = Tree;
export default class ManagerPage extends React.Component {
  constructor(props: any) {
    super(props);
    // this.state = {
    //   cases: [],
    //   createModalVisible: false,
    // };
  }
  onSelect = (keys: any, event: any) => {
    console.log('Trigger Select', keys, event);
  };

  onExpand = () => {
    console.log('Trigger Expand');
  };

  render() {
    return (
      <div className={styles.normal}>
        <Row>
          <Col span={6}>
            <DirectoryTree
              multiple={true}
              defaultExpandAll={true}
              onSelect={this.onSelect}
              onExpand={this.onExpand}
            >
              <TreeNode title="parent 0" key="0-0">
                <TreeNode title="leaf 0-0" key="0-0-0" isLeaf={true} />
                <TreeNode title="leaf 0-1" key="0-0-1" isLeaf={true} />
              </TreeNode>
              <TreeNode title="parent 1" key="0-1">
                <TreeNode title="leaf 1-0" key="0-1-0" isLeaf={true} />
                <TreeNode title="leaf 1-1" key="0-1-1" isLeaf={true} />
              </TreeNode>
            </DirectoryTree>
          </Col>
          <Col span={18}>col-18 col-push-6</Col>
        </Row>
      </div>
    );
  }
}
