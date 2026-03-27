import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Teacherdash } from './teacherdash';

describe('Teacherdash', () => {
  let component: Teacherdash;
  let fixture: ComponentFixture<Teacherdash>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Teacherdash]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Teacherdash);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
