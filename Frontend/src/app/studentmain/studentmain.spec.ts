import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Studentmain } from './studentmain';

describe('Studentmain', () => {
  let component: Studentmain;
  let fixture: ComponentFixture<Studentmain>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Studentmain]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Studentmain);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
