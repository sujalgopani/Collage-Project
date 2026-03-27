import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManagePayment } from './manage-payment';

describe('ManagePayment', () => {
  let component: ManagePayment;
  let fixture: ComponentFixture<ManagePayment>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManagePayment]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ManagePayment);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
