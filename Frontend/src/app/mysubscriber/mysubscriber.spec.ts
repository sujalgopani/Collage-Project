import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Mysubscriber } from './mysubscriber';

describe('Mysubscriber', () => {
  let component: Mysubscriber;
  let fixture: ComponentFixture<Mysubscriber>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Mysubscriber]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Mysubscriber);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
