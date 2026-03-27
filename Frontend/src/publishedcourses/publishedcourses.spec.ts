import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Publishedcourses } from './publishedcourses';

describe('Publishedcourses', () => {
  let component: Publishedcourses;
  let fixture: ComponentFixture<Publishedcourses>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Publishedcourses]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Publishedcourses);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
