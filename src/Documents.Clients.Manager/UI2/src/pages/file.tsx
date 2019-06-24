import React from 'react';

import styles from './manager.css';
import { Row, Col, Tree, Table, Card, Breadcrumb, Button, Popover } from 'antd';
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
import { IImageSet, IMediaSet } from '@/interfaces/file-sets.model';
import { IAllowedOperation } from '@/interfaces/allowed-operation.model';
import SplitPane from 'react-split-pane';
import FileActions from '@/components/FileActions';
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
                    .then(values => {
                      console.log(values.data.response);
                      if (values.data.response && values.data.response.views) {
                        if (
                          values.data.response.views[0] &&
                          values.data.response.views[0].viewerType == 'Image'
                        ) {
                          var imgs = values.data.response as IImageSet;
                          var imgUrl = getFileContentURL(imgs.rootFileIdentifier);
                          if (imgs.allowedOperations) {
                            console.log('allowedOPS:', imgs.allowedOperations);
                          }
                          me.renderView = (
                            <Card
                              hoverable={true}
                              style={{
                                maxWidth: 'calc( 100% - 32px )',
                                display: 'inline-block',
                                transform: 'translate(0%,-0%)',
                                position: 'absolute',
                                left: '16px',
                              }}
                              cover={
                                <img
                                  src={imgUrl}
                                  style={{
                                    maxHeight: 'calc( 100vh - 216px )',
                                    maxWidth: '100%',
                                    width: 'auto',
                                  }}
                                />
                              }
                            >
                              {imgs.allowedOperations && (
                                <FileActions
                                  file={file}
                                  allowedOperations={imgs.allowedOperations}
                                />
                              )}
                              <br />
                              {/* <p style={{maxWidth:"200px"}}>{JSON.stringify(imgs.allowedOperations)}</p> */}
                            </Card>
                          );
                          me.forceUpdate();
                        } else if (
                          values.data.response.views[0] &&
                          values.data.response.views[0].viewerType == 'Video'
                        ) {
                          var vds = values.data.response as IMediaSet;
                          var imgUrl = getFileContentURL(vds.rootFileIdentifier);
                          if (vds.allowedOperations) {
                            console.log('allowedOPS:', vds.allowedOperations);
                          }
                          me.renderView = (
                            <Card
                              hoverable={true}
                              style={{
                                maxWidth: 'calc( 100% - 32px )',
                                display: 'inline-block',
                                transform: 'translate(0%,-0%)',
                                position: 'absolute',
                                left: '16px',
                              }}
                              cover={
                                <video
                                  controls={true}
                                  src={imgUrl}
                                  style={{
                                    maxHeight: 'calc( 100vh - 216px )',
                                    maxWidth: '100%',
                                    width: 'auto',
                                  }}
                                />
                              }
                            >
                              {vds.allowedOperations && (
                                <FileActions
                                  file={file}
                                  allowedOperations={vds.allowedOperations}
                                />
                              )}
                              {/* <p style={{maxWidth:"200px"}}>{JSON.stringify(imgs.allowedOperations)}</p> */}
                            </Card>
                          );
                          me.forceUpdate();
                        }
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
  render() {
    var self = this;
    var path = this.props.match ? decodeURIComponent(this.props.match.params.path || '') : '';
    var pathList = path.split('/');
    if (path === '') {
      pathList = [];
    }
    return (
      // <div className={styles.normal}>
      <SplitPane
        split="vertical"
        minSize={200}
        defaultSize={300}
        resizerClassName={styles.Resizer + ' ' + styles.vertical}
        style={{
          left: 0,
          right: 0,
          bottom: 0,
          top: 0,
        }}
      >
        <div>
          <CaseTree manager={this} pathTree={() => this.state.pathTree} />
        </div>
        <div>
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

            <div style={{ paddingTop: 16, height: '100%' }}>{this.renderView}</div>
          </div>
        </div>
      </SplitPane>
      // </div>
    );
  }
}
