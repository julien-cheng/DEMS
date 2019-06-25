import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FileAutodownloadComponent } from './file-autodownload.component';

describe('FileAutodownloadComponent', () => {
  let component: FileAutodownloadComponent;
  let fixture: ComponentFixture<FileAutodownloadComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FileAutodownloadComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FileAutodownloadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
