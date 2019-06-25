import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaSubtitlesComponent } from './media-subtitles.component';

describe('MediaSubtitlesComponent', () => {
  let component: MediaSubtitlesComponent;
  let fixture: ComponentFixture<MediaSubtitlesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MediaSubtitlesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MediaSubtitlesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
