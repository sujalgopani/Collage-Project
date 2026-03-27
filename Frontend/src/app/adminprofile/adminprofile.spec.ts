import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Adminprofile } from './adminprofile';

describe('Adminprofile', () => {
  let component: Adminprofile;
  let fixture: ComponentFixture<Adminprofile>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Adminprofile]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Adminprofile);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
