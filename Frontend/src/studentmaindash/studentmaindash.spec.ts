import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Studentmaindash } from './studentmaindash';

describe('Studentmaindash', () => {
  let component: Studentmaindash;
  let fixture: ComponentFixture<Studentmaindash>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Studentmaindash]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Studentmaindash);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
