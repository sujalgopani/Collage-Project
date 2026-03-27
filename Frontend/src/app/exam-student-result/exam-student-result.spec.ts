import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExamStudentResult } from './exam-student-result';

describe('ExamStudentResult', () => {
  let component: ExamStudentResult;
  let fixture: ComponentFixture<ExamStudentResult>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExamStudentResult]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ExamStudentResult);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
