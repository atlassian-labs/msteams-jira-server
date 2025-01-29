/* eslint-disable @typescript-eslint/no-unused-expressions */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UserPickerFieldComponent } from './userpicker-field.component';
import { ApiService } from '@core/services';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { UntypedFormGroup, ReactiveFormsModule } from '@angular/forms';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { JiraUser } from '@core/models/Jira/jira-user.model';

describe('UserPickerFieldComponent', () => {
    let component: UserPickerFieldComponent;
    let fixture: ComponentFixture<UserPickerFieldComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService', ['searchUsers']);
        const dropdownUtilServiceSpy = jasmine.createSpyObj('DropdownUtilService', ['mapUserToDropdownOption']);

        await TestBed.configureTestingModule({
            declarations: [UserPickerFieldComponent, DropDownComponent],
            imports: [ReactiveFormsModule],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: DropdownUtilService, useValue: dropdownUtilServiceSpy }
            ],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(UserPickerFieldComponent);
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
            formControlName: 'userPicker',
            defaultValue: { accountId: 'testUser', displayName: 'Test User' }
        };
        component.formGroup = new UntypedFormGroup({});
        dropdownUtilService.mapUserToDropdownOption.and.returnValue({ id: 'testUser', value: 'testUser', label: 'Test User' });

        await component.ngOnInit();

        expect(component.jiraUrl).toBe('http://example.com');
        expect(component.options).toContain({ id: 'testUser', value: 'testUser', label: 'Test User' });
        expect(dropdownUtilService.mapUserToDropdownOption).toHaveBeenCalled();
    });

    it('should search for users', async () => {
        const jiraUser: JiraUser = { accountId: 'testUser', displayName: 'Test User', active: true } as JiraUser;
        apiService.searchUsers.and.returnValue(Promise.resolve([jiraUser]));
        dropdownUtilService.mapUserToDropdownOption.and.returnValue({ id: 'testUser', value: 'testUser', label: 'Test User' });

        component.dropdown = { filteredOptions: [] } as any;
        await component.onSearchChanged('testUser');

        expect(apiService.searchUsers).toHaveBeenCalled();
        expect(dropdownUtilService.mapUserToDropdownOption).toHaveBeenCalled();
        expect(component.dropdown.filteredOptions).toEqual([{ id: 'testUser', value: 'testUser', label: 'Test User' }]);
    });

    it('should handle empty search term', async () => {
        const jiraUser: JiraUser = { accountId: 'testUser', displayName: 'Test User', active: true } as JiraUser;
        apiService.searchUsers.and.returnValue(Promise.resolve([jiraUser]));
        dropdownUtilService.mapUserToDropdownOption.and.returnValue({ id: 'testUser', value: 'testUser', label: 'Test User' });

        component.dropdown = { filteredOptions: [] } as any;
        await component.onSearchChanged('');

        expect(apiService.searchUsers).toHaveBeenCalled();
        expect(dropdownUtilService.mapUserToDropdownOption).toHaveBeenCalled();
        expect(component.dropdown.filteredOptions).toEqual([{ id: 'testUser', value: 'testUser', label: 'Test User' }]);
    });
});
