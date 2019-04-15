import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FileviewsMenuComponent } from './fileviews-menu.component';

describe('FileviewsMenuComponent', () => {
  let component: FileviewsMenuComponent;
  let fixture: ComponentFixture<FileviewsMenuComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [FileviewsMenuComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FileviewsMenuComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
