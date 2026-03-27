import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Teachergetsuggestion } from './teachergetsuggestion';

describe('Teachergetsuggestion', () => {
  let component: Teachergetsuggestion;
  let fixture: ComponentFixture<Teachergetsuggestion>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Teachergetsuggestion]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Teachergetsuggestion);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
