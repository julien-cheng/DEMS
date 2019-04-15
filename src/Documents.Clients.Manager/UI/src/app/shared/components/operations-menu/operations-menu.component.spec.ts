import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { OperationsMenuComponent } from './operations-menu.component';

describe('OperationsMenuComponent', () => {
  let component: OperationsMenuComponent;
  let fixture: ComponentFixture<OperationsMenuComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [OperationsMenuComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OperationsMenuComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
