import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SchemaFormControlComponent } from './schema-form-control.component';

describe('SchemaFormControlComponent', () => {
  let component: SchemaFormControlComponent;
  let fixture: ComponentFixture<SchemaFormControlComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SchemaFormControlComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SchemaFormControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
