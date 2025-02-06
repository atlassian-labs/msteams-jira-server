import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EditIssueDialogComponent } from './edit-issue-dialog.component';
import { ApiService, AppInsightsService, ErrorService, UtilService } from '@core/services';
import { AssigneeService } from '@core/services/entities/assignee.service';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { FieldsService } from '@shared/services/fields.service';
import { NotificationService } from '@shared/services/notificationService';
import { AnalyticsService } from '@core/services/analytics.service';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { PermissionService } from '@core/services/entities/permission.service';
import { IssueTransitionService } from '@core/services/entities/transition.service';
import { UntypedFormGroup, UntypedFormControl, ReactiveFormsModule } from '@angular/forms';
import * as microsoftTeams from '@microsoft/teams-js';
import { Issue, JiraComment, Priority, ProjectType } from '@core/models';
import { JiraPermission, JiraPermissions, JiraPermissionsResponse } from '@core/models/Jira/jira-permission.model';
import { EditIssueMetadata, EditIssueMetadataFields } from '@core/models/Jira/jira-issue-edit-meta.model';
import { JiraIssueFieldMeta } from '@core/models/Jira/jira-issue-field-meta.model';
import { JiraIssueTypeFieldMetaSchema } from '@core/models/Jira/jira-issue-type-field-meta-schema.model';
import { JiraUser, UserGroup } from '@core/models/Jira/jira-user.model';
import { DomSanitizer } from '@angular/platform-browser';
import { JiraIconUrls } from '@core/models/Jira/jira-avatar-urls.model';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { JiraTransition, JiraTransitionsResponse } from '@core/models/Jira/jira-transition.model';

describe('EditIssueDialogComponent', () => {
    let component: EditIssueDialogComponent;
    let fixture: ComponentFixture<EditIssueDialogComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let assigneeService: jasmine.SpyObj<AssigneeService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let permissionService: jasmine.SpyObj<PermissionService>;
    let fieldsService: jasmine.SpyObj<FieldsService>;
    let notificationService: jasmine.SpyObj<NotificationService>;
    let transitionService: jasmine.SpyObj<IssueTransitionService>;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;
    const issueScheme: JiraIssueTypeFieldMetaSchema = { type: 'test type', items: 'test items', system: 'test system' };
    const stringFieldMetadata: JiraIssueFieldMeta<string> = {
        allowedValues: [],
        defaultValue: [],
        hasDefaultValue: true,
        key: 'test key', name: 'test name', operations: [], required: true, schema: issueScheme, fieldId: 'test field id' };
    const nullFieldMetadata: JiraIssueFieldMeta<null> = {
        allowedValues: [],
        defaultValue: [],
        hasDefaultValue: true,
        key: 'test key', name: 'test name', operations: [], required: true, schema: issueScheme, fieldId: 'test field id' };
    const priorityFieldMetadata: JiraIssueFieldMeta<Priority> = {
        allowedValues: [],
        defaultValue: [ { id: '1', name: 'High', iconUrl: 'http://example.com/icon.png', statusColor: 'blue' } ],
        hasDefaultValue: true,
        key: 'test key',
        name: 'test name', operations: [], required: true, schema: issueScheme, fieldId: 'test field id' };
    const editIssueMetadata: EditIssueMetadata = { fields: {
        assignee: stringFieldMetadata,
        attachment: stringFieldMetadata,
        comment: nullFieldMetadata,
        components: stringFieldMetadata,
        description: stringFieldMetadata,
        issuelinks: stringFieldMetadata,
        issuetype: stringFieldMetadata,
        labels: stringFieldMetadata,
        priority: priorityFieldMetadata,
        status: stringFieldMetadata,
        summary: stringFieldMetadata
    } };
    const permission: JiraPermission = {
        id: 'testId', key: 'testKey', name: 'testName', type: 'testType', description: 'testDescription', havePermission: true
    };
    const permissions: JiraPermissions = {
        TRANSITION_ISSUES: permission,
        ADD_COMMENTS: permission,
        EDIT_ALL_COMMENTS: permission,
        EDIT_OWN_COMMENTS: permission,
        DELETE_ALL_COMMENTS: permission,
        DELETE_OWN_COMMENTS: permission,
        ASSIGNABLE_USER: permission,
        ASSIGN_ISSUES: permission,
        CLOSE_ISSUES: permission,
        CREATE_ISSUES: permission,
        DELETE_ISSUES: permission,
        EDIT_ISSUES: permission,
        LINK_ISSUES: permission,
        MODIFY_REPORTER: permission,
        MOVE_ISSUES: permission,
        RESOLVE_ISSUES: permission,
        SCHEDULE_ISSUES: permission,
        SET_ISSUE_SECURITY: permission,
        BROWSE: permission
    };

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService',
            ['getJiraUrlForPersonalScope',
                'getIssueByIdOrKey',
                'getEditIssueMetadata',
                'getCurrentUserData',
                'updateIssue', 'getCreateMetaIssueTypes', 'getCreateMetaFields', 'getCreateMetaIssueTypes']);
        const assigneeServiceSpy = jasmine.createSpyObj('AssigneeService', ['searchAssignable', 'assigneesToDropdownOptions']);
        const dropdownUtilServiceSpy = jasmine.createSpyObj('DropdownUtilService',
            ['mapPriorityToDropdownOption', 'mapTransitionToDropdownOption']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['logNavigation', 'trackException']);
        const utilServiceSpy = jasmine.createSpyObj('UtilService', ['isAddonUpdated', 'getUpgradeAddonMessage']);
        const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['getHttpErrorMessage']);
        const permissionServiceSpy = jasmine.createSpyObj('PermissionService', ['getMyPermissions']);
        const fieldsServiceSpy = jasmine.createSpyObj('FieldsService',
            ['getAllowedFields', 'getAllowedTransformedFields', 'getCustomFieldTemplates', 'getDefaultValue']);
        const notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['notifyError', 'notifySuccess']);
        const analyticsServiceSpy = jasmine.createSpyObj('AnalyticsService', ['sendScreenEvent', 'sendUiEvent']);
        const transitionServiceSpy = jasmine.createSpyObj('IssueTransitionService', ['getTransitions']);

        await TestBed.configureTestingModule({
            declarations: [EditIssueDialogComponent],
            imports: [ReactiveFormsModule],
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
                { provide: IssueTransitionService, useValue: transitionServiceSpy },
                { provide: ActivatedRoute, useValue: { snapshot: { params: {} } } },
                { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } },
                { provide: MatDialog, useValue: {} }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(EditIssueDialogComponent);
        component = fixture.componentInstance;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        assigneeService = TestBed.inject(AssigneeService) as jasmine.SpyObj<AssigneeService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        permissionService = TestBed.inject(PermissionService) as jasmine.SpyObj<PermissionService>;
        fieldsService = TestBed.inject(FieldsService) as jasmine.SpyObj<FieldsService>;
        notificationService = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;
        transitionService = TestBed.inject(IssueTransitionService) as jasmine.SpyObj<IssueTransitionService>;
        dropdownUtilService = TestBed.inject(DropdownUtilService) as jasmine.SpyObj<DropdownUtilService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize correctly', async () => {
        apiService.getJiraUrlForPersonalScope.and.returnValue(Promise.resolve({ jiraUrl: 'http://example.com' }));
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                EDIT_ISSUES: { havePermission: true },
                BROWSE: { havePermission: true }
            }
        }) as Promise<JiraPermissionsResponse>);
        apiService.getIssueByIdOrKey.and.returnValue(Promise.resolve({ expand: '', id: '', self: '', key: '', fields: {} } as Issue));
        apiService.getEditIssueMetadata.and.returnValue(Promise.resolve({ fields: {} } as EditIssueMetadata));

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
        }));

        await component.ngOnInit();

        expect(appInsightsService.logNavigation).toHaveBeenCalled();
        expect(apiService.getJiraUrlForPersonalScope).toHaveBeenCalled();
        expect(permissionService.getMyPermissions).toHaveBeenCalled();
        expect(apiService.getIssueByIdOrKey).toHaveBeenCalled();
        expect(apiService.getEditIssueMetadata).toHaveBeenCalled();
        expect(apiService.getCurrentUserData).toHaveBeenCalled();
    });

    it('should handle form submission successfully', async () => {
        component.issueForm = new UntypedFormGroup({
            summary: new UntypedFormControl('test summary'),
            description: new UntypedFormControl('test description'),
            priorityId: new UntypedFormControl('1'),
            assigneeAccountId: new UntypedFormControl('testUser')
        });
        component.issue = { id: 'testIssueId' };
        component.updatedFormFields = ['summary', 'description', 'priorityId', 'assigneeAccountId'];
        component.fields = { assignee: { assigneeAccountId: 'test value' } };
        apiService.updateIssue.and.returnValue(Promise.resolve({ isSuccess: true }));
        fieldsService.getAllowedTransformedFields.and.returnValue({ } as Partial<any>);

        apiService.getEditIssueMetadata.and.returnValue(Promise.resolve(editIssueMetadata));
        apiService.getJiraUrlForPersonalScope.and.returnValue(Promise.resolve({ jiraUrl: 'http://example.com' }));
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                EDIT_ISSUES: { havePermission: true },
                BROWSE: { havePermission: true }
            }
        }) as Promise<JiraPermissionsResponse>);
        apiService.getIssueByIdOrKey.and.returnValue(Promise.resolve({ expand: '', id: '', self: '', key: '', fields: {} } as Issue));

        await component.ngOnInit();
        await component.onSubmit();

        expect(apiService.updateIssue).toHaveBeenCalled();
        expect(notificationService.notifySuccess).toHaveBeenCalled();
    });

    it('should handle form submission failure', async () => {
        component.issueForm = new UntypedFormGroup({
            summary: new UntypedFormControl('test summary'),
            description: new UntypedFormControl('test description'),
            priorityId: new UntypedFormControl('1'),
            assigneeAccountId: new UntypedFormControl('testUser')
        });
        component.issue = { id: 'testIssueId' };
        component.updatedFormFields = ['summary', 'description', 'priorityId', 'assigneeAccountId'];
        apiService.updateIssue.and.returnValue(Promise.reject(new Error('error')));
        apiService.getEditIssueMetadata.and.returnValue(Promise.resolve(editIssueMetadata));
        component.fields = { assignee: { assigneeAccountId: 'test value' } };
        fieldsService.getAllowedTransformedFields.and.returnValue({ } as Partial<any>);
        apiService.getJiraUrlForPersonalScope.and.returnValue(Promise.resolve({ jiraUrl: 'http://example.com' }));
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                EDIT_ISSUES: { havePermission: true },
                BROWSE: { havePermission: true }
            }
        }) as Promise<JiraPermissionsResponse>);

        await component.ngOnInit();

        try {
            await component.onSubmit();
        } catch (error) {
            // empty catch
        }

        expect(apiService.updateIssue).toHaveBeenCalled();
        expect(notificationService.notifyError).toHaveBeenCalled();
    });

    it('should assign to current user', () => {
        component.currentUserAccountId = 'testUser';
        const setValueSpy = jasmine.createSpy('setValue');
        component.issueForm = new UntypedFormGroup({
            assigneeAccountId: new UntypedFormControl('test value')
        });
        component.initialIssueForm = new UntypedFormGroup({
            assigneeAccountId: new UntypedFormControl('test value')
        });
        component.issueRaw = { fields: { assignee: { assigneeAccountId: 'test value' } } };
        component.issueForm.get('assigneeAccountId').setValue = setValueSpy;
        component.assignToMe();
        expect(setValueSpy).toHaveBeenCalledWith('testUser');
    });

    it('should handle assignee search change', async () => {
        assigneeService.searchAssignable.and.returnValue(Promise.resolve([]));
        await component.onAssigneeSearchChanged('username');
        expect(assigneeService.searchAssignable).toHaveBeenCalled();
    });

    it('should handle new comment creation', () => {
        const comment = { id: 'testCommentId' } as JiraComment;
        component.issue = { comment: { comments: [] } };
        component.onNewCommentCreated(comment);
        expect(component.issue.comment.comments).toContain(comment);
    });

    it('should handle cancel', () => {
        spyOn(microsoftTeams.dialog.url, 'submit');
        component.onCancel();
        expect(microsoftTeams.dialog.url.submit).toHaveBeenCalled();
    });

    it('should determine if user can view issue', () => {
        permissions.BROWSE.havePermission = true;
        component.permissions = permissions;
        expect(component.canViewIssue).toBeTrue();

        permissions.BROWSE.havePermission = false;
        component.permissions = permissions;
        expect(component.canViewIssue).toBeFalse();
    });

    it('should determine if user can edit summary', () => {
        spyOn(component, 'canEditField').and.returnValue(true);
        permissions.EDIT_ISSUES.havePermission = true;
        component.permissions = permissions;
        expect(component.allowEditSummary).toBeTrue();

        permissions.EDIT_ISSUES.havePermission = false;
        component.permissions = permissions;
        expect(component.allowEditSummary).toBeFalse();
    });

    it('should determine if user can edit description', () => {
        spyOn(component, 'canEditField').and.returnValue(true);
        permissions.EDIT_ISSUES.havePermission = true;
        component.permissions = permissions;
        expect(component.allowEditDescription).toBeTrue();

        permissions.EDIT_ISSUES.havePermission = false;
        component.permissions = permissions;
        expect(component.allowEditDescription).toBeFalse();
    });

    it('should determine if user can edit priority', () => {
        spyOn(component, 'canEditField').and.returnValue(true);
        permissions.EDIT_ISSUES.havePermission = true;
        component.permissions = permissions;
        expect(component.allowEditPriority).toBeTrue();

        permissions.EDIT_ISSUES.havePermission = false;
        component.permissions = permissions;
        expect(component.allowEditPriority).toBeFalse();
    });

    it('should determine if user can edit assignee', () => {
        component.issue = { projectTypeKey: ProjectType.ServiceDesk };
        component.currentUser = { groups: { items: [{ name: UserGroup.JiraServicedeskUsers }] } };
        permissions.ASSIGN_ISSUES.havePermission = true;
        component.permissions = permissions;
        expect(component.allowEditAssignee).toBeTrue();

        component.issue = { projectTypeKey: ProjectType.ServiceDesk };
        component.currentUser = { groups: { items: [{ name: UserGroup.JiraSoftwareUsers }] } };
        permissions.ASSIGN_ISSUES.havePermission = true;
        component.permissions = permissions;
        expect(component.allowEditAssignee).toBeFalse();

        component.issue = { projectTypeKey: ProjectType.Software };
        permissions.ASSIGN_ISSUES.havePermission = true;
        component.permissions = permissions;
        expect(component.allowEditAssignee).toBeFalse();
    });

    it('should determine if user can edit status', () => {

        permissions.TRANSITION_ISSUES.havePermission = true;
        component.permissions = permissions;
        expect(component.allowEditStatus).toBeTrue();

        permissions.TRANSITION_ISSUES.havePermission = false;
        component.permissions = permissions;
        expect(component.allowEditStatus).toBeFalse();
    });

    it('should determine if sprint value is set', () => {
        const normalizedRawValue =
        // eslint-disable-next-line max-len
        '["com.atlassian.greenhopper.service.sprint.Sprint@716d6522[activatedDate=2024-10-30T02:13:17.165+02:00,autoStartStop=false,completeDate=<null>,endDate=2024-11-13T02:33:17.165+02:00,goal=<null>,id=1,incompleteIssuesDestinationId=<null>,name=Sample Sprint 2,rapidViewId=1,sequence=1,startDate=2024-10-30T02:13:17.165+02:00,state=ACTIVE,synced=false]"]';
        const normalizedCurrentValue = '1';
        const result = component['isSprintValueSet'](normalizedRawValue, normalizedCurrentValue);
        expect(result).toBeTrue();
    });

    it('should determine if sprint value is not set', () => {
        const normalizedRawValue = '[{"id=1"}]';
        const normalizedCurrentValue = '2';
        const result = component['isSprintValueSet'](normalizedRawValue, normalizedCurrentValue);
        expect(result).toBeFalse();
    });

    it('should normalize value', () => {
        const value = { key: 'value' };
        const result = component['normalizeValue'](value);
        expect(result).toBe(JSON.stringify(value));
    });

    it('should return null for undefined value', () => {
        const value = undefined;
        const result = component['normalizeValue'](value);
        expect(result).toBeNull();
    });

    it('should sanitize URL', () => {
        const url = 'https://example.com';
        const sanitizedUrl = component.sanitazeUrl(url);
        const expectedSanitizedUrl = TestBed.inject(DomSanitizer).bypassSecurityTrustUrl(url);
        expect(sanitizedUrl).toEqual(expectedSanitizedUrl);
    });

    it('should create form', async () => {
        const issue: Issue = {
            expand: '',
            self: '',
            key: 'testKey',
            id: 'testId',
            fields: {
                key: 'testKey',
                issuetype: { id: 'testId', description: 'testDescription', name: 'testName', iconUrl: 'testIconUrl' },
                project: {
                    id: 'testProjectId',
                    key: 'testProjectKey',
                    name: 'testProjectName',
                    issueTypes: [],
                    avatarUrls: {
                        '16x16': 'http://example.com/16x16.png',
                        '24x24': 'http://example.com/24x24.png',
                        '32x32': 'http://example.com/32x32.png',
                        '48x48': 'http://example.com/48x48.png'
                    },
                    simplified: false,
                    projectTypeKey: ProjectType.Software
                },
                priority: { id: '1', name: 'High', iconUrl: 'http://example.com/icon.png', statusColor: 'blue' },
                summary: 'test summary',
                description: 'test description',
                created: '2023-10-01T00:00:00.000Z',
                updated: '2023-10-02T00:00:00.000Z',
                reporter: {
                    name: 'testUser',
                    self: 'http://example.com/self',
                    accountId: 'testAccountId',
                    emailAddress: 'test@example.com',
                    hashedEmailAddress: 'hashedEmail',
                    displayName: 'Test Reporter',
                    active: true,
                    timeZone: 'UTC',
                    avatarUrls: {
                        '16x16': 'http://example.com/16x16.png',
                        '24x24': 'http://example.com/24x24.png',
                        '32x32': 'http://example.com/32x32.png',
                        '48x48': 'http://example.com/48x48.png'
                    }
                },
                assignee: {
                    name: 'testUser',
                    self: 'http://example.com/self',
                    accountId: 'testAccountId',
                    emailAddress: 'test@example.com',
                    hashedEmailAddress: 'hashedEmail',
                    displayName: 'Test Reporter',
                    active: true,
                    timeZone: 'UTC',
                    avatarUrls: {
                        '16x16': 'http://example.com/16x16.png',
                        '24x24': 'http://example.com/24x24.png',
                        '32x32': 'http://example.com/32x32.png',
                        '48x48': 'http://example.com/48x48.png'
                    }
                },
                creator: {
                    name: 'testUser',
                    self: 'http://example.com/self',
                    accountId: 'testAccountId',
                    emailAddress: 'test@example.com',
                    hashedEmailAddress: 'hashedEmail',
                    displayName: 'Test Reporter',
                    active: true,
                    timeZone: 'UTC',
                    avatarUrls: {
                        '16x16': 'http://example.com/16x16.png',
                        '24x24': 'http://example.com/24x24.png',
                        '32x32': 'http://example.com/32x32.png',
                        '48x48': 'http://example.com/48x48.png'
                    }
                },
                comment: { comments: [], maxResults: 0, total: 0, startAt: 0 },
                status: { id: '1', name: 'Open', description: 'Issue is open', iconUrl: 'http://example.com/icon.png' },
            } as any
        };

        component.issue = issue;
        component.fields = { summary: stringFieldMetadata, description: stringFieldMetadata };
        await component['createForm'];
        apiService.getJiraUrlForPersonalScope.and.returnValue(Promise.resolve({ jiraUrl: 'http://example.com' }));
        permissionService.getMyPermissions.and.returnValue(Promise.resolve({
            permissions: {
                EDIT_ISSUES: { havePermission: true },
                BROWSE: { havePermission: false },
                ASSIGN_ISSUES: { havePermission: false },
                TRANSITION_ISSUES: { havePermission: false }
            }
        }) as Promise<JiraPermissionsResponse>);
        apiService.getIssueByIdOrKey.and.returnValue(Promise.resolve(issue));
        const editIssueMetadataFields: EditIssueMetadataFields = {
            assignee: stringFieldMetadata,
            attachment: stringFieldMetadata,
            comment: nullFieldMetadata,
            components: stringFieldMetadata,
            description: stringFieldMetadata,
            issuelinks: stringFieldMetadata,
            issuetype: stringFieldMetadata,
            labels: stringFieldMetadata,
            priority: priorityFieldMetadata,
            status: stringFieldMetadata,
            summary: stringFieldMetadata };
        apiService.getEditIssueMetadata.and.returnValue(Promise.resolve({ fields: editIssueMetadataFields } as EditIssueMetadata));

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

        const jiraIssueMeta: JiraIssueFieldMeta<any> = {
            allowedValues: [],
            defaultValue: [],
            hasDefaultValue: true,
            key: 'test key',
            name: 'test name',
            operations: [],
            required: true,
            schema: issueScheme,
            fieldId: 'test field id'
        };
        const jiraIssueMetas: JiraIssueFieldMeta<any>[] = [jiraIssueMeta];
        apiService.getCreateMetaIssueTypes.and.returnValue(Promise.resolve(jiraIssueMetas) as any);

        await component.ngOnInit();

        expect(component.issueForm).toBeDefined();
        expect(component.issueForm.get('summary').value).toBe('test summary');
        expect(component.issueForm.get('description').value).toBe('test description');
    });

    it('should add and remove control from form', () => {
        component.issueForm = new UntypedFormGroup({});
        component.fields = { testField: stringFieldMetadata };

        component['addRemoveControlFromForm']('testField');
        expect(component.issueForm.contains('testField')).toBeTrue();

        component.issueForm = new UntypedFormGroup({});
        component.fields = {};
        component['addRemoveControlFromForm']('testField');
        expect(component.issueForm.contains('testField')).toBeFalse();
    });

    it('should determine if field is required', () => {
        component.fields = { testField: stringFieldMetadata };
        expect(component.isFieldRequired('testField')).toBeTrue();

        stringFieldMetadata.required = false;
        component.fields = { testField: stringFieldMetadata };
        expect(component.isFieldRequired('testField')).toBeFalse();
    });

    it('should set assignee options correctly', async () => {
        const assignees = [
            { accountId: '1', name: 'user1', displayName: 'User One',
                avatarUrls:  { '24x24': 'http://example.com/1.png', '16x16': '16', '32x32': '32', '48x48': '48' } },
            { accountId: '2', name: 'user2', displayName: 'User Two',
                avatarUrls:  { '24x24': 'http://example.com/1.png', '16x16': '16', '32x32': '32', '48x48': '48' } },
        ];
        const assigneeOptions = assignees.map(assignee => ({
            id: assignee.accountId,
            value: assignee.accountId,
            label: assignee.displayName,
            icon: assignee.avatarUrls['24x24']
        }));
        assigneeService.searchAssignable.and.returnValue(Promise.resolve(assignees as JiraUser[]));
        assigneeService.assigneesToDropdownOptions.and.returnValue(assigneeOptions);

        component.issue = { assignee: assignees[0] };

        await component['setAssigneeOptions']();

        expect(component.assigneesOptions).toEqual(assigneeOptions);
        expect(component.assigneesFilteredOptions).toEqual(assigneeOptions);
        expect(component.selectedAssigneeOption).toEqual(assigneeOptions[0]);
    });

    it('should set unassigned option if assignee is null', async () => {
        const unassignedOption: DropDownOption<string> = {
            id: -2,
            value: null,
            label: 'Unassigned',
            icon: '/assets/useravatar24x24.png'
        };

        assigneeService.searchAssignable.and.returnValue(Promise.resolve([]));
        assigneeService.assigneesToDropdownOptions.and.returnValue([]);

        component.issue = { assignee: null };

        await component['setAssigneeOptions']();

        expect(component.assigneesOptions).toEqual([]);
        expect(component.assigneesFilteredOptions).toEqual([]);
        expect(component.selectedAssigneeOption).toEqual(undefined);
    });

    it('should add not assignable assignee to options', async () => {
        const assignees = [
            { accountId: '1', name: 'user1', displayName: 'User One', avatarUrls: { '24x24': 'http://example.com/1.png' } }
        ];

        assigneeService.searchAssignable.and.returnValue(Promise.resolve([]));
        assigneeService.assigneesToDropdownOptions.and.returnValue([]);

        component.issue = { assignee: assignees[0] };

        await component['setAssigneeOptions']();

        expect(component.assigneesOptions).toContain(jasmine.objectContaining({ value: '1' }));
        expect(component.selectedAssigneeOption).toEqual(jasmine.objectContaining({ value: '1' }));
    });

    it('should set statuses options correctly', async () => {
        const transitions: JiraTransition[] = [
            { id: '1', name: 'To Do', to:
                { id: '1', name: 'To Do', self: '', description: '', iconUrl: '', statusCategory:
                    { id: 1, key: '', colorName: '', name: '' } },
            hasScreen: false, isGlobal: false, isInitial: false, isConditional: false },
            { id: '2', name: 'In Progress', to:
                { id: '2', name: 'In Progress', self: '', description: '', iconUrl: '', statusCategory:
                    { id: 1, key: '', colorName: '', name: '' } },
            hasScreen: false, isGlobal: false, isInitial: false, isConditional: false },
        ];
        const jiraTransitionsResponse: JiraTransitionsResponse = { transitions: transitions, expand: '' };
        const statusOption = {
            id: '1',
            value: { id: '1', name: 'To Do', to:
                { id: '1', name: 'To Do', self: '', description: '', iconUrl: '', statusCategory:
                    { id: 1, key: '', colorName: '', name: '' } },
            hasScreen: false, isGlobal: false, isInitial: false, isConditional: false },
            label: 'To Do'
        } as DropDownOption<JiraTransition>;

        transitionService.getTransitions.and.returnValue(Promise.resolve(jiraTransitionsResponse));
        dropdownUtilService.mapTransitionToDropdownOption.and.callFake((transition: JiraTransition) => ({
            id: transition.id,
            value: transition,
            label: transition.name,
        }));

        component.issue = { status: { id: '1', name: 'To Do' } };

        await component['setStatusesOptions']();

        expect(transitionService.getTransitions).toHaveBeenCalledWith(component.jiraUrl, component.issue.key);
        expect(component.statusesOptions.length).toBe(2);
        expect(component.statusesOptions).toContain(statusOption);
        expect(component.selectedStatusOption).toEqual(statusOption);
    });

    it('should set initial status option if not in transitions', async () => {
        const transitions: JiraTransition[] = [
            { id: '2', name: 'In Progress', to:
                { id: '2', name: 'In Progress', self: '', description: '', iconUrl: '', statusCategory:
                    { id: 1, key: '', colorName: '', name: '' } },
            hasScreen: false, isGlobal: false, isInitial: false, isConditional: false },
        ];
        const jiraTransitionsResponse: JiraTransitionsResponse = { transitions: transitions, expand: '' };
        const statusOption = {
            id: '1',
            value: { id: '1' },
            label: 'To Do'
        } as DropDownOption<JiraTransition>;

        transitionService.getTransitions.and.returnValue(Promise.resolve(jiraTransitionsResponse));
        dropdownUtilService.mapTransitionToDropdownOption.and.callFake((transition: JiraTransition) => ({
            id: transition.id,
            value: transition,
            label: transition.name
        }));

        component.issue = { status: { id: '1', name: 'To Do' } };

        await component['setStatusesOptions']();

        expect(transitionService.getTransitions).toHaveBeenCalledWith(component.jiraUrl, component.issue.key);
        expect(component.statusesOptions.length).toBe(2);
        expect(component.statusesOptions[0]).toEqual(statusOption);
        expect(component.selectedStatusOption).toEqual(statusOption);
    });

    it('should add priority control to form if priorities exist', () => {
        component.issueForm = new UntypedFormGroup({});
        component.fields = { priority: priorityFieldMetadata };
        component.defaultPriority = 'High';
        dropdownUtilService.mapPriorityToDropdownOption.and.callFake((priority: Priority) => ({
            id: priority.id,
            value: priority.id,
            label: priority.name,
            icon: priority.iconUrl
        }));

        component['addRemovePriorityFromForm']();

        expect(component.issueForm.contains('priority')).toBeTrue();
    });

    it('should set default priority value if defaultPriority is not set', () => {
        component.issueForm = new UntypedFormGroup({});
        component.fields = { priority: priorityFieldMetadata };
        component.defaultPriority = null;
        dropdownUtilService.mapPriorityToDropdownOption.and.callFake((priority: Priority) => ({
            id: priority.id,
            value: priority.id,
            label: priority.name,
            icon: priority.iconUrl
        }));

        component['addRemovePriorityFromForm']();

        expect(component.issueForm.contains('priority')).toBeTrue();
    });

    it('should remove priority control from form if priorities do not exist', () => {
        component.issueForm = new UntypedFormGroup({
            priority: new UntypedFormControl('1')
        });
        component.fields = {};

        component['addRemovePriorityFromForm']();

        expect(component.issueForm.contains('priority')).toBeFalse();
        expect(component.prioritiesOptions.length).toBe(0);
    });

    it('should handle case when priorities have default value', () => {
        component.issueForm = new UntypedFormGroup({});
        component.fields = { priority: { ...priorityFieldMetadata, hasDefaultValue: true, defaultValue: { id: '2', name: 'Medium' } } };
        dropdownUtilService.mapPriorityToDropdownOption.and.callFake((priority: Priority) => ({
            id: priority.id,
            value: priority.id,
            label: priority.name,
            icon: priority.iconUrl
        }));

        component['addRemovePriorityFromForm']();

        expect(component.issueForm.contains('priority')).toBeTrue();
        expect(component.issueForm.get('priority').value).toBe('2');
    });

    it('should handle case when priorities do not have default value', () => {
        const defaultPriority: Priority = { id: '2', name: 'Medium', iconUrl: 'http://example.com/icon.png', statusColor: 'blue' };
        component.defaultPriority = defaultPriority;
        component.issueForm = new UntypedFormGroup({});
        component.fields = { priority: { ...priorityFieldMetadata, hasDefaultValue: false } };
        dropdownUtilService.mapPriorityToDropdownOption.and.callFake((priority: Priority) => ({
            id: priority.id,
            value: priority.id,
            label: priority.name,
            icon: priority.iconUrl
        }));

        component['addRemovePriorityFromForm']();

        expect(component.issueForm.contains('priority')).toBeTrue();
    });
});
