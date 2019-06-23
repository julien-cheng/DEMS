import React from 'react';

import styles from './manager.css';
import { Row, Col, Tree, Table } from 'antd';
import { PathService } from '@/services/path.service';
const { TreeNode, DirectoryTree } = Tree;
import { Upload, Icon, message } from 'antd';
import Link from 'umi/link';
import Axios from 'axios';
import httpAdapter from 'axios';
import { RcFile } from 'antd/lib/upload';
import { QuerystringPipe } from '@/pipes/querystring.pipe';

const { Dragger } = Upload;

var props = {
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
function mapFileIconToAnt(icon: string): string {
  if (icon == 'file') {
    return 'file';
  } else if (icon == 'video') {
    return 'video-camera';
  } else {
    return 'file-unknown';
  }
}
function urlFromFolderIdentifier(identifier: any) {
  return (
    `/manager/${identifier.organizationKey}/${identifier.folderKey}/` +
    encodeURIComponent(identifier.pathKey)
  );
}
export default class ManagerPage extends React.Component {
  constructor(props: any) {
    super(props);
    this.state = {
      pathTree: null,
      views: [],
    };
  }
  onSelect = (keys: any, event: any) => {
    console.log('Trigger Select', keys, event);
  };

  onExpand = (e: any) => {
    console.log('Trigger Expand', e);
  };
  fetchDirectory() {
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
          console.log(value.data.response.views);
          this.setState({
            pathTree: value.data.response.pathTree,
            views: value.data.response.views,
          });
        });
    }
  }
  componentDidMount() {
    this.fetchDirectory();
  }
  componentWillReceiveProps() {
    this.fetchDirectory();
  }
  mapTreeNodeToComponent(node: any, top: bool = false): any {
    var self = this;
    // console.log(node.name, node.fullPath);
    return (
      <TreeNode
        title={
          <Link
            to={
              `/manager/${this.props.match.params.organization}/${this.props.match.params.case}/` +
              encodeURIComponent(node.fullPath)}
            onClick={() => {
              this.props.match.params.path = node.fullPath;
              self.fetchDirectory();
            }}
          >
            {top ? 'Case Files' : node.name}
          </Link>
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
  fileGrid(view: any) {
    var self = this;
    console.log(view.rows);
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
        dataSource={view.rows.map((x: any) => {
          var r = { key: x.identifier.fileKey };
          for (var q of cms) {
            (r as any)[q.dataIndex] = (x as any)[q.dataIndex];
          }
          if ((x as any).type == 'ManagerPathModel') {
            (r as any).name = (
              <Link
                to={urlFromFolderIdentifier(x.identifier)}
                onClick={() => {
                  this.props.match.params.path = x.identifier.pathKey;
                  self.fetchDirectory();
                }}
              >
                <Icon type="folder"></Icon> {(r as any).name}
              </Link>
            );
          } else if ((x as any).type == 'ManagerFileModel') {
            if ((x as any).icons) {
              (r as any).name = (
                <div>
                  <Icon type={mapFileIconToAnt((x as any).icons[0])}></Icon> {(r as any).name}
                </div>
              );
            }
          }
          return r;
        })}
        columns={cms}
      />
    );
  }
  render() {
    var self = this;
    var gridView = this.state.views.filter(x => x.type == 'Grid')[0];
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
              {this.state.pathTree && this.mapTreeNodeToComponent(this.state.pathTree, true)}
            </DirectoryTree>
          </Col>
          <Col span={20}>
            <div style={{ padding: 16, paddingBottom: 0 }}>
              {gridView && this.fileGrid(gridView)}
            </div>
            <div style={{ padding: 16, paddingTop: 0 }}>
              {this.props.match && (
                <Dragger
                  {...props}
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
                    ).finally(() => {
                      self.fetchDirectory();
                    });
                  }}
                >
                  <p className="ant-upload-drag-icon">
                    <Icon type="inbox" />
                  </p>
                  <p className="ant-upload-text">Click or drag file to this area to upload</p>
                  <p className="ant-upload-hint">
                    Support for a single or bulk upload. Strictly prohibit from uploading company
                    data or other band files
                  </p>
                </Dragger>
              )}
            </div>
          </Col>
        </Row>
      </div>
    );
  }
}
