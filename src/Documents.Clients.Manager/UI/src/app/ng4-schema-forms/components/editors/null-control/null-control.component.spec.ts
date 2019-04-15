import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NullControlComponent } from './null-control.component';

describe('NullControlComponent', () => {
  let component: NullControlComponent;
  let fixture: ComponentFixture<NullControlComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [NullControlComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NullControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
