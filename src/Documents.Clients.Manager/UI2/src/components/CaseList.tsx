import { Table, Popconfirm, Button } from 'antd';
import React from "react";
const CaseList = ({ onDelete, cases }:any) => {
  const columns = [
    {
      title: 'Case ID',
      dataIndex: 'casenum',
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
      render: (text:any, record:any) => {
        return (
          <Popconfirm title="Delete?" onConfirm={() => onDelete(record.id)}>
            <Button>Delete</Button>
          </Popconfirm>
        );
      },
    },
  ];
  return <Table dataSource={cases} columns={columns} />;
};

export default CaseList;
