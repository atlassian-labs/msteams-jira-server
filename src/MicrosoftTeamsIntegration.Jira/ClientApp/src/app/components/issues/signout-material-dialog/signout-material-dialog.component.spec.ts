import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SignoutMaterialDialogComponent } from './signout-material-dialog.component';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ApiService, AuthService, UtilService, AppInsightsService } from '@core/services';
import { of, throwError } from 'rxjs';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';

describe('SignoutMaterialDialogComponent', () => {
    let component: SignoutMaterialDialogComponent;
    let fixture: ComponentFixture<SignoutMaterialDialogComponent>;
    let apiService: jasmine.SpyObj<ApiService>;
    let authService: jasmine.SpyObj<AuthService>;
    let utilService: jasmine.SpyObj<UtilService>;
    let appInsightsService: jasmine.SpyObj<AppInsightsService>;
    let dialogRef: jasmine.SpyObj<MatDialogRef<SignoutMaterialDialogComponent>>;

    beforeEach(async () => {
        const apiServiceSpy = jasmine.createSpyObj('ApiService', ['logOut', ['removePersonalNotification']]);
        const authServiceSpy = jasmine.createSpyObj('AuthService', ['']);
        const utilServiceSpy = jasmine.createSpyObj('UtilService', ['convertStringToNull']);
        const appInsightsServiceSpy = jasmine.createSpyObj('AppInsightsService', ['trackException']);
        const dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

        await TestBed.configureTestingModule({
            declarations: [SignoutMaterialDialogComponent],
            providers: [
                { provide: ApiService, useValue: apiServiceSpy },
                { provide: AuthService, useValue: authServiceSpy },
                { provide: UtilService, useValue: utilServiceSpy },
                { provide: AppInsightsService, useValue: appInsightsServiceSpy },
                { provide: MatDialogRef, useValue: dialogRefSpy },
                { provide: MAT_DIALOG_DATA, useValue: { jiraUrl: 'http://example.com' } }
            ],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();

        fixture = TestBed.createComponent(SignoutMaterialDialogComponent);
        component = fixture.componentInstance;
        apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
        authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
        utilService = TestBed.inject(UtilService) as jasmine.SpyObj<UtilService>;
        appInsightsService = TestBed.inject(AppInsightsService) as jasmine.SpyObj<AppInsightsService>;
        dialogRef = TestBed.inject(MatDialogRef) as jasmine.SpyObj<MatDialogRef<SignoutMaterialDialogComponent>>;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize correctly', async () => {
        utilService.convertStringToNull.and.returnValue('http://example.com');

        await component.ngOnInit();

        expect(utilService.convertStringToNull).toHaveBeenCalledWith('http://example.com');
    });

    it('should handle error during initialization', async () => {
        const error = new Error('Test error');
        utilService.convertStringToNull.and.throwError(error);

        await component.ngOnInit();

        expect(appInsightsService.trackException).toHaveBeenCalledWith(new Error('Error: Test error'),
            'SignoutMaterialDialogComponent::ngOnInit');
    });

    it('should handle any error during initialization', async () => {
        const error = { status: 401 };
        utilService.convertStringToNull.and.throwError(error as any);

        await component.ngOnInit();

        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('should sign out successfully', async () => {
        utilService.convertStringToNull.and.returnValue('http://example.com');
        apiService.removePersonalNotification.and.returnValue(Promise.resolve());
        apiService.logOut.and.returnValue(Promise.resolve({ isSuccess: true }));

        await component.ngOnInit();
        await component.signOut();

        expect(apiService.logOut).toHaveBeenCalledWith('http://example.com');
        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });

    it('should handle error during sign out', async () => {
        const error = new Error('Test error');
        apiService.logOut.and.returnValue(Promise.reject(error));
        utilService.convertStringToNull.and.returnValue('http://example.com');

        await component.ngOnInit();
        await component.signOut();

        expect(apiService.logOut).toHaveBeenCalledWith('http://example.com');
        expect(appInsightsService.trackException).toHaveBeenCalledWith(
            new Error('Error while signout from Jira'),
            'SignoutMaterialDialogComponent::signOut',
            { originalErrorMessage: error.message }
        );
        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });
});
