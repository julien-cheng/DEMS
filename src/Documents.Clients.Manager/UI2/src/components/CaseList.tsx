import { Table, Popconfirm, Button } from 'antd';
import React from 'react';
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
          <Popconfirm title="Delete?" onConfirm={() => onDelete(record.id)}>
            <Button type="danger">Delete</Button>
          </Popconfirm>
        );
      },
    },
  ];
  return (
    <Table
      dataSource={cases.map((x: any) => ({
        caseid: x.identifier.folderKey,
        first: null,
        last: null,
      }))}
      columns={columns}
    />
  );
};

export default CaseList;
