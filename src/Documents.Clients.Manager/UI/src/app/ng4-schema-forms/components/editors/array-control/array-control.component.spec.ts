import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ArrayControlComponent } from './array-control.component';

describe('ArrayControlComponent', () => {
  let component: ArrayControlComponent;
  let fixture: ComponentFixture<ArrayControlComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ArrayControlComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ArrayControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
