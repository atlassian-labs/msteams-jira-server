import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LabelsFieldComponent } from './labels-field.component';
import { ApiService } from '@core/services';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { UntypedFormGroup, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { of } from 'rxjs';
import { JiraFieldAutocomplete } from '@core/models/Jira/jira-field-autocomplete-data.model';

describe('LabelsFieldComponent', () => {
    let component: LabelsFieldComponent;
    let fixture: ComponentFixture<LabelsFieldComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService', ['getAutocompleteData']);
        const dropdownUtilServiceSpy = jasmine.createSpyObj('DropdownUtilService', ['mapAutocompleteDataToSelectOption']);

        await TestBed.configureTestingModule({
            declarations: [LabelsFieldComponent],
            imports: [ReactiveFormsModule, NgSelectModule],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: DropdownUtilService, useValue: dropdownUtilServiceSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(LabelsFieldComponent);
        component = fixture.componentInstance;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        dropdownUtilService = TestBed.inject(DropdownUtilService) as jasmine.SpyObj<DropdownUtilService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with default values', async () => {
        component.data = {
            jiraUrl: 'http://example.com',
            defaultValue: ['label1', 'label2']
        };
        component.formGroup = new UntypedFormGroup({});
        apiService.getAutocompleteData.and.returnValue(Promise.resolve([]));
        dropdownUtilService.mapAutocompleteDataToSelectOption.and.returnValue({ id: 'label1', name: 'Label 1' });

        await component.ngOnInit();

        expect(component.jiraUrl).toBe('http://example.com');
        expect(component.selectedOptionIds).toEqual(['label1', 'label2']);
    });

    it('should load labels on open', async () => {
        component.data = {
            jiraUrl: 'http://example.com'
        };
        component.formGroup = new UntypedFormGroup({});
        const autocomplete: JiraFieldAutocomplete = { value: 'label1', displayName: 'label1' };
        apiService.getAutocompleteData.and.returnValue(Promise.resolve([autocomplete]));
        dropdownUtilService.mapAutocompleteDataToSelectOption.and.returnValue({ id: 'label1', name: 'Label 1' });

        await component.onOpen();

        expect(apiService.getAutocompleteData).toHaveBeenCalled();
        expect(dropdownUtilService.mapAutocompleteDataToSelectOption).toHaveBeenCalled();
        expect(component.labelOptions).toEqual([{ id: 'label1', name: 'Label 1' }]);
    });

    it('should not load labels if already initialized', async () => {
        component.dataInitialized = true;
        component.data = {
            jiraUrl: 'http://example.com'
        };
        component.formGroup = new UntypedFormGroup({});
        apiService.getAutocompleteData.and.returnValue(Promise.resolve([{ value: 'label1', displayName: 'label1' }]));
        dropdownUtilService.mapAutocompleteDataToSelectOption.and.returnValue({ id: 'label1', name: 'Label 1' });

        await component.onOpen();

        expect(apiService.getAutocompleteData).not.toHaveBeenCalled();
        expect(dropdownUtilService.mapAutocompleteDataToSelectOption).not.toHaveBeenCalled();
    });

    it('should add custom label', () => {
        const term = 'customLabel';
        const result = component.addCustomLabel(term);
        expect(result).toEqual({ id: term, name: term });
    });

    it('should not add custom label with spaces', () => {
        const term = 'custom label';
        const result = component.addCustomLabel(term);
        expect(result).toBeNull();
    });

    it('should not add custom label exceeding max length', () => {
        const term = 'a'.repeat(256);
        const result = component.addCustomLabel(term);
        expect(result).toBeNull();
    });

    it('should handle search event with valid term', () => {
        const event = { term: 'validLabel' };
        component.onSearch(event);
        expect(component.labelsError).toBeFalse();
    });

    it('should handle search event with invalid term', () => {
        const event = { term: 'invalid label' };
        component.onSearch(event);
        expect(component.labelsError).toBeTrue();
    });

    it('should clear labels error on clear', () => {
        component.labelsError = true;
        component.onClear();
        expect(component.labelsError).toBeFalse();
    });

    it('should clear labels error on blur', () => {
        component.labelsError = true;
        component.onBlur();
        expect(component.labelsError).toBeFalse();
    });
});
