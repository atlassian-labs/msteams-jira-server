import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PriorityDropdownComponent } from './priority-dropdown.component';
import { ApiService } from '@core/services';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { Priority } from '@core/models';
import { DropDownOption } from '@shared/models/dropdown-option.model';

describe('PriorityDropdownComponent', () => {
    let component: PriorityDropdownComponent;
    let fixture: ComponentFixture<PriorityDropdownComponent>;
    let apiServiceMock: any;
    let dropdownUtilServiceMock: any;

    beforeEach(async () => {
        apiServiceMock = {
            getPriorities: jasmine.createSpy('getPriorities'),
            updatePriority: jasmine.createSpy('updatePriority')
        };

        dropdownUtilServiceMock = {
            mapPriorityToDropdownOption: jasmine.createSpy('mapPriorityToDropdownOption')
        };

        await TestBed.configureTestingModule({
            declarations: [PriorityDropdownComponent, DropDownComponent],
            imports: [MatTooltipModule],
            providers: [
                { provide: ApiService, useValue: apiServiceMock },
                { provide: DropdownUtilService, useValue: dropdownUtilServiceMock }
            ],
            schemas: [NO_ERRORS_SCHEMA]
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(PriorityDropdownComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize priorities on init', async () => {
        const priorities: Priority[] = [{ id: '1', name: 'High', iconUrl: '', statusColor: '' }];
        const dropdownOptions: DropDownOption<string>[] = [{ id: '1', value: 'High', label: 'label' }];
        apiServiceMock.getPriorities.and.returnValue(Promise.resolve(priorities));
        dropdownUtilServiceMock.mapPriorityToDropdownOption.and.returnValue(dropdownOptions[0]);
        component.issuePriorityId = '1';

        await component.ngOnInit();

        expect(component.loading).toBe(false);
        expect(component.priorityOptions).toEqual(dropdownOptions);
        expect(component.selectedOption).toEqual(dropdownOptions[0]);
    });

    it('should handle priority option selection', async () => {
        const option: DropDownOption<string> = { id: '1', value: 'High', label: 'label' };
        const response = { isSuccess: true };
        const priorities: Priority[] = [{ id: '1', name: 'High', iconUrl: '', statusColor: '' }];
        const dropdownOptions: DropDownOption<string>[] = [{ id: '1', value: 'High', label: 'label' }];
        apiServiceMock.getPriorities.and.returnValue(Promise.resolve(priorities));
        dropdownUtilServiceMock.mapPriorityToDropdownOption.and.returnValue(dropdownOptions[0]);
        apiServiceMock.updatePriority.and.returnValue(Promise.resolve(response));
        spyOn(component.priorityChange, 'emit');

        await component.ngOnInit();
        await component.onPriorityOptionSelected(option);

        expect(component.loading).toBe(false);
        expect(component.priorityChange.emit).toHaveBeenCalledWith('1');
    });

    it('should handle error during priority option selection', async () => {
        const option: DropDownOption<string> = { id: '1', value: 'High', label: 'label' };
        const response = { isSuccess: false, errorMessage: 'Error' };
        apiServiceMock.updatePriority.and.returnValue(Promise.resolve(response));
        spyOn(component.dropdown, 'setPreviousValue');

        await component.onPriorityOptionSelected(option);

        expect(component.loading).toBe(false);
        expect(component.errorMessage).toBe('Error');
        expect(component.dropdown.setPreviousValue).toHaveBeenCalled();
    });

    it('should handle exception during priority option selection', async () => {
        const option: DropDownOption<string> = { id: '1', value: 'High', label: 'label' };
        const error = { message: 'Exception' };
        apiServiceMock.updatePriority.and.returnValue(Promise.reject(error));
        spyOn(component.dropdown, 'setPreviousValue');

        await component.onPriorityOptionSelected(option);

        expect(component.loading).toBe(false);
        expect(component.errorMessage).toBe('Exception');
        expect(component.dropdown.setPreviousValue).toHaveBeenCalled();
    });
});
