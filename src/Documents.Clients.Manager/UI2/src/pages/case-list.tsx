import React from 'react';

import styles from './case-list.css';
import CaseList from '@/components/CaseList';
import { FolderService } from '@/services/folder.service';
import { Table, Popconfirm, Form, Modal, Input, Radio, Button } from 'antd';
const CaseCreateForm = Form.create({ name: 'case_form_modal' })(
  // eslint-disable-next-line
  class extends React.Component {
    render() {
      const { visible, onCancel, onCreate, form } = this.props as any;
      const { getFieldDecorator } = form;
      return (
        <Modal
          visible={visible}
          title="Create a new case"
          okText="Create"
          onCancel={onCancel}
          onOk={onCreate}
        >
          <Form layout="vertical">
            <Form.Item label="Case Identifier">
              {getFieldDecorator('caseid', {
                rules: [{ required: true, message: 'Please input a unique case identifier!' }],
              })(<Input />)}
            </Form.Item>
          </Form>
        </Modal>
      );
    }
  },
);
export default class CaseListPage extends React.Component {
  formRef: any;
  constructor(props: any) {
    super(props);
    this.state = {
      cases: [],
      createModalVisible: false,
    };
  }
  showModal = () => {
    this.setState({ createModalVisible: true });
  };

  handleCancel = () => {
    this.setState({ createModalVisible: false });
  };

  handleCreate = () => {
    var me = this;
    const { form } = this.formRef.props;
    form.validateFields((err: any, values: any) => {
      if (err) {
        return;
      }

      new FolderService()
        .createNewCase({
          type: 'ManagerFolderModel',
          name: 'Defendant:' + values.caseid,
          identifier: {
            folderKey: 'Defendant:' + values.caseid,
            organizationKey: this.props.match.params.id,
          },
          fields: {
            docketNumber: null,
            arrestNumber: null,
            trialNumber: null,
            icmsNumber: null,
            indictmentNumber: null,
          },
        })
        .finally(() => {
          me.fetchCases();
        });
      form.resetFields();
      this.setState({ createModalVisible: false });
    });
  };

  saveFormRef = (formRef: any) => {
    this.formRef = formRef;
  };
  fetchCases() {
    var me = this;
    new FolderService().getAllFolders().then(value => {
      me.setState({
        cases: value.data.response.filter(
          (x: any) =>
            x.identifier.organizationKey == this.props.match.params.id ||
            !this.props.match.params.id,
        ),
      });
    });
  }
  componentDidMount() {
    this.fetchCases();
  }

  render() {
    return (
      <div className={styles.normal}>
        <CaseList cases={(this.state as any).cases} />
        {this.props.match.params.id && (
          <div style={{ marginTop: -48 }}>
            <Button type="primary" onClick={this.showModal}>
              Create Case
            </Button>
            <CaseCreateForm
              wrappedComponentRef={this.saveFormRef}
              visible={(this.state as any).createModalVisible}
              onCancel={this.handleCancel}
              onCreate={this.handleCreate}
            />
          </div>
        )}
      </div>
    );
  }
}
