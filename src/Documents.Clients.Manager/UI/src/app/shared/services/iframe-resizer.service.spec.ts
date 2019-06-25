import { TestBed, inject } from '@angular/core/testing';

import { IframeResizerService } from './iframe-resizer.service';

describe('IframeResizerService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [IframeResizerService]
    });
  });

  it('should be created', inject([IframeResizerService], (service: IframeResizerService) => {
    expect(service).toBeTruthy();
  }));
});
