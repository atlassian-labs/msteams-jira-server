import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SelectCascadingFieldComponent } from './select-cascading-field.component';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { UntypedFormGroup, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';

describe('SelectCascadingFieldComponent', () => {
    let component: SelectCascadingFieldComponent;
    let fixture: ComponentFixture<SelectCascadingFieldComponent>;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;

    beforeEach(async () => {
        const dropdownUtilServiceSpy = jasmine.createSpyObj('DropdownUtilService',
            ['mapAllowedValueWithChildrenToSelectOption', 'mapAllowedValueToSelectOption']);

        await TestBed.configureTestingModule({
            declarations: [SelectCascadingFieldComponent],
            imports: [ReactiveFormsModule, NgSelectModule],
            providers: [
                { provide: DropdownUtilService, useValue: dropdownUtilServiceSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(SelectCascadingFieldComponent);
        component = fixture.componentInstance;
        dropdownUtilService = TestBed.inject(DropdownUtilService) as jasmine.SpyObj<DropdownUtilService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with default values', () => {
        component.data = {
            formControlName: 'testField',
            required: true,
            allowedValues: [{ id: '1', name: 'Parent 1', children: [{ id: '1.1', name: 'Child 1.1' }] }],
            defaultValue: { id: '1', child: { id: '1.1' } }
        };
        component.formGroup = new UntypedFormGroup({});
        dropdownUtilService.mapAllowedValueWithChildrenToSelectOption.and.returnValue({
            id: '1', name: 'Parent 1', children: [{ id: '1.1', name: 'Child 1.1' }] });
        dropdownUtilService.mapAllowedValueToSelectOption.and.returnValue({ id: '1.1', name: 'Child 1.1' });

        component.ngOnInit();

        expect(component.selectedParentId).toBe('1');
        expect(component.selectedChildId).toBe('1.1');
        expect(component.allowedParentOptions).toEqual([{ id: '1', name: 'Parent 1', children: [{ id: '1.1', name: 'Child 1.1' }] }]);
        expect(component.allowedChildrenOptions).toEqual([{ id: '1.1', name: 'Child 1.1' }]);
    });

    it('should clear child options on parent change to null', () => {
        component.onParentChange(null);

        expect(component.selectedChildId).toEqual([]);
        expect(component.allowedChildrenOptions).toEqual([]);
        expect(component.selectedCascadingOptions).toBeUndefined();
    });
});
