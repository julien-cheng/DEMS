import { TestBed, inject } from '@angular/core/testing';

import { SearchResolverService } from './search-resolver.service';

describe('SearchResolverService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SearchResolverService]
    });
  });

  it('should be created', inject([SearchResolverService], (service: SearchResolverService) => {
    expect(service).toBeTruthy();
  }));
});
