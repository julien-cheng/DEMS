import React from 'react';

import styles from './case-list.css';
import CaseList from '@/components/CaseList';
import { FolderService } from '@/services/folder.service';
import { Button } from 'antd';

export default class CaseListPage extends React.Component {
  constructor(props: any) {
    super(props);
    this.state = {
      cases: [],
    };
  }

  componentDidMount() {
    var me = this;
    new FolderService().getAllFolders().then(value => {
      me.setState({ cases: value.data.response });
    });
  }

  render() {
    return (
      <div className={styles.normal}>
        <CaseList cases={this.state.cases}></CaseList>
        <div style={{ marginTop: -48 }}>
          <Button type="primary" onClick={}>
            Create Case
          </Button>
        </div>
      </div>
    );
  }
}
