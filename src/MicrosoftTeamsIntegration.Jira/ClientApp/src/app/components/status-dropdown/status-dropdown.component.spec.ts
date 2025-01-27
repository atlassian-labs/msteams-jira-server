import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StatusDropdownComponent } from './status-dropdown.component';
import { IssueTransitionService } from '@core/services/entities/transition.service';
import { DropdownUtilService } from '@shared/services/dropdown.util.service';
import { AppInsightsService } from '@core/services';
import { DropDownOption } from '@shared/models/dropdown-option.model';
import { JiraTransition } from '@core/models/Jira/jira-transition.model';
import { IssueStatus } from '@core/models';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';

describe('StatusDropdownComponent', () => {
    let component: StatusDropdownComponent;
    let fixture: ComponentFixture<StatusDropdownComponent>;
    let transitionService: jasmine.SpyObj<IssueTransitionService>;
    let dropdownUtilService: jasmine.SpyObj<DropdownUtilService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;

    beforeEach(async () => {
        const transitionServiceSpy = jasmine.createSpyObj('IssueTransitionService', ['getTransitions', 'doTransition']);
        const dropdownUtilServiceSpy = jasmine.createSpyObj('DropdownUtilService', ['mapTransitionToDropdownOption']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['trackException']);

        await TestBed.configureTestingModule({
            declarations: [StatusDropdownComponent],
            providers: [
                { provide: IssueTransitionService, useValue: transitionServiceSpy },
                { provide: DropdownUtilService, useValue: dropdownUtilServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy }
            ],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(StatusDropdownComponent);
        component = fixture.componentInstance;
        transitionService = TestBed.inject(IssueTransitionService) as jasmine.SpyObj<IssueTransitionService>;
        dropdownUtilService = TestBed.inject(DropdownUtilService) as jasmine.SpyObj<DropdownUtilService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize status options on init', async () => {
        const transitions = [{ id: '1', to: { id: '1' } }] as JiraTransition[];
        transitionService.getTransitions.and.returnValue(Promise.resolve({ expand: '', transitions }));
        dropdownUtilService.mapTransitionToDropdownOption.and.returnValue({
            id: '1',
            value: {
                id: '1',
                name: 'Test Transition',
                to: { 
                    id: '1', 
                    name: 'Test Status', 
                    self: 'selfUrl', 
                    description: 'Test Description', 
                    iconUrl: 'iconUrl', 
                    statusCategory: { id: 1, key: 'test', colorName: 'blue', name: 'Test Category' } 
                },
                hasScreen: false,
                isGlobal: false,
                isInitial: false,
                isConditional: false
            },
            label: 'Test'
        });

        component.jiraUrl = 'testUrl';
        component.issueKey = 'testKey';
        component.initialStatus = { id: '1', name: 'Test' } as IssueStatus;

        await component.ngOnInit();

        expect(component.statusOptions.length).toBe(1);
        expect(component.selectedOption.id).toBe('1');
    });

    it('should handle status option selection', async () => {
        const option = { id: '1', value: { to: { id: '1' } }, label: 'Test' } as DropDownOption<JiraTransition>;
        transitionService.doTransition.and.returnValue(Promise.resolve({ isSuccess: true }));

        spyOn(component.statusChange, 'emit');

        await component.onStatusOptionSelected(option);

        expect(component.selectedOption).toBe(option);
        expect(component.statusChange.emit).toHaveBeenCalledWith('1');
    });

    it('should handle status option selection error', async () => {
        const option = { id: '1', value: { id: '1' }, label: 'Test' } as DropDownOption<JiraTransition>;
        const error = new Error('Test error');
        transitionService.doTransition.and.returnValue(Promise.reject(error));

        await component.onStatusOptionSelected(option);

        expect(appInsightsService.trackException).toHaveBeenCalledWith(
            new Error('Error: Test error'),
            'StatusDropdwonComponent::onStatusOptionSelected',
            option
        );
        expect(component.errorMessage).toBe('Test error');
    });
});
