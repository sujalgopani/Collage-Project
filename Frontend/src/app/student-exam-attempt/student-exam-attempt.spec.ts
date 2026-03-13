import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StudentExamAttempt } from './student-exam-attempt';

describe('StudentExamAttempt', () => {
  let component: StudentExamAttempt;
  let fixture: ComponentFixture<StudentExamAttempt>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StudentExamAttempt]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StudentExamAttempt);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
