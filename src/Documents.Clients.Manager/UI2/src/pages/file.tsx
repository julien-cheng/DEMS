import React from 'react';

import styles from './manager.css';
import { Row, Col, Tree, Table, Card, Breadcrumb } from 'antd';
import { PathService } from '@/services/path.service';
const { TreeNode, DirectoryTree } = Tree;
import { Upload, Icon, message } from 'antd';
import Link from 'umi/link';
import Axios from 'axios';
import httpAdapter from 'axios';
import { RcFile } from 'antd/lib/upload';
import { QuerystringPipe } from '@/pipes/querystring.pipe';
import CaseTree from '@/components/CaseTree';
import { IView, IGridView } from '@/interfaces/view.model';
import { IPath } from '@/interfaces/path.model';
import { IFile, IFileViewer } from '@/interfaces/file.model';
import { IFileIdentifier } from '@/interfaces/identifiers.model';
import Utils from '@/services/utils';
import { FileService } from '@/services/file.service';
import { ExplorerService } from '@/services/explorer.service';
import { IImageSet } from '@/interfaces/file-sets.model';

const { Dragger } = Upload;
function getFileContentURL(fileIdentifier: IFileIdentifier) {
  return `/api/file/contents?fileidentifier.organizationKey=${fileIdentifier.organizationKey}&fileidentifier.folderKey=${fileIdentifier.folderKey}&fileidentifier.fileKey=${fileIdentifier.fileKey}&open=true`;
}

export default class FilePage extends React.Component<
  { match: any },
  { pathTree?: IPath; views: IView[]; file?: IFile }
> {
  explorer: ExplorerService;
  renderView?: JSX.Element;
  constructor(props: any) {
    super(props);
    this.state = {
      views: [],
      pathTree: {
        name: '',
        identifier: {
          organizationKey: this.props.match ? this.props.match.params.organization : '',
          folderKey: this.props.match ? this.props.match.params.case : '',
          pathKey: this.props.match ? decodeURIComponent(this.props.match.params.path || '') : '',
        },
        type: 'ManagerPathModel',
      },
    };
    this.explorer = new ExplorerService();
  }
  onSelect = (keys: any, event: any) => {
    // console.log('Trigger Select', keys, event);
  };

  onExpand = (e: any) => {
    // console.log('Trigger Expand', e);
  };
  fetchViews() {
    var me = this;
    if (this.props) {
      new PathService()
        .getPathPage({
          organizationKey: this.props.match.params.organization,
          folderKey: this.props.match.params.case,
          pathKey: '',
        })
        .then(value => {
          // console.log(value.data.response);
          if (value.data.response) {
            this.setState({
              pathTree: value.data.response.pathTree,
              views: value.data.response.views,
            });
            // console.log("COOL",value.data.response.views,value.data.response.pathTree);

            var fis = new FileService(this.explorer);
            fis
              .fileGet(
                {
                  organizationKey: this.props.match.params.organization,
                  folderKey: this.props.match.params.case,
                  fileKey: this.props.match.params.file,
                },
                {
                  organizationKey: this.props.match.params.organization,
                  folderKey: this.props.match.params.case,
                  pathKey: this.props.match
                    ? decodeURIComponent(this.props.match.params.path || '')
                    : '',
                },
              )
              .then(valuef => {
                // console.log(valuef.data.response);
                if (valuef.data.response) {
                  var file = valuef.data.response as IFile;
                  this.setState({
                    file: file,
                  });

                  fis
                    .getFileMediaSet(
                      {
                        organizationKey: this.props.match.params.organization,
                        folderKey: this.props.match.params.case,
                        fileKey: this.props.match.params.file,
                      },
                      this.state.views[0].type,
                    )
                    .then(value => {
                      // console.log(value.data.response);
                      if (value.data.response as IImageSet) {
                        var imgs = value.data.response as IImageSet;
                        var imgUrl = getFileContentURL(imgs.rootFileIdentifier);
                        me.renderView = (
                          <Card
                            hoverable={true}
                            style={{ width: '100%' }}
                            cover={<img src={imgUrl}></img>}
                          ></Card>
                        );
                        me.forceUpdate();
                      }
                    });
                  // (this.pathTreeRef.current as CaseTree).forceUpdate();
                }
              });
          }
        });
    }
  }
  componentDidMount() {
    this.fetchViews();
  }
  componentWillReceiveProps() {
    this.fetchViews();
  }
  // mapTreeNodeToComponent(node: any, top: boolean = false): any {
  //   var self = this;
  //   // console.log(node.name, node.fullPath);
  //   return (
  //     <TreeNode
  //       title={
  //         <Link
  //           to={
  //             `/manager/${this.props.match.params.organization}/${this.props.match.params.case}/` +
  //             encodeURIComponent(node.fullPath)}
  //           onClick={() => {
  //             this.props.match.params.path = node.fullPath;
  //             self.fetchDirectory();
  //           }}
  //         >
  //           {top ? 'Case Files' : node.name}
  //         </Link>
  //       }
  //       key={node.fullPath}
  //     >
  //       {node.paths &&
  //         node.paths.map((x: any) => {
  //           return self.mapTreeNodeToComponent(x);
  //         })}
  //     </TreeNode>
  //   );
  // }
  fileGrid(view: any) {
    var self = this;
    // console.log(view.rows);
    var cms = [
      {
        keyName: 'name',
        label: 'Name',
        isSortable: true,
      },
      {
        keyName: 'modified',
        label: 'Date Modified',
        isSortable: true,
      },
      {
        keyName: 'viewerType',
        label: 'Type',
        isSortable: true,
      },
      {
        keyName: 'lengthForHumans',
        label: 'Size',
        isSortable: true,
      },
    ].map((x: any) => ({ title: x.label, dataIndex: x.keyName })); //view.gridColumns.map((x:any)=>({title:x.label,dataIndex:x.keyName}));
    return (
      <Table
        dataSource={view.rows.map((x: IFile | IPath) => {
          var r = {
            key: (x as IPath).type
              ? (x as IPath).identifier.pathKey
              : (x as IFile).identifier.fileKey,
          };
          for (var q of cms) {
            (r as any)[q.dataIndex] = (x as any)[q.dataIndex];
          }
          if ((x as any).type == 'ManagerPathModel') {
            var p = x as IPath;
            (r as any).name = (
              <Link
                to={Utils.urlFromPathIdentifier(p.identifier)}
                onClick={() => {
                  this.props.match.params.path = p.identifier.pathKey;
                  self.fetchViews();
                }}
              >
                <Icon type="folder"></Icon> {(r as any).name}
              </Link>
            );
          } else if ((x as any).type == 'ManagerFileModel') {
            var f = x as IFile;
            if ((x as any).icons) {
              (r as any).name = (
                <span>
                  <Icon type={Utils.mapFileIconToAnt((x as any).icons[0])}></Icon> {(r as any).name}
                </span>
              );
            }
            console.log(f.views as IFileViewer[]);
            (r as any).name = (
              <Link
                to={Utils.urlFromFileViewIdentifier(
                  f.identifier,
                  this.props.match.params.path,
                  (f.views as IFileViewer[])[0],
                )}
                onClick={() => {
                  console.log(f.views as IFileViewer[]);
                  // this.props.match.params.path = x.identifier.pathKey;
                  // self.fetchDirectory();
                }}
              >
                {(r as any).name}
              </Link>
            );
          }
          return r;
        })}
        columns={cms}
      />
    );
  }
  render() {
    var self = this;
    var path = this.props.match ? decodeURIComponent(this.props.match.params.path || '') : '';
    var pathList = path.split('/');
    if (path === '') {
      pathList = [];
    }
    return (
      <div className={styles.normal}>
        <Row>
          <Col span={4}>
            <CaseTree manager={this} pathTree={() => this.state.pathTree}></CaseTree>
          </Col>
          <Col span={20}>
            <div style={{ padding: 16, paddingBottom: 0 }}>
              <Breadcrumb>
                {/* <Breadcrumb.Item href="/case-list">
                <Icon type="home" />
              </Breadcrumb.Item> */}
                <Breadcrumb.Item>
                  <Link
                    to={Utils.urlFromPathIdentifier({
                      organizationKey: this.props.match.params.organization,
                      pathKey: '',
                      folderKey: this.props.match.params.case,
                    })}
                    onClick={() => {
                      this.props.match.params.path = '';
                    }}
                  >
                    <Icon type="user" />
                    <span> Case Root</span>
                  </Link>
                </Breadcrumb.Item>
                {pathList.map((x, i) => (
                  <Breadcrumb.Item key={i}>
                    <Link
                      to={Utils.urlFromPathIdentifier({
                        organizationKey: this.props.match.params.organization,
                        pathKey: pathList.slice(0, i + 1).join('/'),
                        folderKey: this.props.match.params.case,
                      })}
                    >
                      {x}
                    </Link>
                  </Breadcrumb.Item>
                ))}
                {this.state.file && (
                  <Breadcrumb.Item key={'f'}>{this.state.file.name}</Breadcrumb.Item>
                )}
              </Breadcrumb>

              <div style={{ paddingTop: 16 }}>{this.renderView}</div>
            </div>
          </Col>
        </Row>
      </div>
    );
  }
}
