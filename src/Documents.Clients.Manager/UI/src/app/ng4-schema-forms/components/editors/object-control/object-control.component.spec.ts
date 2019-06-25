import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ObjectControlComponent } from './object-control.component';

describe('ObjectControlComponent', () => {
  let component: ObjectControlComponent;
  let fixture: ComponentFixture<ObjectControlComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ObjectControlComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ObjectControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
