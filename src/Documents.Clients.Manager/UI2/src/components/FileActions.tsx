import { Popconfirm, Button, Popover } from 'antd';
import React from 'react';
import { FolderService } from '@/services/folder.service';
import Link from 'umi/link';
import { Row, Col, Tree, Table } from 'antd';
import { PathService } from '@/services/path.service';
import ManagerPage from '@/pages/manager';
import FilePage from '@/pages/file';
import { IFile } from '@/interfaces/file.model';
import { IAllowedOperation } from '@/interfaces/allowed-operation.model';
const { TreeNode, DirectoryTree } = Tree;

class FileActions extends React.Component<
  { file: IFile; allowedOperations: IAllowedOperation[] },
  { file: IFile; allowedOperations: IAllowedOperation[] }
> {
  constructor(props: any) {
    super(props);
    // console.log("kdiusgfygfyd",props);
    this.state = {
      file: props.file,
      allowedOperations: props.allowedOperations,
    };
  }

  render() {
    var self = this;
    return (
      <div>
        {self.state.allowedOperations.map((x: IAllowedOperation, i) => {
          if (x.isSingleton && x.batchOperation && x.batchOperation.type == 'DownloadRequest') {
            return (
              <Popover
                trigger="click"
                content={<span>not implemented</span>}
                style={{ marginRight: 12 }}
              >
                <Button
                  type="primary"
                  style={{
                    marginRight: 12,
                  }}
                >
                  {x.displayName}
                </Button>
              </Popover>
            );
          }

          return '';
        })}
      </div>
    );
  }
}

export default FileActions;
