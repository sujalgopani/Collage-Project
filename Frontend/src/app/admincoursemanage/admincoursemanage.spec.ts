import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Admincoursemanage } from './admincoursemanage';

describe('Admincoursemanage', () => {
  let component: Admincoursemanage;
  let fixture: ComponentFixture<Admincoursemanage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Admincoursemanage]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Admincoursemanage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
