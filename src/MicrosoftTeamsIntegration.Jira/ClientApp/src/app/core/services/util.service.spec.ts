import { TestBed } from '@angular/core/testing';

import { UtilService } from '@core/services/util.service';
import { } from 'jasmine'; // Fix warnings with decribe, beforeEach and other jasmine methods

describe('Service: Util',
    () => {
        let service: UtilService;

        beforeEach(() => {
            TestBed.configureTestingModule({
                providers: [UtilService]
            });

            service = TestBed.get(UtilService);
        });

        it('should convert string to null or return string value',
            () => {
                const nullString = service.convertStringToNull('null');
                expect(nullString).toEqual(null);

                const value = service.convertStringToNull('value');
                expect(value).toEqual('value');
            });

        it('should return predefined filters',
            () => {
                const expectedPredefinedFilters = [
                    { id: 0, value: 'all-issues', label: 'All issues' },
                    { id: 1, value: 'open-issues', label: 'Open issues' },
                    { id: 2, value: 'done-issues', label: 'Done issues' },
                    { id: 3, value: 'viewed-recently', label: 'Viewed recently' },
                    { id: 4, value: 'created-recently', label: 'Created recently' },
                    { id: 5, value: 'resolved-recently', label: 'Resolved recently' },
                    { id: 6, value: 'updated-recently', label: 'Updated recently' }
                ];

                const predefinedFilters = service.getFilters();
                expect(predefinedFilters).toEqual(expectedPredefinedFilters);
            });

        it('should encode string',
            () => {
                const encodedValue = service.encode('(PRO!)');
                expect(encodedValue).toEqual('%28PRO%21%29');
            });
    });
