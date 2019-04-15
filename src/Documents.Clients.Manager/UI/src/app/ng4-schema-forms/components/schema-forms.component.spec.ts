import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SchemaFormsComponent } from './schema-forms.component';

describe('SchemaFormsComponent', () => {
  let component: SchemaFormsComponent;
  let fixture: ComponentFixture<SchemaFormsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [SchemaFormsComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SchemaFormsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
