import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ErrorService } from './error.service';
import { AppInsightsService } from '@core/services/app-insights.service';
import { MatDialog } from '@angular/material/dialog';
import { NotificationService } from '@shared/services/notificationService';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { StatusCode, ApplicationType } from '@core/enums';

describe('ErrorService', () => {
    let service: ErrorService;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let matDialog: jasmine.SpyObj<MatDialog>;
    let notificationService: jasmine.SpyObj<NotificationService>;
    let router: jasmine.SpyObj<Router>;

    beforeEach(() => {
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['trackWarning', 'trackException']);
        const matDialogSpy = jasmine.createSpyObj('MatDialog', ['closeAll']);
        const notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['notifyError']);
        const routerStateSpy = jasmine.createSpyObj('RouterState', [], {
            root: jasmine.createSpyObj('ActivatedRoute', [], {
                children: [{ params: { subscribe: (callback: any) => callback({}) } }]
            })
        });
        const routerSpy = jasmine.createSpyObj('Router', ['navigate'], {
            routerState: routerStateSpy
        });

        TestBed.configureTestingModule({
            imports: [RouterTestingModule],
            providers: [
                ErrorService,
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: MatDialog, useValue: matDialogSpy },
                { provide: NotificationService, useValue: notificationServiceSpy },
                { provide: Router, useValue: routerSpy }
            ]
        });

        service = TestBed.inject(ErrorService);
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        matDialog = TestBed.inject(MatDialog) as jasmine.SpyObj<MatDialog>;
        notificationService = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should show Jira unavailable window', () => {
        service.showJiraUnavailableWindow(StatusCode.ServiceUnavailable);

        expect(appInsightsService.trackWarning).toHaveBeenCalled();
        expect(router.navigate).toHaveBeenCalledWith(['/error'],
            { queryParams: { message: service.UNAVAILABLE_SITE_MESSAGE, status: StatusCode.ServiceUnavailable },
                queryParamsHandling: 'merge' });
    });

    it('should show addon not installed window', () => {
        service.showAddonNotInstalledWindow();

        expect(appInsightsService.trackWarning).toHaveBeenCalled();
        expect(router.navigate).toHaveBeenCalledWith(['/error'], { queryParams: { message: service.ADDON_NOT_INSTALLED_MESSAGE, ...{} } });
    });

    it('should show default error', () => {
        // Override the router spy object for this specific test
        const overriddenRouter = jasmine.createSpyObj('Router', ['navigate'], {
            routerState: jasmine.createSpyObj('RouterState', [], {
                root: jasmine.createSpyObj('ActivatedRoute', [], {
                    children: [{ params: { subscribe: (callback: any) =>
                        callback({ application: ApplicationType.JiraServerCompose }) } }]
                })
            })
        });

        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            imports: [RouterTestingModule],
            providers: [
                ErrorService,
                { provide: AppInsightsService, useValue: appInsightsService },
                { provide: MatDialog, useValue: matDialog },
                { provide: NotificationService, useValue: notificationService },
                { provide: Router, useValue: overriddenRouter }
            ]
        }).compileComponents();

        service = TestBed.inject(ErrorService);

        service.showDefaultError(new Error('Test error'));

        expect(appInsightsService.trackException).toHaveBeenCalled();
        expect(overriddenRouter.navigate).toHaveBeenCalledWith(['/error'],
            { queryParams: { message: service.DEFAULT_ERROR_MESSAGE, ...{ application: ApplicationType.JiraServerCompose } } });
    });

    it('should show error message', () => {
        service.showErrorMessage('Test error message');

        expect(router.navigate).toHaveBeenCalledWith(['/error'], { queryParams: { message: 'Test error message' } });
    });

    it('should show error modal', () => {
        const error = new Error('Test error');

        service.showErrorModal(error);

        expect(appInsightsService.trackException).toHaveBeenCalled();
        expect(notificationService.notifyError).toHaveBeenCalled();
    });

    it('should go to login with status code', () => {
        service.goToLoginWithStatusCode(StatusCode.Unauthorized);

        expect(router.navigate).toHaveBeenCalledWith(['/login', { ...{}, status: StatusCode.Unauthorized }]);
    });

    it('should show my filters empty error', async () => {
        spyOn(service, 'showMyFiltersEmptyError').and.callThrough();

        await service.showMyFiltersEmptyError();

        expect(router.navigate).toHaveBeenCalledWith(['/favorite-filters-empty']);
    });

    it('should get HTTP error message', () => {
        const error = new HttpErrorResponse({ error: { error: 'Test error' }, status: 400, statusText: 'Bad Request' });

        const message = service.getHttpErrorMessage(error);

        expect(message).toBe('Test error');
    });

    it('should get default error message', () => {
        const error = new Error('Test error');

        const message = service.getHttpErrorMessage(error);

        expect(message).toBe(service.DEFAULT_ERROR_MESSAGE);
    });
});
