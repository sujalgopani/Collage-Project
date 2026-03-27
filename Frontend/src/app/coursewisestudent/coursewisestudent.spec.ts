import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Coursewisestudent } from './coursewisestudent';

describe('Coursewisestudent', () => {
  let component: Coursewisestudent;
  let fixture: ComponentFixture<Coursewisestudent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Coursewisestudent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Coursewisestudent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
