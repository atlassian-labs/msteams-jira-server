import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { AuthService } from '@core/services';
import { StatusCode } from '@core/enums';

describe('AuthGuard', () => {
    let authGuard: AuthGuard;
    let authService: jasmine.SpyObj<AuthService>;
    let router: jasmine.SpyObj<Router>;

    beforeEach(() => {
        const authServiceSpy = jasmine.createSpyObj('AuthService', ['getToken']);
        const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

        TestBed.configureTestingModule({
            providers: [
                AuthGuard,
                { provide: AuthService, useValue: authServiceSpy },
                { provide: Router, useValue: routerSpy }
            ]
        });

        authGuard = TestBed.inject(AuthGuard);
        authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
        router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    });

    it('should be created', () => {
        expect(authGuard).toBeTruthy();
    });

    it('should allow activation if user is authenticated', async () => {
        authService.getToken.and.returnValue(Promise.resolve('token'));

        const result = await authGuard.canActivate({} as any);

        expect(result).toBeTrue();
    });

    it('should navigate to login if user is not authenticated', async () => {
        authService.getToken.and.returnValue(Promise.reject('error'));
        const route = { params: { someParam: 'someValue' } } as any;

        const result = await authGuard.canActivate(route);

        expect(result).toBeFalse();
        expect(router.navigate)
            .toHaveBeenCalledWith(['/login', { ...route.params, status: StatusCode.Unauthorized }]);
    });
});
