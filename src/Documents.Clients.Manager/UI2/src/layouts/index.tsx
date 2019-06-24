import React from 'react';
import './index.css';
import { Layout, Menu, Icon } from 'antd';
import Link from 'umi/link';

const BasicLayout: React.FC = props => {
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Layout.Header>
        {' '}
        <Menu theme="dark" mode="horizontal" style={{ lineHeight: '64px' }}>
          <Menu.Item key="home">
            <Link to="/">
              <Icon type="home" />
              Home
            </Link>
          </Menu.Item>
          <Menu.Item key="cases">
            <Link to="/case-list">
              <Icon type="container" />
              Cases
            </Link>
          </Menu.Item>
        </Menu>
      </Layout.Header>
      <Layout.Content style={{ height: 'fill-available', position: 'relative' }}>
        {props.children}
      </Layout.Content>
      {/* <Layout.Footer></Layout.Footer> */}
    </Layout>
  );
};

export default BasicLayout;
