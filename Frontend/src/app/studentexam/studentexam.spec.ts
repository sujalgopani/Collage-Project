import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Studentexam } from './studentexam';

describe('Studentexam', () => {
  let component: Studentexam;
  let fixture: ComponentFixture<Studentexam>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Studentexam]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Studentexam);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
