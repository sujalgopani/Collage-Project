import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Resultsheet } from './resultsheet';

describe('Resultsheet', () => {
  let component: Resultsheet;
  let fixture: ComponentFixture<Resultsheet>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Resultsheet]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Resultsheet);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
