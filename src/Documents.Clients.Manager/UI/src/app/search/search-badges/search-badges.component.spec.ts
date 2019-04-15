import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SearchBadgesComponent } from './search-badges.component';

describe('SearchBadgesComponent', () => {
  let component: SearchBadgesComponent;
  let fixture: ComponentFixture<SearchBadgesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [SearchBadgesComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SearchBadgesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
