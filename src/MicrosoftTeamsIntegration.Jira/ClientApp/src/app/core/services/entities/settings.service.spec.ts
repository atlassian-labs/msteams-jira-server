import { TestBed } from '@angular/core/testing';
import { SettingsService } from './settings.service';
import { SelectOption } from '@shared/models/select-option.model';

describe('SettingsService', () => {
    let service: SettingsService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [SettingsService]
        });

        service = TestBed.inject(SettingsService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should build options for response with key', () => {
        const response = [
            { id: '1', name: 'Option 1', key: 'key1' },
            { id: '2', name: 'Option 2', key: 'key2' }
        ];

        const expectedOptions: SelectOption[] = [
            { id: 1, label: 'Option 1', value: 'key1' },
            { id: 2, label: 'Option 2', value: 'key2' }
        ];

        const options = service.buildOptionsFor(response);
        expect(options).toEqual(expectedOptions);
    });

    it('should build options for response without key', () => {
        const response = [
            { id: '1', name: 'Option 1' },
            { id: '2', name: 'Option 2' }
        ];

        const expectedOptions: SelectOption[] = [
            { id: 1, label: 'Option 1', value: 'Option 1' },
            { id: 2, label: 'Option 2', value: 'Option 2' }
        ];

        const options = service.buildOptionsFor(response);
        expect(options).toEqual(expectedOptions);
    });

    it('should handle empty response', () => {
        const response: any[] = [];

        const expectedOptions: SelectOption[] = [];

        const options = service.buildOptionsFor(response);
        expect(options).toEqual(expectedOptions);
    });
});
