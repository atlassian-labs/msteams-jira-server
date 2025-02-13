import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SelectComponent } from './select.component';
import { SelectOption } from '@shared/models/select-option.model';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';

describe('SelectComponent', () => {
    let component: SelectComponent;
    let fixture: ComponentFixture<SelectComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [SelectComponent],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(SelectComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with default values', () => {
        expect(component.options).toEqual([]);
        expect(component.multiple).toBeFalse();
        expect(component.placeholder).toBe('');
        expect(component.title).toBe('');
        expect(component.isOpened).toBeFalse();
    });

    it('should set options and reset selected options', () => {
        const options: SelectOption[] = [
            { id: '1', value: 'option1', label: 'Option 1' },
            { id: '2', value: 'option2', label: 'Option 2' }
        ];
        component.options = options;
        expect(component.options).toEqual(options);
        expect(component['selectedOptions']).toEqual([]);
    });

    it('should handle multiple selection', () => {
        component.multiple = true;
        expect(component.multiple).toBeTrue();
        expect(component.options).toContain({ id: -1, value: null, label: 'All' });
    });

    it('should handle single option click', () => {
        const option: SelectOption = { id: '1', value: 'option1', label: 'Option 1' };
        spyOn(component.optionSelect, 'emit');
        component.optionClicked(option);
        expect(component['selectedOptions']).toEqual([option]);
        expect(component.optionSelect.emit).toHaveBeenCalledWith({ options: [option] });
    });

    it('should handle multiple option click', () => {
        const options: SelectOption[] = [
            { id: '1', value: 'option1', label: 'Option 1' },
            { id: '2', value: 'option2', label: 'Option 2' }
        ];
        component.options = options;
        component.multiple = true;
        const option: SelectOption = { id: '1', value: 'option1', label: 'Option 1' };
        spyOn(component.optionSelect, 'emit');
        component.onCheckboxClicked(option);
        expect(component['selectedOptions']).toEqual([option]);
        expect(component.optionSelect.emit).toHaveBeenCalledWith({ options: [option], isAll: false });
    });

    it('should handle all option click', () => {
        component.multiple = true;
        const allOption: SelectOption = { id: -1, value: null, label: 'All' };
        component.options = [
            allOption,
            { id: '1', value: 'option1', label: 'Option 1' },
            { id: '2', value: 'option2', label: 'Option 2' }
        ];
        spyOn(component.optionSelect, 'emit');
        component.onCheckboxClicked(allOption);
        expect(component['selectedOptions']).toEqual(component.options);
        expect(component.optionSelect.emit).toHaveBeenCalledWith({ options: [allOption], isAll: true });
    });

    it('should toggle dropdown', () => {
        component.toggle();
        expect(component.isOpened).toBeTrue();
        component.toggle();
        expect(component.isOpened).toBeFalse();
    });

    it('should close dropdown', () => {
        component.close();
        expect(component.isOpened).toBeFalse();
    });

    it('should return label text for single selection', () => {
        const option: SelectOption = { id: '1', value: 'option1', label: 'Option 1' };
        component['selectedOptions'] = [option];
        expect(component.labelText).toBe('Option 1');
    });

    it('should return placeholder text when no option is selected', () => {
        component.placeholder = 'Select an option';
        expect(component.labelText).toBe('Select an option');
    });

    it('should check if option is selected', () => {
        const option: SelectOption = { id: '1', value: 'option1', label: 'Option 1' };
        component['selectedOptions'] = [option];
        expect(component.isOptionSelected(option)).toBeTrue();
    });

    it('should change selected options', () => {
        const option: SelectOption = { id: '1', value: 'option1', label: 'Option 1' };
        component['selectedOptions'] = [];
        component['changeSelectedOptions'](option);
        expect(component['selectedOptions']).toEqual([option]);
    });

    it('should handle all option selection', () => {
        const allOption: SelectOption = { id: -1, value: null, label: 'All' };
        component.options = [
            allOption,
            { id: '1', value: 'option1', label: 'Option 1' },
            { id: '2', value: 'option2', label: 'Option 2' }
        ];
        component['selectedOptions'] = [];
        component['changeSelectedOptions'](allOption);
        expect(component['selectedOptions']).toEqual(component.options);
    });

    it('should remove option from selected options', () => {
        const option: SelectOption = { id: '1', value: 'option1', label: 'Option 1' };
        component['selectedOptions'] = [option];
        component['changeSelectedOptions'](option);
        expect(component['selectedOptions']).toEqual([]);
    });
});
