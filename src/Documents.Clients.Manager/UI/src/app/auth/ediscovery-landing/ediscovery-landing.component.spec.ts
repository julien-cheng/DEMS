import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { EdiscoveryLandingComponent } from './ediscovery-landing.component';

describe('EdiscoveryLandingComponent', () => {
  let component: EdiscoveryLandingComponent;
  let fixture: ComponentFixture<EdiscoveryLandingComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ EdiscoveryLandingComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EdiscoveryLandingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
