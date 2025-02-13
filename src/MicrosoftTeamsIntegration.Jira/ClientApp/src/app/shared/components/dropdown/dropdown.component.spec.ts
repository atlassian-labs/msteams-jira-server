import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DropDownComponent } from './dropdown.component';
import { UntypedFormControl, ReactiveFormsModule } from '@angular/forms';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { UtilService } from '@core/services';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { DomSanitizer } from '@angular/platform-browser';

describe('DropDownComponent', () => {
    let component: DropDownComponent<string>;
    let fixture: ComponentFixture<DropDownComponent<string>>;
    let utilService: jasmine.SpyObj<UtilService>;

    beforeEach(async () => {
        const utilServiceSpy = jasmine.createSpyObj('UtilService', ['jsonEqual']);

        await TestBed.configureTestingModule({
            declarations: [DropDownComponent],
            imports: [ReactiveFormsModule],
            providers: [
                { provide: UtilService, useValue: utilServiceSpy },
                { provide: DomSanitizer, useValue: { bypassSecurityTrustUrl: (url: string) => url } }
            ],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(DropDownComponent<string>);
        component = fixture.componentInstance;
        utilService = TestBed.inject(UtilService) as jasmine.SpyObj<UtilService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with default values', () => {
        component.ngOnInit();
        expect(component.options).toEqual([{ id: null, value: null, label: 'Loading...' }]);
    });

    it('should set options and selected value', () => {
        const options: DropDownOption<string>[] = [
            { id: '1', value: 'option1', label: 'Option 1' },
            { id: '2', value: 'option2', label: 'Option 2' }
        ];
        component.options = options;
        expect(component.options).toEqual(options);
        expect(component.selected).toEqual(options[0]);
    });

    it('should handle option click', () => {
        const option: DropDownOption<string> = { id: '1', value: 'option1', label: 'Option 1' };
        spyOn(component.optionSelect, 'emit');
        component.optionClicked(option);
        expect(component.selected).toEqual(option);
        expect(component.optionSelect.emit).toHaveBeenCalledWith(option);
    });

    it('should handle search input', () => {
        const options: DropDownOption<string>[] = [
            { id: '1', value: 'option1', label: 'Option 1' },
            { id: '2', value: 'option2', label: 'Option 2' }
        ];
        component.options = options;
        component.searchable = true;
        component.isInnerSearch = true;
        component.onSearchInput({ target: { value: 'Option 1' } } as any);
        expect(component.filteredOptions).toEqual([options[0]]);
    });

    it('should set previous value', () => {
        const option: DropDownOption<string> = { id: '1', value: 'option1', label: 'Option 1' };
        component['previouslySelected'] = option;
        component.setPreviousValue();
        expect(component.selected).toEqual(option);
    });

    it('should open dropdown', () => {
        component.open();
        expect(component.opened).toBeTrue();
    });

    it('should close dropdown', () => {
        component.close();
        expect(component.opened).toBeFalse();
    });

    it('should toggle dropdown', () => {
        component.toggle();
        expect(component.opened).toBeTrue();
        component.toggle();
        expect(component.opened).toBeFalse();
    });

    it('should write value', () => {
        const option: DropDownOption<string> = { id: '1', value: 'option1', label: 'Option 1' };
        component.options = [option];
        utilService.jsonEqual.and.returnValue(true);
        component.writeValue('option1');
        expect(component.selected).toEqual(option);
    });

    it('should register onChange', () => {
        const fn = jasmine.createSpy('fn');
        component.registerOnChange(fn);
        component['_onChange']('value');
        expect(fn).toHaveBeenCalledWith('value');
    });

    it('should register onTouched', () => {
        const fn = jasmine.createSpy('fn');
        component.registerOnTouched(fn);
        component['_onTouched']();
        expect(fn).toHaveBeenCalled();
    });

    it('should set disabled state', () => {
        component.setDisabledState(true);
        expect(component.disabled).toBeTrue();
    });

    it('should set icon classes', () => {
        component.iconSize = 'md';
        const classes = component.setIconClasses();
        expect(classes).toEqual({
            'icon': true,
            'dropdown__option-icon': true,
            'icon--md': true,
            'icon--sm': false
        });
    });

    it('should handle blur event', () => {
        const fn = jasmine.createSpy('fn');
        component.registerOnTouched(fn);
        component.ontouchend();
        expect(fn).toHaveBeenCalled();
    });

    it('should unsubscribe from searchChangeDebouncer on destroy', () => {
        const subscription = jasmine.createSpyObj('Subscription', ['unsubscribe']);
        component['searchChangeDebouncerSubscription'] = subscription;
        component.ngOnDestroy();
        expect(subscription.unsubscribe).toHaveBeenCalled();
    });
});
