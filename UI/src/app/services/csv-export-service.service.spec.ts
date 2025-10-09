import { TestBed } from '@angular/core/testing';

import { CsvExportServiceService } from './csv-export-service.service';

describe('CsvExportServiceService', () => {
  let service: CsvExportServiceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CsvExportServiceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
