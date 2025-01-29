import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EpicFieldComponent } from './epic-field.component';
import { ApiService } from '@core/services';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { UntypedFormGroup, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { JiraIssueEpic } from '@core/models/Jira/jira-issue-epic.model';

describe('EpicFieldComponent', () => {
    let component: EpicFieldComponent;
    let fixture: ComponentFixture<EpicFieldComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;
    const epic: JiraIssueEpic = {
        id: '1', name: 'Epic 1', self: '', key: '', summary: '', done: 'false', color: { key: '' }
    };

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService', ['getEpics']);
        const dropdownUtilServiceSpy = jasmine.createSpyObj('DropdownUtilService', ['mapEpicDataToSelectOption']);

        await TestBed.configureTestingModule({
            declarations: [EpicFieldComponent],
            imports: [ReactiveFormsModule, NgSelectModule],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: DropdownUtilService, useValue: dropdownUtilServiceSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(EpicFieldComponent);
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
            defaultValue: '1'
        };
        component.formGroup = new UntypedFormGroup({});
        apiService.getEpics.and.returnValue(Promise.resolve([]));
        dropdownUtilService.mapEpicDataToSelectOption.and.returnValue({ id: '1', name: 'Epic 1' });

        await component.ngOnInit();

        expect(component.jiraUrl).toBe('http://example.com');
        expect(component.projectKeyOrId).toBe('TEST');
        expect(component.selectedEpicId).toBe('1');
        expect(apiService.getEpics).toHaveBeenCalled();
    });

    it('should load epics on open', async () => {
        component.data = {
            jiraUrl: 'http://example.com',
            projectKeyOrId: 'TEST'
        };
        component.formGroup = new UntypedFormGroup({});

        apiService.getEpics.and.returnValue(Promise.resolve([epic]));
        dropdownUtilService.mapEpicDataToSelectOption.and.returnValue({ id: '1', name: 'Epic 1' });

        await component.onOpen();

        expect(apiService.getEpics).toHaveBeenCalled();
        expect(dropdownUtilService.mapEpicDataToSelectOption).toHaveBeenCalled();
    });

    it('should not load epics if already initialized', async () => {
        component.dataInitialized = true;
        component.data = {
            jiraUrl: 'http://example.com',
            projectKeyOrId: 'TEST'
        };
        component.formGroup = new UntypedFormGroup({});
        apiService.getEpics.and.returnValue(Promise.resolve([epic]));
        dropdownUtilService.mapEpicDataToSelectOption.and.returnValue({ id: '1', name: 'Epic 1' });

        await component.onOpen();

        expect(apiService.getEpics).not.toHaveBeenCalled();
        expect(dropdownUtilService.mapEpicDataToSelectOption).not.toHaveBeenCalled();
    });
});
