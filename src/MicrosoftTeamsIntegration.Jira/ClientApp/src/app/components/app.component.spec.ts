import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AppComponent } from './app.component';
import { LoadingIndicatorService } from '@shared/services/loading-indicator.service';
import { RoutingState, UtilService } from '@core/services';
import { Router, NavigationStart, NavigationEnd, NavigationCancel, NavigationError } from '@angular/router';
import { of, Subject } from 'rxjs';
import * as microsoftTeams from '@microsoft/teams-js';
import { SharedModule } from '@shared/shared.module';

describe('AppComponent', () => {
    let component: AppComponent;
    let fixture: ComponentFixture<AppComponent>;
    let loadingIndicatorService: jasmine.SpyObj<LoadingIndicatorService>;
    let routingState: jasmine.SpyObj<RoutingState>;
    let utilService: jasmine.SpyObj<UtilService>;
    let router: jasmine.SpyObj<Router>;
    let routerEventsSubject: Subject<any>;

    beforeEach(async () => {
        const loadingIndicatorServiceSpy = jasmine.createSpyObj('LoadingIndicatorService', ['show', 'hide']);
        const routingStateSpy = jasmine.createSpyObj('RoutingState', ['loadRouting']);
        const utilServiceSpy = jasmine.createSpyObj('UtilService', ['isMobile', 'getQueryParam']);
        const routerSpy = jasmine.createSpyObj('Router', ['navigate'], { events: new Subject() });

        await TestBed.configureTestingModule({
            declarations: [AppComponent],
            imports: [RouterTestingModule, SharedModule],
            providers: [
                { provide: LoadingIndicatorService, useValue: loadingIndicatorServiceSpy },
                { provide: RoutingState, useValue: routingStateSpy },
                { provide: UtilService, useValue: utilServiceSpy },
                { provide: Router, useValue: routerSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(AppComponent);
        component = fixture.componentInstance;
        loadingIndicatorService = TestBed.inject(LoadingIndicatorService) as jasmine.SpyObj<LoadingIndicatorService>;
        routingState = TestBed.inject(RoutingState) as jasmine.SpyObj<RoutingState>;
        utilService = TestBed.inject(UtilService) as jasmine.SpyObj<UtilService>;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
        routerEventsSubject = router.events as Subject<any>;

        spyOn(microsoftTeams.app, 'initialize').and.returnValue(Promise.resolve());
        spyOn(microsoftTeams.app, 'registerOnThemeChangeHandler').and.returnValue();
        spyOn(microsoftTeams.app, 'getContext').and.returnValue(Promise.resolve({
            app: {
                locale: 'en-US',
                theme: 'default'
            },
            page: {
                id: 'pageId',
                frameContext: 'content'
            },
            user: {
                id: 'userId',
                displayName: 'Test User'
            },
            team: {
                id: 'teamId',
                name: 'Test Team'
            },
            channel: {
                id: 'channelId',
                name: 'Test Channel'
            }
        } as any));
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize correctly', async () => {
        spyOn(component, 'ngOnInit').and.callThrough();
        utilService.isMobile.and.returnValue(Promise.resolve(false));

        await component.ngOnInit();

        expect(routingState.loadRouting).toHaveBeenCalled();
        expect(microsoftTeams.app.initialize).toHaveBeenCalled();
        expect(microsoftTeams.app.registerOnThemeChangeHandler).toHaveBeenCalled();
        expect(utilService.isMobile).toHaveBeenCalled();
        expect(document.body.classList.contains('mobile')).toBeFalse();
    });

    it('should show loading indicator on NavigationStart', async () => {
        await component.ngOnInit();
        routerEventsSubject.next(new NavigationStart(1, 'testUrl'));
        expect(loadingIndicatorService.show).toHaveBeenCalled();
    });

    it('should hide loading indicator on NavigationEnd', async () => {
        await component.ngOnInit();

        routerEventsSubject.next(new NavigationEnd(1, 'testUrl', 'testUrl'));
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should hide loading indicator on NavigationCancel', async () => {
        await component.ngOnInit();
        routerEventsSubject.next(new NavigationCancel(1, 'testUrl', 'testUrl'));
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should hide loading indicator on NavigationError', async () => {
        await component.ngOnInit();
        routerEventsSubject.next(new NavigationError(1, 'testUrl', 'error'));
        expect(loadingIndicatorService.hide).toHaveBeenCalled();
    });

    it('should add mobile class to body if isMobile returns true', async () => {
        utilService.isMobile.and.returnValue(Promise.resolve(true));

        await component.ngOnInit();

        expect(document.body.classList.contains('mobile')).toBeTrue();
    });


    it('should apply Teams theme as a class', async () => {
        microsoftTeams.app.getContext = jasmine.createSpy('getContext').and.returnValue(Promise.resolve({
            app: {
                locale: 'en-US',
                theme: 'dark'
            },
            page: {
                id: 'pageId',
                frameContext: 'content'
            },
            user: {
                id: 'userId',
                displayName: 'Test User'
            },
            team: {
                id: 'teamId',
                name: 'Test Team'
            },
            channel: {
                id: 'channelId',
                name: 'Test Channel'
            }
        } as any));
        await component.ngOnInit();

        expect(document.body.classList.contains('dark')).toBeTrue();
    });

    it('should remove mobile class from body if isMobile returns false', async () => {
        utilService.isMobile.and.returnValue(Promise.resolve(false));
        await component.ngOnInit();
        expect(document.body.classList.contains('mobile')).toBeFalse();
    });

    it('should unsubscribe from router events on destroy', () => {
        spyOn(component, 'ngOnDestroy').and.callThrough();
        component.ngOnDestroy();
        expect(component.ngOnDestroy).toHaveBeenCalled();
    });
});
