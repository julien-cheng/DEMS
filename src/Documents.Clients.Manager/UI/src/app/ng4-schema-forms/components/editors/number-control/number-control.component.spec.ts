import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NumberControlComponent } from './number-control.component';

describe('NumberControlComponent', () => {
  let component: NumberControlComponent;
  let fixture: ComponentFixture<NumberControlComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NumberControlComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NumberControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
