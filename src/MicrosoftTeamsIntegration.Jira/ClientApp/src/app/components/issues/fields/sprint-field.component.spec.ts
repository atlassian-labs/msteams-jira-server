import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SprintFieldComponent } from './sprint-field.component';
import { ApiService } from '@core/services';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { UntypedFormGroup, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { JiraIssueSprint } from '@core/models/Jira/jira-issue-sprint.model';

describe('SprintFieldComponent', () => {
    let component: SprintFieldComponent;
    let fixture: ComponentFixture<SprintFieldComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;
    const sprint: JiraIssueSprint = {
        id: '1', name: 'Sample Sprint 1', self: '', state: ''
    };

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService', ['getSprints']);
        const dropdownUtilServiceSpy = jasmine.createSpyObj('DropdownUtilService', ['mapSprintDataToSelectOption']);

        await TestBed.configureTestingModule({
            declarations: [SprintFieldComponent],
            imports: [ReactiveFormsModule, NgSelectModule],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: DropdownUtilService, useValue: dropdownUtilServiceSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(SprintFieldComponent);
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
            projectKeyOrId: 'TEST',
            defaultValue: ['com.atlassian.greenhopper.service.sprint.Sprint@716d6522[name=Sample Sprint 1]']
        };
        component.formGroup = new UntypedFormGroup({});
        apiService.getSprints.and.returnValue(Promise.resolve([]));
        dropdownUtilService.mapSprintDataToSelectOption.and.returnValue({ id: '1', name: 'Sample Sprint 1' });

        await component.ngOnInit();

        expect(component.jiraUrl).toBe('http://example.com');
        expect(component.projectKeyOrId).toBe('TEST');
        expect(apiService.getSprints).toHaveBeenCalled();
    });

    it('should load sprints on open', async () => {
        component.data = {
            jiraUrl: 'http://example.com',
            projectKeyOrId: 'TEST'
        };
        component.formGroup = new UntypedFormGroup({});
        apiService.getSprints.and.returnValue(Promise.resolve([sprint]));
        dropdownUtilService.mapSprintDataToSelectOption.and.returnValue({ id: '1', name: 'Epic 1' });

        await component.onOpen();

        expect(apiService.getSprints).toHaveBeenCalled();
        expect(dropdownUtilService.mapSprintDataToSelectOption).toHaveBeenCalled();
        expect(component.sprintOptions).toEqual([{ id: '1', name: 'Epic 1' }]);
    });

    it('should not load sprints if already initialized', async () => {
        component.dataInitialized = true;
        component.data = {
            jiraUrl: 'http://example.com',
            projectKeyOrId: 'TEST'
        };
        component.formGroup = new UntypedFormGroup({});
        apiService.getSprints.and.returnValue(Promise.resolve([sprint]));
        dropdownUtilService.mapSprintDataToSelectOption.and.returnValue({ id: '1', name: 'Sample Sprint 1' });

        await component.onOpen();

        expect(apiService.getSprints).not.toHaveBeenCalled();
        expect(dropdownUtilService.mapSprintDataToSelectOption).not.toHaveBeenCalled();
    });
});
