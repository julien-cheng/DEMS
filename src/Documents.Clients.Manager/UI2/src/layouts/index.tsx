import React from 'react';
import styles from './index.css';
import { Layout, Menu, Icon } from 'antd';
import Link from 'umi/link';

const BasicLayout: React.FC = props => {
  return (
    <Layout>
      <Layout.Header>
        {' '}
        <Menu theme="dark" mode="horizontal" style={{ lineHeight: '64px' }}>
          <Menu.Item key="home">
            <Link to="/">
              <Icon type="home"></Icon>Home
            </Link>
          </Menu.Item>
          <Menu.Item key="cases">
            <Link to="/case-list">
              <Icon type="container"></Icon>Cases
            </Link>
          </Menu.Item>
        </Menu>
      </Layout.Header>
      <Layout.Content>{props.children}</Layout.Content>
      <Layout.Footer></Layout.Footer>
    </Layout>
  );
};

export default BasicLayout;
