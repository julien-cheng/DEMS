import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { VideoBasicComponent } from './video-basic.component';

describe('VideoBasicComponent', () => {
  let component: VideoBasicComponent;
  let fixture: ComponentFixture<VideoBasicComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [VideoBasicComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(VideoBasicComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
