import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CreateIssueDialogComponent } from './create-issue-dialog.component';
import { ApiService, AppInsightsService, ErrorService, UtilService } from '@core/services';
import { AssigneeService } from '@core/services/entities/assignee.service';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { FieldsService } from '@shared/services/fields.service';
import { NotificationService } from '@shared/services/notificationService';
import { AnalyticsService } from '@core/services/analytics.service';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { PermissionService } from '@core/services/entities/permission.service';
import { Issue, Priority, Project } from '@core/models';
import { JiraPermissionsResponse } from '@core/models/Jira/jira-permission.model';
import { JiraIssueFieldMeta } from '@core/models/Jira/jira-issue-field-meta.model';
import { TeamsService } from '@core/services/teams.service';
import { UntypedFormGroup, UntypedFormControl } from '@angular/forms';

describe('CreateIssueDialogComponent', () => {
    let teamsService: jasmine.SpyObj<TeamsService>;
    let component: CreateIssueDialogComponent;
    let fixture: ComponentFixture<CreateIssueDialogComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let assigneeService: jasmine.SpyObj<AssigneeService>;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let utilService: jasmine.SpyObj<UtilService>;
    let errorService: jasmine.SpyObj<ErrorService>;
    let permissionService: jasmine.SpyObj<PermissionService>;
    let fieldsService: jasmine.SpyObj<FieldsService>;
    let notificationService: jasmine.SpyObj<NotificationService>;
    let analyticsService: jasmine.SpyObj<AnalyticsService>;
    let route: ActivatedRoute;
    let router: Router;
    let dialog: MatDialog;

    const fieldMeta: JiraIssueFieldMeta<any>[] = [{
        allowedValues: [],
        defaultValue: [],
        hasDefaultValue: false,
        key: 'customfield_10000',
        name: 'Custom Field',
        operations: ['set', 'add'],
        required: true,
        schema: {
            type: 'string',
            items: 'string',
            system: 'custom',
        },
        fieldId: 'customfield_10000'
    }];

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService',
            ['getAddonStatus',
                'getCurrentUserData', 'createIssue', 'findProjects', 'getProjects', 'getCreateMetaIssueTypes', 'getCreateMetaFields']);
        const assigneeServiceSpy = jasmine.createSpyObj('AssigneeService', ['searchAssignableMultiProject', 'assigneesToDropdownOptions']);
        const dropdownUtilServiceSpy = jasmine.createSpyObj('DropdownUtilService',
            ['mapProjectToDropdownOption', 'mapIssueTypeToDropdownOption', 'mapPriorityToDropdownOption']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['logNavigation', 'trackException']);
        const utilServiceSpy = jasmine.createSpyObj('UtilService', ['isAddonUpdated', 'getUpgradeAddonMessage']);
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['getHttpErrorMessage']);
        const permissionServiceSpy = jasmine.createSpyObj('PermissionService', ['getMyPermissions']);
        const fieldsServiceSpy = jasmine.createSpyObj('FieldsService',
            ['getAllowedFields', 'getAllowedTransformedFields', 'getCustomFieldTemplates']);
        const notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['notifyError', 'notifySuccess']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendScreenEvent', 'sendUiEvent']);

        await TestBed.configureTestingModule({
            declarations: [CreateIssueDialogComponent],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: AssigneeService, useValue: assigneeServiceSpy },
                { provide: DropdownUtilService, useValue: dropdownUtilServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: UtilService, useValue: utilServiceSpy },
                { provide: ErrorService, useValue: errorServiceSpy },
                { provide: PermissionService, useValue: permissionServiceSpy },
                { provide: FieldsService, useValue: fieldsServiceSpy },
                { provide: NotificationService, useValue: notificationServiceSpy },
                { provide: AnalyticsService, useValue: analyticsServiceSpy },
                { provide: ActivatedRoute, useValue: { snapshot: { params: {} } } },
                { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } },
                { provide: MatDialog, useValue: {} },
                {
                    provide: TeamsService,
                    useValue: {
                        initialize: jasmine.createSpy('initialize').and.returnValue(Promise.resolve()),
                        notifySuccess: jasmine.createSpy('notifySuccess'),
                        submitDialog: jasmine.createSpy('submitDialog')
                    }
                }]
        }).compileComponents();

        fixture = TestBed.createComponent(CreateIssueDialogComponent);
        component = fixture.componentInstance;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        assigneeService = TestBed.inject(AssigneeService) as jasmine.SpyObj<AssigneeService>;
        dropdownUtilService = TestBed.inject(DropdownUtilService) as jasmine.SpyObj<DropdownUtilService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        utilService = TestBed.inject(UtilService) as jasmine.SpyObj<UtilService>;
        errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;
        permissionService = TestBed.inject(PermissionService) as jasmine.SpyObj<PermissionService>;
        fieldsService = TestBed.inject(FieldsService) as jasmine.SpyObj<FieldsService>;
        notificationService = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;
        analyticsService = TestBed.inject(AnalyticsService) as jasmine.SpyObj<AnalyticsService>;
        route = TestBed.inject(ActivatedRoute);
        router = TestBed.inject(Router);
        dialog = TestBed.inject(MatDialog);
        teamsService = TestBed.inject(TeamsService) as jasmine.SpyObj<TeamsService>;
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(CreateIssueDialogComponent);
        component = fixture.componentInstance;
        component.assigneesDropdown = { filteredOptions: [] } as any;
        component.issueForm = new UntypedFormGroup({
            assignee: new UntypedFormControl()
        });
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize correctly', async () => {
        apiService.getAddonStatus.and.returnValue(Promise.resolve({ addonStatus: 1, addonVersion: '1.0.0' }));
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                CREATE_ISSUES: { havePermission: true },
            }
        }) as Promise<JiraPermissionsResponse>);
        apiService.getCurrentUserData.and.returnValue(Promise.resolve({
            name: 'Test User',
            accountId: 'testUser',
            jiraServerInstanceUrl: 'http://example.com',
            self: 'http://example.com/self',
            emailAddress: 'test@example.com',
            hashedEmailAddress: 'hashedEmail',
            avatarUrls: {
                '16x16': 'http://example.com/16x16.png',
                '24x24': 'http://example.com/24x24.png',
                '32x32': 'http://example.com/32x32.png',
                '48x48': 'http://example.com/48x48.png'
            },
            displayName: 'Test User',
            active: true,
            timeZone: 'UTC',
            locale: 'en_US'
        }));
        apiService.getProjects.and.returnValue(Promise.resolve([]));
        await component.ngOnInit();
        expect(appInsightsService.logNavigation).toHaveBeenCalled();
        expect(apiService.getAddonStatus).toHaveBeenCalled();
        expect(apiService.getCurrentUserData).toHaveBeenCalled();
    });

    it('should handle form submission', async () => {
        component.issueForm = { invalid: false, value: {} } as any;
        fieldsService.getAllowedTransformedFields.and.returnValue({});
        apiService.createIssue.and.returnValue(Promise.resolve({
            isSuccess: true, content: { expand: '', id: '', self: '', key: '', fields: {} } as Issue }));

        await component.onSubmit();
        expect(apiService.createIssue).toHaveBeenCalled();
    });

    it('should handle project selection', async () => {
        fieldsService.getAllowedFields.and.returnValue(fieldMeta);
        apiService.getCreateMetaIssueTypes.and.returnValue(Promise.resolve([]));
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                CREATE_ISSUES: { havePermission: true },
            }
        }) as Promise<JiraPermissionsResponse>);
        await component.onProjectSelected('projectId');
        expect(apiService.getCreateMetaIssueTypes).toHaveBeenCalled();
    });

    it('should handle issue type selection', async () => {
        fieldsService.getAllowedFields.and.returnValue(fieldMeta);
        apiService.getCreateMetaFields.and.returnValue(Promise.resolve([]));
        assigneeService.searchAssignableMultiProject.and.returnValue(Promise.resolve([]));
        await component.onIssueTypeSelected('issueTypeId');
        expect(apiService.getCreateMetaFields).toHaveBeenCalled();
    });

    it('should handle assignee search change', async () => {
        component.assigneesDropdown = { filteredOptions: ['assignee'] } as any;
        assigneeService.searchAssignableMultiProject.and.returnValue(Promise.resolve([]));
        await component.onAssigneeSearchChanged('username');
        expect(assigneeService.searchAssignableMultiProject).toHaveBeenCalled();
    });

    it('should assign to current user', () => {
        component.currentUserAccountId = 'testUser';
        const setValueSpy = jasmine.createSpy('setValue');
        component.issueForm = new UntypedFormGroup({
            assignee: new UntypedFormControl()
        });
        component.issueForm.get('assignee').setValue = setValueSpy; // Spy on the setValue method
        component.assignToMe();
        expect(setValueSpy).toHaveBeenCalledWith('testUser');
    });

    it('should handle search change', async () => {
        const projects = [{ id: 'testProjectId', key: 'testProjectKey', name: 'Test Project' }];
        apiService.findProjects.and.returnValue(Promise.resolve(projects as Project[]));
        await component.onSearchChanged('test');
        expect(apiService.findProjects).toHaveBeenCalled();
    });

    it('should determine if user is assignable', () => {
        component.assigneesOptions = [{ value: 'testUser' }];
        component.currentUserAccountId = 'testUser';
        expect(component.isAssignableUser).toBeTrue();

        component.assigneesOptions = [{ value: 'anotherUser' }];
        expect(component.isAssignableUser).toBeFalse();
    });

    it('should determine if field is required', () => {
        component.fields = { testField: { required: true } };
        expect(component.isFieldRequired('testField')).toBeTrue();

        component.fields = { testField: { required: false } };
        expect(component.isFieldRequired('testField')).toBeFalse();
    });

    it('should open snack bar', () => {
        component['openSnackBar']();
        expect(notificationService.notifyError).toHaveBeenCalledWith(utilService.getUpgradeAddonMessage());
    });

    it('should get default issue type', () => {
        component.defaultIssueType = 'Bug';
        component.availableIssueTypesOptions = [{ label: 'Bug', value: 'bug' }];
        expect(component['getDefaultIssueType']()).toEqual({ label: 'Bug', value: 'bug' });

        component.defaultIssueType = undefined;
        component.availableIssueTypesOptions = [{ label: 'Bug', value: 'bug' }];
        expect(component['getDefaultIssueType']()).toEqual('bug');
    });

    it('should get default assignee', () => {
        component.defaultAssignee = 'testUser';
        component.assigneesOptions = [{ label: 'testUser', value: 'testUser' }];
        expect(component['getDefaultAssignee']()).toEqual({ label: 'testUser', value: 'testUser' });

        component.defaultAssignee = undefined;
        component.assigneesOptions = [{ label: 'testUser', value: 'testUser' }];
        expect(component['getDefaultAssignee']()).toEqual('testUser');
    });

    it('should find projects', async () => {
        const projects = [{ id: 'testProjectId', key: 'testProjectKey', name: 'Test Project' }] as Project[];
        apiService.findProjects.and.returnValue(Promise.resolve(projects as Project[]));
        const result = await component['findProjects']('http://example.com', 'test');
        expect(apiService.findProjects).toHaveBeenCalledWith('http://example.com', 'test', true);
        expect(result).toEqual(projects);
    });

    it('should remove priority control from form when priorities are not available', () => {
        component.fields = {};
        component.issueForm = new UntypedFormGroup({
            priority: new UntypedFormControl()
        });
        component['addRemovePriorityFromForm']();
        expect(component.issueForm.contains('priority')).toBeFalse();
        expect(component.prioritiesOptions.length).toBe(0);
    });

    it('should create form and initialize correctly when projects are available', async () => {
        const projects = [{ id: 'testProjectId', key: 'testProjectKey', name: 'Test Project' }] as Project[];
        apiService.getProjects.and.returnValue(Promise.resolve(projects));
        dropdownUtilService.mapProjectToDropdownOption.and.returnValue(
            { value: 'testProjectId', label: 'Test Project', id: 'testProjectId' });
        spyOn(component, 'onProjectSelected').and.returnValue(Promise.resolve());
        fieldsService.getAllowedFields.and.returnValue(fieldMeta);
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                CREATE_ISSUES: { havePermission: true },
            }
        }) as Promise<JiraPermissionsResponse>);

        await component['createForm']();

        expect(apiService.getProjects).toHaveBeenCalled();
        expect(component.projects).toEqual(projects);
        expect(component.onProjectSelected).toHaveBeenCalledWith('testProjectId');
        expect(component.issueForm).toBeDefined();
        expect(component.issueForm.get('project').value).toEqual('testProjectId');
        expect(component.issueForm.get('summary').value).toEqual('');
    });

    it('should navigate to error page when no projects are available', async () => {
        apiService.getProjects.and.returnValue(Promise.resolve([]));
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                CREATE_ISSUES: { havePermission: true },
            }
        }) as Promise<JiraPermissionsResponse>);

        await component['createForm']();

        expect(apiService.getProjects).toHaveBeenCalled();
        expect(component.projects).toEqual([]);
        expect(component['router'].navigate).toHaveBeenCalledWith(['/error'],
            { queryParams: { message: 'You don\'t have permission to perform this action' } });
    });

    it('should add and remove priority control based on availability', async () => {
        const projects = [{ id: 'testProjectId', key: 'testProjectKey', name: 'Test Project' }] as Project[];
        apiService.getProjects.and.returnValue(Promise.resolve(projects));
        dropdownUtilService.mapProjectToDropdownOption.and.returnValue(
            { value: 'testProjectId', label: 'Test Project', id: 'testProjectId' });
        spyOn(component, 'onProjectSelected').and.returnValue(Promise.resolve());
        fieldsService.getAllowedFields.and.returnValue(fieldMeta);

        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                CREATE_ISSUES: { havePermission: true },
            }
        }) as Promise<JiraPermissionsResponse>);


        await component['createForm']();

        expect(fieldsService.getAllowedFields).toHaveBeenCalled();
    });
});
