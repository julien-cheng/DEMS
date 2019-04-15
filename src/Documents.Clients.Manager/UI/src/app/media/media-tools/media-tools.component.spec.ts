import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaToolsComponent } from './media-tools.component';

describe('MediaToolsComponent', () => {
  let component: MediaToolsComponent;
  let fixture: ComponentFixture<MediaToolsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [MediaToolsComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MediaToolsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
