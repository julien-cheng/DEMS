import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LeoLandingComponent } from './leo-landing.component';

describe('LeoLandingComponent', () => {
  let component: LeoLandingComponent;
  let fixture: ComponentFixture<LeoLandingComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [LeoLandingComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LeoLandingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
