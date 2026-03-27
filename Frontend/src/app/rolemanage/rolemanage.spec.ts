import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Rolemanage } from './rolemanage';

describe('Rolemanage', () => {
  let component: Rolemanage;
  let fixture: ComponentFixture<Rolemanage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Rolemanage]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Rolemanage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
