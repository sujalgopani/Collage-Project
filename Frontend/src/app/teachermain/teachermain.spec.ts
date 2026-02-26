import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Teachermain } from './teachermain';

describe('Teachermain', () => {
  let component: Teachermain;
  let fixture: ComponentFixture<Teachermain>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Teachermain]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Teachermain);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
