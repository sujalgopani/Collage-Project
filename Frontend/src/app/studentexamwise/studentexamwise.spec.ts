import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Studentexamwise } from './studentexamwise';

describe('Studentexamwise', () => {
  let component: Studentexamwise;
  let fixture: ComponentFixture<Studentexamwise>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Studentexamwise]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Studentexamwise);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
