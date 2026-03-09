import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Owncourse } from './owncourse';

describe('Owncourse', () => {
  let component: Owncourse;
  let fixture: ComponentFixture<Owncourse>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Owncourse]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Owncourse);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
