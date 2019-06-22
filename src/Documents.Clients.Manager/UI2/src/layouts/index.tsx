import React from 'react';
import styles from './index.css';
import { Layout, Menu,Icon } from 'antd';

const BasicLayout: React.FC = props => {
  return (
    <Layout>
      <Layout.Header>  <Menu
        theme="dark"
        mode="horizontal"
        style={{ lineHeight: '64px' }}
      >
        <Menu.Item key="home"><Icon type="home"></Icon>Home</Menu.Item>
        <Menu.Item key="cases"><Icon type="container"></Icon>Cases</Menu.Item>
      </Menu></Layout.Header>
      <Layout.Content>{props.children}</Layout.Content>
      <Layout.Footer></Layout.Footer>
      
    </Layout>
  );
};

export default BasicLayout;
