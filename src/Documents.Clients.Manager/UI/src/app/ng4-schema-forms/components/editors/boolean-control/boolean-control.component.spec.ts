import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BooleanControlComponent } from './boolean-control.component';

describe('BooleanControlComponent', () => {
  let component: BooleanControlComponent;
  let fixture: ComponentFixture<BooleanControlComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [BooleanControlComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BooleanControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
