import { TestBed, inject } from '@angular/core/testing';

import { BatchOperationService } from './batch-operation.service';

describe('BatchOperationService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [BatchOperationService]
    });
  });

  it('should be created', inject([BatchOperationService], (service: BatchOperationService) => {
    expect(service).toBeTruthy();
  }));
});
