import { TestBed, inject } from '@angular/core/testing';

import { MediaToolsService } from './media-tools.service';

describe('MediaToolsService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [MediaToolsService]
    });
  });

  it('should be created', inject([MediaToolsService], (service: MediaToolsService) => {
    expect(service).toBeTruthy();
  }));
});
