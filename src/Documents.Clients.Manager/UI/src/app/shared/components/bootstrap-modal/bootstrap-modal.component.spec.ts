import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BootstrapModalComponent } from './bootstrap-modal.component';

describe('BootstrapModalComponent', () => {
  let component: BootstrapModalComponent;
  let fixture: ComponentFixture<BootstrapModalComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [BootstrapModalComponent]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BootstrapModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
