import { TestBed } from '@angular/core/testing';
import { FieldsService } from './fields.service';
import { DropdownUtilService } from './dropdown.util.service';
import { SelectFieldComponent } from '@app/components/issues/fields/select-field.component';
import { TextFieldSingleComponent } from '@app/components/issues/fields/text-field-single.component';
import { DatePickerFieldComponent } from '@app/components/issues/fields/datepicker-field.component';
import { RadioSelectFieldComponent } from '@app/components/issues/fields/radio-select-field.component';
import { CheckboxSelectFieldComponent } from '@app/components/issues/fields/checkbox-select-field';
import { TextFieldNumberComponent } from '@app/components/issues/fields/text-field-number.component';
import { UserPickerFieldComponent } from '@app/components/issues/fields/userpicker-field.component';
import { LabelsFieldComponent } from '@app/components/issues/fields/labels-field.component';
import { SprintFieldComponent } from '@app/components/issues/fields/sprint-field.component';
import { EpicFieldComponent } from '@app/components/issues/fields/epic-field.component';
import { UrlFieldComponent } from '@app/components/issues/fields/url-field.component';

describe('FieldsService', () => {
    let service: FieldsService;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;

    beforeEach(() => {
        const spy = jasmine.createSpyObj('DropdownUtilService', ['mapAllowedValueToSelectOption']);

        TestBed.configureTestingModule({
            providers: [
                FieldsService,
                { provide: DropdownUtilService, useValue: spy }
            ]
        });
        service = TestBed.inject(FieldsService);
        dropdownUtilService = TestBed.inject(DropdownUtilService) as jasmine.SpyObj<DropdownUtilService>;
    });

    it('should get allowed fields', () => {
        const fields = {
            project: { schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:select' }, required: true },
            customfield_10000: { schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:textfield' }, required: false }
        };
        const result = service.getAllowedFields(fields);
        expect(result.length).toBe(2);
    });

    it('should get allowed transformed fields', () => {
        const fields = {
            project: { schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:select' }, required: true }
        };
        const formValue = { project: '10000' };
        const result = service.getAllowedTransformedFields(fields, formValue);
        expect(result).toEqual({ project: '10000' });
    });

    it('should get custom field templates', () => {
        const fields = {
            project: { schema:
                { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:select' },
            required: true, allowedValues: [{ id: '10000', name: 'Project 1' }] }
        };
        dropdownUtilService.mapAllowedValueToSelectOption.and.returnValue({ id: '10000', name: 'Project 1' });
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(SelectFieldComponent);
    });

    it('should return empty array if fields are null or undefined', () => {
        expect(service.getCustomFieldTemplates(null, 'http://jira.example.com')).toEqual([]);
        expect(service.getCustomFieldTemplates(undefined, 'http://jira.example.com')).toEqual([]);
    });

    it('should return SelectFieldComponent for multiselect custom field', () => {
        const fields = {
            customfield_10000: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:multiselect' },
                allowedValues: [{ id: '10000', name: 'Option 1' }],
                required: true
            }
        };
        dropdownUtilService.mapAllowedValueToSelectOption.and.returnValue({ id: '10000', name: 'Option 1' });
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(SelectFieldComponent);
    });

    it('should return TextFieldSingleComponent for textfield custom field', () => {
        const fields = {
            customfield_10001: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:textfield' },
                required: true
            }
        };
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(TextFieldSingleComponent);
    });

    it('should return DatePickerFieldComponent for datepicker custom field', () => {
        const fields = {
            customfield_10002: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:datepicker' },
                required: true
            }
        };
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(DatePickerFieldComponent);
    });

    it('should return RadioSelectFieldComponent for radiobuttons custom field', () => {
        const fields = {
            customfield_10003: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:radiobuttons' },
                allowedValues: [{ id: '10000', name: 'Option 1' }],
                required: true
            }
        };
        dropdownUtilService.mapAllowedValueToSelectOption.and.returnValue({ id: '10000', name: 'Option 1' });
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(RadioSelectFieldComponent);
    });

    it('should return CheckboxSelectFieldComponent for multicheckboxes custom field', () => {
        const fields = {
            customfield_10004: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:multicheckboxes' },
                allowedValues: [{ id: '10000', name: 'Option 1' }],
                required: true
            }
        };
        dropdownUtilService.mapAllowedValueToSelectOption.and.returnValue({ id: '10000', name: 'Option 1' });
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(CheckboxSelectFieldComponent);
    });

    it('should return TextFieldNumberComponent for float custom field', () => {
        const fields = {
            customfield_10005: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:float' },
                required: true
            }
        };
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(TextFieldNumberComponent);
    });

    it('should return UserPickerFieldComponent for userpicker custom field', () => {
        const fields = {
            customfield_10006: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:userpicker' },
                required: true
            }
        };
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(UserPickerFieldComponent);
    });

    it('should return LabelsFieldComponent for labels system field', () => {
        const fields = {
            labels: {
                schema: { system: 'labels' },
                required: true
            }
        };
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(LabelsFieldComponent);
    });

    it('should return SprintFieldComponent for sprint custom field', () => {
        const fields = {
            customfield_10007: {
                schema: { custom: 'com.pyxis.greenhopper.jira:gh-sprint' },
                required: true
            }
        };
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(SprintFieldComponent);
    });

    it('should return SprintFieldComponent for sprint custom field with project', () => {
        const fields = {
            customfield_10007: {
                schema: { custom: 'com.pyxis.greenhopper.jira:gh-sprint' },
                required: true
            },
            project: { schema:
                { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:select' },
            required: true, allowedValues: [{ id: '10000', name: 'Project 1' }] }
        };
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(2);
    });

    it('should return EpicFieldComponent for epic-link custom field', () => {
        const fields = {
            customfield_10008: {
                schema: { custom: 'com.pyxis.greenhopper.jira:gh-epic-link' },
                required: true
            }
        };
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(EpicFieldComponent);
    });

    it('should return UrlFieldComponent for url custom field', () => {
        const fields = {
            customfield_10009: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:url' },
                required: true
            }
        };
        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        expect(result.length).toBe(1);
        expect(result[0].component).toBe(UrlFieldComponent);
    });

    it('should order dynamic fields correctly', () => {
        const fields = {
            project: { schema:
                { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:select' },
            required: true, allowedValues: [{ id: '10000', name: 'Project 1' }] },
            customfield_10000: { schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:textfield' }, required: false },
            customfield_10001: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:multiselect' },
                allowedValues: [{ id: '10000', name: 'Option 1' }],
                required: true
            },
            customfield_10002: { schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:datepicker' }, required: true },
            customfield_10003: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:radiobuttons' },
                allowedValues: [{ id: '10000', name: 'Option 1' }],
                required: true
            },
            customfield_10004: {
                schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:multicheckboxes' },
                allowedValues: [{ id: '10000', name: 'Option 1' }],
                required: true
            },
            customfield_10005: { schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:float' }, required: true },
            customfield_10006: { schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:userpicker' }, required: true },
            customfield_10007: { schema: { custom: 'com.pyxis.greenhopper.jira:gh-sprint' }, required: true },
            customfield_10008: { schema: { custom: 'com.pyxis.greenhopper.jira:gh-epic-link' }, required: true },
            customfield_10009: { schema: { custom: 'com.atlassian.jira.plugin.system.customfieldtypes:url' }, required: true },
            labels: { schema: { system: 'labels' }, required: true },
            duedate: { schema: { system: 'duedate' }, required: true }
        };

        const result = service.getCustomFieldTemplates(fields, 'http://jira.example.com');
        const orderedKeys = result.map(field => field.data.formControlName);

        expect(orderedKeys).toEqual([
            'project',
            'labels',
            'duedate',
            'customfield_10007',
            'customfield_10008',
            'customfield_10000',
            'customfield_10001',
            'customfield_10002',
            'customfield_10003',
            'customfield_10004',
            'customfield_10005',
            'customfield_10006',
            'customfield_10009'
        ]);
    });

    it('should return null if value is null or undefined', () => {
        expect(service.getDefaultValue(null)).toBeNull();
        expect(service.getDefaultValue(undefined)).toBeNull();
    });

    it('should return array of ids if value is an array', () => {
        const value = [{ id: '1' }, { id: '2' }];
        expect(service.getDefaultValue(value)).toEqual(['1', '2']);
    });

    it('should return id if value is an object with id', () => {
        const value = { id: '1' };
        expect(service.getDefaultValue(value)).toBe('1');
    });

    it('should return value if value is an object without id', () => {
        const value = { name: 'test' };
        expect(service.getDefaultValue(value)).toEqual(value);
    });

    it('should return value if value is a primitive', () => {
        expect(service.getDefaultValue('test')).toBe('test');
        expect(service.getDefaultValue(123)).toBe(123);
        expect(service.getDefaultValue(true)).toBe(true);
    });

    it('should return value if value is an object with id and child', () => {
        const value = { id: '1', child: 'child' };
        expect(service.getDefaultValue(value)).toEqual(value);
    });

    it('should return default value if field has default value', () => {
        const dynamicField = {
            hasDefaultValue: true,
            defaultValue: { id: '1' }
        };
        spyOn(service, 'getDefaultValue').and.returnValue('1');
        const result = service['getDefaultValueFromField'](dynamicField);
        expect(result).toBe('1');
        expect(service.getDefaultValue).toHaveBeenCalledWith(dynamicField.defaultValue);
    });

    it('should return null if field does not have default value', () => {
        const dynamicField = {
            hasDefaultValue: false,
            defaultValue: { id: '1' }
        };
        const result = service['getDefaultValueFromField'](dynamicField);
        expect(result).toBeNull();
    });

    it('should return null if getDefaultValue throws an error', () => {
        const dynamicField = {
            hasDefaultValue: true,
            defaultValue: { id: '1' }
        };
        spyOn(service, 'getDefaultValue').and.throwError('Error');
        const result = service['getDefaultValueFromField'](dynamicField);
        expect(result).toBeNull();
    });
});
