import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TreeComponent } from './tree.component';
import { SelectOption } from '@shared/models/select-option.model';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';

describe('TreeComponent', () => {
    let component: TreeComponent;
    let fixture: ComponentFixture<TreeComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [TreeComponent],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(TreeComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with default values', () => {
        expect(component.options).toEqual([]);
        expect(component.isExpanded).toBeFalse();
        expect(component.selectedOptions).toEqual([]);
    });

    it('should toggle expansion state', () => {
        component.toggle();
        expect(component.isExpanded).toBeTrue();
        component.toggle();
        expect(component.isExpanded).toBeFalse();
    });

    it('should check if option is selected', () => {
        const option: SelectOption = { id: '1', value: 'option1', label: 'Option 1' };
        component.selectedOptions = [option];
        expect(component.isOptionSelected(option)).toBeTrue();
    });

    it('should check if all options are selected', () => {
        const options: SelectOption[] = [
            { id: '1', value: 'option1', label: 'Option 1' },
            { id: '2', value: 'option2', label: 'Option 2' }
        ];
        component.options = options;
        component.selectedOptions = options;
        expect(component.isAllOptionsSelected()).toBeTrue();
    });

    it('should handle option click', () => {
        const option: SelectOption = { id: '1', value: 'option1', label: 'Option 1' };
        spyOn(component.optionSelect, 'emit');
        component.onOptionClicked(option);
        expect(component.selectedOptions).toEqual([option]);
        expect(component.optionSelect.emit).toHaveBeenCalledWith({ options: [option], isAll: false });
    });

    it('should remove option from selected options on click', () => {
        const option: SelectOption = { id: '1', value: 'option1', label: 'Option 1' };
        component.selectedOptions = [option];
        spyOn(component.optionSelect, 'emit');
        component.onOptionClicked(option);
        expect(component.selectedOptions).toEqual([]);
        expect(component.optionSelect.emit).toHaveBeenCalledWith({ options: [], isAll: true });
    });

    it('should emit event when all options are selected', () => {
        const options: SelectOption[] = [
            { id: '1', value: 'option1', label: 'Option 1' },
            { id: '2', value: 'option2', label: 'Option 2' }
        ];
        component.options = options;
        component.selectedOptions = options;
        spyOn(component.optionSelect, 'emit');
        component.onOptionClicked(options[0]);
        expect(component.optionSelect.emit)
            .toHaveBeenCalledWith({ options: [ {id: '2', value: 'option2', label: 'Option 2'} ], isAll: true });
    });
});
