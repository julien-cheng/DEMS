import React from 'react';

import styles from './manager.css';
import { Row, Col, Tree, Table, Breadcrumb } from 'antd';
import { PathService } from '@/services/path.service';
const { TreeNode, DirectoryTree } = Tree;
import { Upload, Icon, message } from 'antd';
import Link from 'umi/link';
import Axios from 'axios';
import httpAdapter from 'axios';
import { RcFile } from 'antd/lib/upload';
import { QuerystringPipe } from '@/pipes/querystring.pipe';
import CaseTree from '@/components/CaseTree';
import { IView } from '@/interfaces/view.model';
import { IPath } from '@/interfaces/path.model';
import { IFile, IFileViewer } from '@/interfaces/file.model';
import { IFileIdentifier } from '@/interfaces/identifiers.model';
import Utils from '@/services/utils';
import SplitPane from 'react-split-pane';
import ManagerBreadcrumbs from '@/components/ManagerBreadcrumbs';

const { Dragger } = Upload;

var props2 = {
  name: 'file',
  multiple: true,
  action: 'https://www.mocky.io/v2/5cc8019d300000980a055e76',
  onChange(info: any) {
    const { status } = info.file;
    if (status !== 'uploading') {
      console.log(info.file, info.fileList);
    }
    if (status === 'done') {
      message.success(`${info.file.name} file uploaded successfully.`);
    } else if (status === 'error') {
      message.error(`${info.file.name} file upload failed.`);
    }
  },
};
export default class ManagerPage extends React.Component<
  { match: any },
  { pathTree?: IPath; views: IView[] }
> {
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
  }
  onSelect = (keys: any, event: any) => {
    // console.log('Trigger Select', keys, event);
  };

  onExpand = (e: any) => {
    // console.log('Trigger Expand', e);
  };
  onClick(p: any) {
    this.props.match.params.path = p.identifier.pathKey;
    this.fetchDirectory();
  }
  fetchDirectory(path?: string) {
    if (path) {
      this.props.match.params.path = encodeURIComponent(this.props.match.params.path);
    }
    var me = this;
    if (this.props) {
      new PathService()
        .getPathPage({
          organizationKey: this.props.match.params.organization,
          folderKey: this.props.match.params.case,
          pathKey: decodeURIComponent(this.props.match.params.path || ''),
        })
        .then(value => {
          console.log(value.data.response);
          if (value.data.response) {
            // console.log("COOL",value.data.response.views,value.data.response.pathTree);
            this.setState({
              pathTree: value.data.response.pathTree,
              views: value.data.response.views,
            });
            // (this.pathTreeRef.current as CaseTree).forceUpdate();
          }
        });
    }
  }
  componentDidMount() {
    this.fetchDirectory();
  }
  componentWillReceiveProps() {
    this.fetchDirectory();
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
    let dataSource = view.rows.map((x: IFile | IPath) => {
      var r = {
        key: (x as IPath).type ? (x as IPath).identifier.pathKey : (x as IFile).identifier.fileKey,
      };
      for (var q of cms) {
        (r as any)[q.dataIndex] = (x as any)[q.dataIndex];
      }
      if ((x as any).type == 'ManagerPathModel') {
        var p = x as IPath;
        (r as any).name = (
          <Link to={Utils.urlFromPathIdentifier(p.identifier)} onClick={this.onClick.bind(p)}>
            <Icon type="folder" /> {(r as any).name}
          </Link>
        );
      } else if ((x as any).type == 'ManagerFileModel') {
        var f = x as IFile;
        if ((x as any).icons) {
          (r as any).name = (
            <span>
              <Icon type={Utils.mapFileIconToAnt((x as any).icons[0])} /> {(r as any).name}
            </span>
          );
        }
        // console.log(f.views as IFileViewer[]);
        let path = Utils.urlFromFileViewIdentifier(
          f.identifier,
          decodeURIComponent(this.props.match.params.path || ''),
          (f.views as IFileViewer[])[0],
        );
        (r as any).name = <Link to={path}>{(r as any).name}</Link>;
      }
      return r;
    });
    return <Table dataSource={dataSource} columns={cms} />;
  }
  render() {
    var self = this;
    var gridView = this.state.views.filter(x => x.type == 'Grid')[0];
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
            <ManagerBreadcrumbs
              manager={this}
              organization={() => this.props.match.params.organization || ''}
              path={() => this.props.match.params.path || ''}
              case={() => this.props.match.params.case || ''}
            />

            <div style={{ paddingTop: 16 }}>{gridView && this.fileGrid(gridView)}</div>
          </div>
          <div style={{ padding: 16, paddingTop: 0 }}>
            {this.props.match && (
              <Dragger
                {...props2}
                action={(file: RcFile) => {
                  var headers = {};
                  (headers as any)['Content-Disposition'] =
                    'attachment; filename="' + file.name + '"';
                  (headers as any)['Content-Range'] =
                    'bytes 0-' + (file.size - 1) + '/' + file.size;

                  return Axios.post(
                    '/api/upload?pathIdentifier.organizationKey=' +
                      this.props.match.params.organization +
                      '&pathIdentifier.folderKey=' +
                      this.props.match.params.case +
                      (this.props.match.params.path
                        ? '&pathIdentifier.pathKey=' + this.props.match.params.path
                        : '') +
                      ['lastModified', 'lastModifiedDate', 'name', 'size', 'type']
                        .map((k: string) => '&fileInformation.' + k + '=' + (file as any)[k])
                        .join(''),
                    file,
                    { headers: headers },
                  )
                    .finally(() => {
                      self.fetchDirectory();
                    })
                    .then(x => '' + x);
                }}
              >
                <p className="ant-upload-drag-icon">
                  <Icon type="inbox" />
                </p>
                <p className="ant-upload-text">Click or drag file to this area to upload</p>
                <p className="ant-upload-hint">Support for a single or bulk upload.</p>
              </Dragger>
            )}
          </div>
        </div>
      </SplitPane>
      // </div>
    );
  }
}
