import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Videostreaming } from './videostreaming';

describe('Videostreaming', () => {
  let component: Videostreaming;
  let fixture: ComponentFixture<Videostreaming>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Videostreaming]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Videostreaming);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
