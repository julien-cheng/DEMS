import { TestBed, inject } from '@angular/core/testing';

import { MediaResolverService } from './media-resolver.service';

describe('MediaResolverService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [MediaResolverService]
    });
  });

  it('should be created', inject([MediaResolverService], (service: MediaResolverService) => {
    expect(service).toBeTruthy();
  }));
});
