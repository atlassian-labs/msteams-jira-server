import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AssigneeDropdownComponent } from './assignee-dropdown.component';
import { ApiService } from '@core/services/api.service';
import { AssigneeService } from '@core/services/entities/assignee.service';
import { AppInsightsService } from '@core/services';
import { DropDownComponent } from '@shared/components/dropdown/dropdown.component';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CurrentJiraUser, JiraUser } from '@core/models/Jira/jira-user.model';

describe('AssigneeDropdownComponent', () => {
    let component: AssigneeDropdownComponent;
    let fixture: ComponentFixture<AssigneeDropdownComponent>;
    let apiServiceMock: any;
    let assigneeServiceMock: any;
    let appInsightsServiceMock: any;

    beforeEach(async () => {
        apiServiceMock = jasmine.createSpyObj('ApiService', ['getCurrentUserData']);
        assigneeServiceMock = jasmine.createSpyObj('AssigneeService', ['setAssignee', 'searchAssignable', 'assigneesToDropdownOptions']);
        appInsightsServiceMock = jasmine.createSpyObj('AppInsightsService', ['trackException']);

        await TestBed.configureTestingModule({
            declarations: [AssigneeDropdownComponent, DropDownComponent],
            imports: [MatTooltipModule],
            providers: [
                { provide: ApiService, useValue: apiServiceMock },
                { provide: AssigneeService, useValue: assigneeServiceMock },
                { provide: AppInsightsService, useValue: appInsightsServiceMock }
            ]
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(AssigneeDropdownComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize options and current user on init', async () => {
        const mockUserData: CurrentJiraUser = {
            accountId: '123', name: 'testUser',
            jiraServerInstanceUrl: '',
            self: '',
            emailAddress: '',
            hashedEmailAddress: '',
            avatarUrls: { '24x24': '', '16x16': '', '32x32': '', '48x48': '' },
            displayName: '',
            active: false,
            timeZone: ''
        };
        apiServiceMock.getCurrentUserData.and.returnValue(Promise.resolve(mockUserData));
        assigneeServiceMock.searchAssignable.and.returnValue(Promise.resolve([]));
        assigneeServiceMock.assigneesToDropdownOptions.and.returnValue([]);

        await component.ngOnInit();

        expect(component.currentUserAccountId).toBe(mockUserData.accountId);
        expect(component.loading).toBeFalse();
    });

    it('should handle error on setAssignee', async () => {
        const mockErrorResponse = { isSuccess: false, errorMessage: 'Error' };
        assigneeServiceMock.setAssignee.and.returnValue(Promise.resolve(mockErrorResponse));

        await component.onOptionSelected({ id: 'invalidId', value: 'invalidId', label: 'Invalid' });

        expect(component.errorMessage).toBe(mockErrorResponse.errorMessage);
        expect(appInsightsServiceMock.trackException).toHaveBeenCalled();
        expect(component.loading).toBeFalse();
    });

    it('should emit assigneeChange on successful setAssignee', async () => {
        const mockResponse = { isSuccess: true, content: 'newAccountId' };
        const mockAssignee: JiraUser[] = [
            { 
                accountId: 'newAccountId',
                displayName: 'New Assignee',
                name: '',
                self: '',
                emailAddress: '',
                hashedEmailAddress: '',
                avatarUrls: { '24x24': '16x16', '16x16': '16x16', '32x32': '16x16', '48x48': '16x16' },
                active: true,
                timeZone: '',
            }
        ];

        assigneeServiceMock.setAssignee.and.returnValue(Promise.resolve(mockResponse));
        assigneeServiceMock.searchAssignable.and.returnValue(Promise.resolve(mockAssignee));
        assigneeServiceMock.assigneesToDropdownOptions.and.returnValue([{ value: 'newAccountId', label: 'New Assignee' }]);

        spyOn(component.assigneeChange, 'emit');

        await component.onSearchChanged('');
        await component.onOptionSelected({ id: 'newAccountId', value: 'newAccountId', label: 'New Assignee' });

        expect(component.selectedOption?.value).toBe('newAccountId');
        expect(component.assigneeChange.emit).toHaveBeenCalledWith(mockAssignee[0]);
        expect(component.loading).toBeFalse();
    });

    it('should filter options on search change', async () => {
        const mockOptions = [{ value: '1', label: 'User1' }, { value: '2', label: 'User2' }];
        assigneeServiceMock.searchAssignable.and.returnValue(Promise.resolve([]));
        assigneeServiceMock.assigneesToDropdownOptions.and.returnValue(mockOptions);

        await component.onSearchChanged('User');

        expect(component.dropdown.filteredOptions).toEqual(mockOptions);
        expect(component.loading).toBeFalse();
    });
});
