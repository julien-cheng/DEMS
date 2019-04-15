import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaClipsComponent } from './media-clips.component';

describe('MediaClipsComponent', () => {
  let component: MediaClipsComponent;
  let fixture: ComponentFixture<MediaClipsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [MediaClipsComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MediaClipsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
