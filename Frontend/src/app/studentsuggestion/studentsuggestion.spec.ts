import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Studentsuggestion } from './studentsuggestion';

describe('Studentsuggestion', () => {
  let component: Studentsuggestion;
  let fixture: ComponentFixture<Studentsuggestion>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Studentsuggestion]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Studentsuggestion);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
