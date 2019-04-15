import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaTimelineComponent } from './media-timeline.component';

describe('MediaTimelineComponent', () => {
  let component: MediaTimelineComponent;
  let fixture: ComponentFixture<MediaTimelineComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [MediaTimelineComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MediaTimelineComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
