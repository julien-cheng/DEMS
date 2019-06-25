import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FileLinkComponent } from './file-links.component';

describe('FileLinkComponent', () => {
  let component: FileLinkComponent;
  let fixture: ComponentFixture<FileLinkComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FileLinkComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FileLinkComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
