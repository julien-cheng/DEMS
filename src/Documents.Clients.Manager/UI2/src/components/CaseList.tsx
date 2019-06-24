import { Table, Popconfirm, Button } from 'antd';
import React from 'react';
import { FolderService } from '@/services/folder.service';
import Link from 'umi/link';

const CaseList = ({ onDelete, cases }: any) => {
  const columns = [
    {
      title: 'Case ID',
      dataIndex: 'caseid',
    },
    {
      title: 'First Name',
      dataIndex: 'first',
    },
    {
      title: 'Last Name',
      dataIndex: 'last',
    },
    {
      title: 'Actions',
      render: (text: any, record: any) => {
        return (
          <div>
            <Link
              to={
                '/manager/' +
                record.entry.identifier.organizationKey +
                '/' +
                record.entry.identifier.folderKey
              }
              style={{ marginRight: 12 }}
            >
              <Button type="primary">View</Button>
            </Link>
            <Popconfirm
              title="Delete?"
              onConfirm={() => new FolderService().deleteCase(record.entry.identifier)}
            >
              <Button type="danger">Delete</Button>
            </Popconfirm>
          </div>
        );
      },
    },
  ];
  return (
    <Table
      dataSource={cases.map((x: any) => ({
        key: x.identifier.folderKey,
        caseid: x.identifier.folderKey,
        first: null,
        last: null,
        entry: x,
      }))}
      columns={columns}
    />
  );
};

export default CaseList;
