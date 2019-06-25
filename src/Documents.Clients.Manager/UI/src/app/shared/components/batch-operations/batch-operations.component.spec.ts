import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BatchOperationsComponent } from './batch-operations.component';

describe('BatchOperationsComponent', () => {
  let component: BatchOperationsComponent;
  let fixture: ComponentFixture<BatchOperationsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BatchOperationsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BatchOperationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
