import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Learningcourse } from './learningcourse';

describe('Learningcourse', () => {
  let component: Learningcourse;
  let fixture: ComponentFixture<Learningcourse>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Learningcourse]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Learningcourse);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
