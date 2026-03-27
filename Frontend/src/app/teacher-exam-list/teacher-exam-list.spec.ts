import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TeacherExamList } from './teacher-exam-list';

describe('TeacherExamList', () => {
  let component: TeacherExamList;
  let fixture: ComponentFixture<TeacherExamList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TeacherExamList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TeacherExamList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
