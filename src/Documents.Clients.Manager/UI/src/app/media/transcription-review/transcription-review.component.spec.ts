import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TranscriptionReviewComponent } from './transcription-review.component';

describe('TranscriptionReviewComponent', () => {
  let component: TranscriptionReviewComponent;
  let fixture: ComponentFixture<TranscriptionReviewComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TranscriptionReviewComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TranscriptionReviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
