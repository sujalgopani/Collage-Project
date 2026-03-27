import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Suggestion } from './suggestion';

describe('Suggestion', () => {
  let component: Suggestion;
  let fixture: ComponentFixture<Suggestion>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Suggestion]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Suggestion);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
