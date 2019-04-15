import { TestBed, inject } from '@angular/core/testing';

import { SearchResolver } from './search-resolver.service';

describe('SearchResolverService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SearchResolver]
    });
  });

  it('should be created', inject([SearchResolver], (service: SearchResolver) => {
    expect(service).toBeTruthy();
  }));
});
