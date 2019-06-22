
import React from 'react';

import styles from './case-list.css';
import CaseList from '@/components/CaseList';

export default function() {
  return (
    <div className={styles.normal}>
      <CaseList cases={[{first:"Billy",last:"Bob",casenum:"Placeholder"}]}></CaseList>
    </div>
  );
}
