import React from 'react';
import Link from 'umi/link';
import { Tree, Breadcrumb, Icon } from 'antd';
import ManagerPage from '@/pages/manager';
import FilePage from '@/pages/file';
import Utils from '@/services/utils';
const { TreeNode, DirectoryTree } = Tree;

class ManagerBreadcrumbs extends React.Component<
  {
    organization: () => string;
    path: () => string;
    case: () => string;
    manager: ManagerPage | FilePage;
    fileName?: () => string | undefined;
  },
  {
    organization: () => string;
    path: () => string;
    case: () => string;
    manager: ManagerPage | FilePage;
    fileName?: () => string | undefined;
  }
> {
  constructor(props: any) {
    super(props);
    this.state = {
      manager: props.manager,
      organization: props.organization,
      path: props.path,
      case: props.case,
      fileName: props.fileName,
    };
  }
  onSelect() {}

  onExpand() {}

  titleOnClick(node: any) {
    (this.state.manager as ManagerPage).props.match.params.path = node.fullPath;
    (this.state.manager as ManagerPage).fetchDirectory();
  }
  title(node: any) {
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
      <TreeNode title={this.title(node)} key={node.fullPath}>
        {items}
      </TreeNode>
    );
  }

  render() {
    let caseRoot = Utils.urlFromPathIdentifier({
      organizationKey: this.state.organization(),
      pathKey: '',
      folderKey: this.state.case(),
    });
    let pathList = decodeURIComponent(this.state.path()).split('/');
    let breadcrumbItems = pathList.map((x, i) => (
      <Breadcrumb.Item key={i}>
        <Link
          to={Utils.urlFromPathIdentifier({
            organizationKey: this.props.organization(),
            pathKey: pathList.slice(0, i + 1).join('/'),
            folderKey: this.props.case(),
          })}
          onClick={() => {
            if (this.state.manager instanceof ManagerPage)
              this.state.manager.fetchDirectory(pathList.slice(0, i + 1).join('/'));
          }}
        >
          {x}
        </Link>
      </Breadcrumb.Item>
    ));
    return (
      <Breadcrumb>
        <Breadcrumb.Item>
          <Link
            to={caseRoot}
            onClick={() => {
              if (this.state.manager instanceof ManagerPage) this.state.manager.fetchDirectory('');
            }}
          >
            <Icon type="user" />
            <span> Case Root</span>
          </Link>
        </Breadcrumb.Item>
        {breadcrumbItems}
        {this.state.fileName && this.state.fileName() && (
          <Breadcrumb.Item>
            <Link to={'#'}>
              <span>{this.state.fileName()}</span>
            </Link>
          </Breadcrumb.Item>
        )}
      </Breadcrumb>
    );
  }
}

export default ManagerBreadcrumbs;
