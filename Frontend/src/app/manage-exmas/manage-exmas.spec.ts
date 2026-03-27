import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManageExmas } from './manage-exmas';

describe('ManageExmas', () => {
  let component: ManageExmas;
  let fixture: ComponentFixture<ManageExmas>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageExmas]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ManageExmas);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
