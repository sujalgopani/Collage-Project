import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Createcource } from './createcource';

describe('Createcource', () => {
  let component: Createcource;
  let fixture: ComponentFixture<Createcource>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Createcource]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Createcource);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
