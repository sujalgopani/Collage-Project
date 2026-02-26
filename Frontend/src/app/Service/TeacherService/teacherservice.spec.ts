import { TestBed } from '@angular/core/testing';

import { Teacherservice } from './teacherservice';

describe('Teacherservice', () => {
  let service: Teacherservice;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Teacherservice);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
